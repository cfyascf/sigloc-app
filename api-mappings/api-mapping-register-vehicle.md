# API Mapping — Register Vehicle

## POST `/api/vehicles`

`RegisterVehicle` submits through the authenticated `vehicleService.createVehicle` client. Carrier access and tenant scoping are enforced by the API. Success is `201` with the created vehicle DTO; the UI returns to fleet management. Validation/network errors remain visible without discarding the form.

```json
{
  "plate": "ABC1D23",
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
  "driverPhone": "+55 41 99999-0000",
  "traccarDeviceId": 42
}
```

- `driverPhone` and `traccarDeviceId` are optional/nullable. Blank inputs serialize as `null`; nonblank phones are trimmed, at most 30 characters. Tracker IDs must be positive safe integers in the JavaScript client. No provider credentials are collected.
- The service normalizes plate to uppercase without spaces/hyphens. The server validates its format and uniqueness.
- Positive axle count, weight and volume are required. Enum integers are sourced from `src/constants/vehicles.js`; body type is 1–5, refrigeration 0–2.
- New vehicles start `Livre` (`status: 0`); create does not accept a status override. Status changes belong to fleet editing.
- The response includes `id`, `transportadoraId`, all vehicle attributes, numeric `status`, and the nullable tracking/contact fields.
- The form does not call a plate-validation endpoint or fetch a body-type catalog; it submits once and displays API errors.
- Authentication/authorization and validation errors use the existing shared API error format.