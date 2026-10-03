# API Mapping — Active Route Tracking

## GET `/api/viagens/{tripId}/detalhes`

`ActiveRouteTracking` reads `tripId` from `/active-route-tracking/:tripId`. Both opening details and **Consultar localização** invoke the same authenticated GET through `trip-service.js`. There is no ping POST, polling, map dependency, provider credential, or forced-refresh parameter.

### Cache and lifecycle

- A successful calculation less than 15 minutes old is reused (`isCacheRenovado: false`). Exactly 15 minutes is expired; only the server decides whether another provider lookup is required.
- A committed new calculation returns `isCacheRenovado: true`.
- Failed refresh with an existing snapshot returns `cacheDesatualizado: true`, `aviso`, and the previous successful timestamp. The UI prominently warns that data is stale.
- Failed refresh without a snapshot returns standardized `503`; the UI shows an unavailable/retry state, not invented telemetry.
- `ENTREGUE` and `CANCELADA` are terminal. The UI presents preserved history and **Consultar histórico**; the same GET cannot restart monitoring.
- The API sets `Cache-Control: no-store`; this does not disable the application-level 15-minute cache.
- Timestamps are UTC on the wire and shown in the browser's local timezone. GPS fix time (`ultimaPosicaoEm`) and last calculation (`ultimaAtualizacao`) are displayed separately.

### Response

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "rotaId": "22222222-2222-2222-2222-222222222222",
  "referencia": "TRP-111111",
  "status": "EM_TRANSITO",
  "risco": "NORMAL",
  "transportadora": "Transportadora",
  "placa": "ABC1D23",
  "motorista": "Motorista",
  "telefoneMotorista": "+55 41 99999-0000",
  "progressoPercentual": 40,
  "distanciaTotalKm": 400,
  "distanciaPercorridaKm": 160,
  "distanciaRestanteKm": 240,
  "ocupacaoPercentual": 85,
  "novoEta": "2026-10-01T19:30:00Z",
  "prazoProximaParada": "2026-10-01T20:00:00Z",
  "latitude": -25.4,
  "longitude": -49.2,
  "ultimaPosicaoEm": "2026-10-01T18:39:00Z",
  "ultimaAtualizacao": "2026-10-01T18:40:00Z",
  "isCacheRenovado": false,
  "cacheDesatualizado": false,
  "aviso": null,
  "paradas": [
    {
      "id": "33333333-3333-3333-3333-333333333333",
      "ordem": 1,
      "cidade": "Curitiba",
      "estado": "PR",
      "latitude": -25.4,
      "longitude": -49.2,
      "status": "CONCLUIDA",
      "concluidaEm": "2026-10-01T16:55:00Z",
      "fonteConclusao": "GEOFENCE_INFERRED",
      "acoes": [
        {
          "id": "44444444-4444-4444-4444-444444444444",
          "trechoId": "55555555-5555-5555-5555-555555555555",
          "produto": "Carga",
          "tipo": "COLETA",
          "prazo": "2026-10-01T17:00:00Z",
          "concluidaEm": "2026-10-01T16:55:00Z",
          "fonteConclusao": "GEOFENCE_INFERRED"
        }
      ]
    }
  ],
  "eventos": [
    {
      "id": "66666666-6666-6666-6666-666666666666",
      "tipo": "CALCULO",
      "descricao": "Posição observada e percurso restante calculado sob demanda.",
      "ocorridoEm": "2026-10-01T18:40:00Z"
    }
  ]
}
```

### Presentation

- Driver/contact and monitoring fields can be null and render as **Não informado**. Coordinates are textual, not a simulated map. Phone links are only offered when a usable number exists.
- The browser does not calculate ETA, SLA, route distances, completion or utilization. Utilization over 100% remains visible; unknown is not zero. `novoEta` concerns the next pending stop, not final arrival.
- The server's supplied stop order is preserved, including revisits. Status drives accessible labels and colors: `CONCLUIDA` green, `EM_TRANSITO` yellow, `PENDENTE` gray. Each action retains its individual pickup/delivery deadline and actual completion timestamp.
- `GEOFENCE_INFERRED` is explicitly labeled as GPS-inferred, not proof of physical handling. `LEGACY_STATE` can indicate completion with no known actual time; the UI does not invent one.
- Only server event descriptions and timestamps are rendered (bounded recent window of 50); no simulated traffic events.
- Every request is cancellable and protected against out-of-order response updates. Loading, missing data, failed request/retry, stale, cached and terminal states are distinct.

Standard errors: validation `400`, unauthenticated `401`, forbidden `403`, missing/cross-tenant trip `404`, tracking unavailable without cache `503`.

### Focused verification

From `sigloc-ui`, run `node --test src/services/monitoring-contract.test.mjs` and `npm run build`. Tests use Vite's existing module loader and Node's built-in test runner; no live API/provider calls or extra testing dependency are required.