# API Mapping — Carrier Route Tracking

`CarrierRouteTracking` (`/carrier-route-tracking` and `/carrier-route-tracking/:tripId`, carrier role) lets the awarded carrier operate a trip in transit. All endpoints require `RequireCarrierAccess`. The carrier is taken from the JWT `empresaId`; a trip awarded to another carrier is **404**, never 403. Mutations also need the user id (`NameIdentifier`) and return **401** without it.

| Verb + path | Purpose |
|---|---|
| `GET /api/transportador/viagens/ativas?page&pageSize&search&status&risk` | Carrier's active trips (same paging/filters as `/api/viagens/ativas`, plus `embarcador`) |
| `GET /api/transportador/viagens/{id}/detalhes?refresh=true\|false` | Detail. `refresh=true` is **Transmitir Posição GPS (Ping)** |
| `POST /api/transportador/viagens/{id}/paradas/{stopId}/conclusao` | Confirm the next pending stop (multipart) |
| `POST /api/transportador/viagens/{id}/ocorrencias` | Report an occurrence |
| `GET /api/transportador/viagens/{id}/comprovantes/{proofId}` | Download a proof of delivery |

## Detail response

Every detail-returning endpoint (GET detail, stop completion, occurrence) answers with the same shape. `viagem` is exactly the shipper detail documented in `api-mapping-active-route-tracking.md`, so the same normalizer (`toTripDetails`) applies.

```json
{
  "viagem": { "viagemId": "…", "statusGeral": "EM_CURSO", "indicadorRisco": "WARNING", "paradas": [ … ], "eventos": [ … ], "…": "…" },
  "embarcador": { "razaoSocial": "Embarcador SA", "nomeFantasia": "Embarcador", "nomeContato": "Mesa Logística", "telefone": "(41) 3333-0000", "email": "logistica@embarcador.com.br" },
  "especificacaoCarga": {
    "ambiente": "Congelado", "refrigerada": true, "temperaturaMinima": -20, "temperaturaMaxima": -15,
    "paletizada": true, "embalagens": ["Paletizado"], "perigosa": false, "fragil": false,
    "carroceria": "Baú / Sider", "pesoTotalKg": 1200, "volumeTotalM3": 30,
    "produtos": [{ "nome": "Sorvete", "quantidade": 12 }]
  },
  "acaoAtual": { "paradaId": "…", "tipo": "COLETA | ENTREGA | COLETA_E_ENTREGA", "exigeComprovante": true, "prazo": "2026-10-01T20:00:00Z" },
  "ocorrencias": [{ "id": "…", "tipo": "TRANSITO_INTENSO", "rotulo": "Trânsito Intenso", "severidade": "WARNING", "observacao": "…", "reportadaEm": "…", "paradaId": "…", "encerradaEm": null, "aberta": true }],
  "comprovantes": [{ "id": "…", "paradaId": "…", "nomeArquivo": "canhoto.pdf", "tipoConteudo": "application/pdf", "tamanhoBytes": 18234, "enviadoEm": "…" }]
}
```

- `embarcador` contact fields are optional (shipper Profile → "Contato Logístico") and render as **Não informado** when null. Offer `tel:`/`mailto:` links only for usable values.
- `especificacaoCarga` is derived from the products on the trip's segments: refrigeration/bodywork/MOPP/fixing use the "strongest wins" rule, the temperature window is the range every product tolerates, weight/volume are route totals (null when unknown).
- `acaoAtual` is the **only** stop the carrier can confirm next; it is null for delivered/cancelled trips or when nothing is pending. `exigeComprovante` is true when the stop has a pending delivery.

## Stop completion (`multipart/form-data`)

| Field | Required | Notes |
|---|---|---|
| `comprovante` | when `acaoAtual.exigeComprovante` | JPG, PNG or PDF, max 10 MB. Type is detected from the file content; the name and declared type are ignored |
| `observacao` | no | max 500 characters, added to the event log |

- Only `acaoAtual.paradaId` can be completed → otherwise **409 `TRIP_STOP_NOT_ACTIONABLE`**; unknown stop → **404 `TRIP_STOP_NOT_FOUND`**; delivered/cancelled trip → **409 `TRIP_NOT_ACTIVE`**; missing/invalid file → **400 `VALIDATION_ERROR`** (`details[].field = "comprovante"`); body over 11 MB → **413 `PAYLOAD_TOO_LARGE`**.
- Completion sources: `DRIVER_CONFIRMED` (pickup) and `POD_CONFIRMED` (delivery), alongside the existing `GEOFENCE_INFERRED`/`LEGACY_STATE`. Completing the first stop starts the trip; completing the last finishes it (`ENTREGUE`).
- After a manual completion the previous ETA no longer applies: `novoEta` and the ETA risk are cleared until the next ping, and that ping always recalculates (it bypasses the 15-minute cache).

## Occurrences

Request: `{ "tipo": "TRANSITO_INTENSO | FALTA_DOCUMENTACAO | AVARIA_MECANICA | RISCO_SLA", "observacao": "opcional, ≤ 500" }` → **201** with `Location` pointing at the detail.

- Severity: `AVARIA_MECANICA` and `RISCO_SLA` → `CRITIC`; `TRANSITO_INTENSO` and `FALTA_DOCUMENTACAO` → `WARNING`.
- The trip's effective risk is the highest of the ETA risk and every **open** occurrence. `CRITIC` shows as `statusGeral: ATRASADO` and `statusSla: ATRASADO`; `WARNING` shows as `statusSla: ATENCAO`. Shippers see the same effective risk.
- An occurrence closes automatically when the stop that was next when it was reported is completed (manually or by geofence). Events `OCORRENCIA`, `OCORRENCIA_ENCERRADA` and `SLA` are added to `viagem.eventos`.

## GPS ping

`GET …/detalhes?refresh=true` runs the same server pipeline as the shipper's "Localizar Motorista": Traccar position, OpenRouteService routing, geofence inference, ETA/risk, 15-minute cache and stale fallback (`cacheDesatualizado`, `aviso`). `isCacheRenovado: false` means no new position was calculated — the UI must say so instead of implying a fresh fix. `503 TRACKING_UNAVAILABLE` when nothing was ever calculated and the provider fails.

## Storage

Proof files are stored through `IFileStorage` (local disk under `FileStorage:RootPath`, default `App_Data/uploads`). On Azure App Service set `FileStorage__RootPath` to a persistent path such as `/home/data/sigloc/uploads`.
