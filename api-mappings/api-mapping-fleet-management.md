# API Mapping — Fleet Management

The implemented carrier-scoped resource is `/api/vehicles` (not `/api/v1/fleet/vehicles`). All calls use the existing authenticated API client.

| Method | Endpoint | Response / consumer |
| --- | --- | --- |
| GET | `/api/vehicles` | `200`, array of vehicle DTOs; `FleetManagement` filters locally |
| GET | `/api/vehicles/{id}` | `200`, one vehicle DTO |
| PUT | `/api/vehicles/{id}` | `204`, no content; inline editor updates the visible row |
| DELETE | `/api/vehicles/{id}` | `204`, no content; explicit deletion confirmation |

Vehicle response fields: `id`, `transportadoraId`, `plate`, `model`, `axleCount`, `capacityWeight`, `capacityVolume`, `bodyType`, `refrigerationLevel`, `hasMopp`, `hasCargoSecuring`, `driver`, `currentLocation`, `status`, optional/nullable `driverPhone` and `traccarDeviceId`.

## Update contract

```json
{
  "model": "Volvo FH",
  "axleCount": 6,
  "capacityWeight": 20000,
  "capacityVolume": 80,
  "bodyType": 1,
  "refrigerationLevel": 0,
  "hasMopp": false,
  "hasCargoSecuring": true,
  "driver": "Motorista",
  "currentLocation": "Curitiba, PR",
  "status": 0,
  "driverPhone": "+55 41 99999-0000",
  "traccarDeviceId": 42
}
```

`plate` is immutable and excluded from PUT. The service maps capacity/location names and numeric enums to existing UI names/Portuguese labels, preserving noneditable fields when round-tripping. `driverPhone` and `traccarDeviceId` are editable through the shared `VehicleForm` component and preserved by `toVehicle`/`toUpdateDto`.

On update, `null`/omitted tracking or contact fields preserve existing values. A blank phone is sent as an empty string to clear it; a blank tracker is sent as `null` and keeps the existing device (the form explains this). The current API does not offer tracker removal. Phones are trimmed (maximum 30 characters); the browser service rejects nonpositive/fractional/unsafe tracker IDs before sending. Existing numeric enum mappings remain unchanged. Tracking identifiers stay on the vehicle API; the browser never contacts Traccar directly.

The screen shows loading, empty and API-error states; update/delete failures retain the current editing/confirmation state. No fleet statistics, locked-period, feature-list, or server pagination endpoints are called by this screen.

See [registration](api-mapping-register-vehicle.md) for POST.