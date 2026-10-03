# API Mapping — Active Routes

## GET `/api/viagens/ativas`

`ActiveRoutes` uses the existing authenticated API client through `trip-service.js`. Shipper/contractor access and tenant isolation are enforced server-side. The list reads persisted state only: it does not poll, call GPS/routing providers, or eagerly fetch details.

| Query | Contract |
| --- | --- |
| `page` | 1-based, default 1 |
| `pageSize` | Default 20, maximum 100; UI offers 20/50/100 |
| `search` | Trimmed reference/carrier/plate search; 350 ms UI debounce resets page to 1 |
| `status` | Optional `AGUARDANDO_COLETA`, `EM_TRANSITO`, `ATRASADO` |
| `risk` | Optional `NORMAL`, `CRITIC`, `NAO_MONITORADO` |

```json
{
  "viagens": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "rotaId": "22222222-2222-2222-2222-222222222222",
      "referencia": "TRP-111111",
      "status": "EM_TRANSITO",
      "risco": "NORMAL",
      "transportadora": "Transportadora",
      "placa": "ABC1D23",
      "origem": "Curitiba, PR",
      "destino": "Joinville, SC",
      "progressoPercentual": 40,
      "novoEta": "2026-10-01T19:30:00Z",
      "ultimaAtualizacao": "2026-10-01T18:40:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

- `referencia` follows the existing dashboard convention: `TRP-` plus the first six uppercase GUID hexadecimal characters. It is display/search text, not the unique request identifier.
- `id` is the trip GUID; `rotaId` is a different consolidated-route GUID. Cards navigate to `/active-route-tracking/{id}`, never using `rotaId` for detail requests.
- Delivered/cancelled trips are excluded. `ATRASADO` is the persisted SLA projection; unmonitored trips remain visible with nullable progress, ETA and calculation timestamp. Unknown values are never replaced with on-time/zero values.
- `ultimaAtualizacao` is the last successful calculation, not GPS fix time. GPS observation time is supplied by the detail endpoint.
- Filters reset pagination. Requests carry abort signals and request-key ownership prevents old responses from replacing newer results.
- Loading, empty, failed-request/retry and previous/next states are explicit. Returning from details requests the list again to reflect persisted lifecycle/SLA changes.
- Standard errors include validation `400`, authentication `401`, and authorization `403`. No refresh bypass is sent.

See [tracking](api-mapping-active-route-tracking.md) for cache, stale-state and detail behavior.