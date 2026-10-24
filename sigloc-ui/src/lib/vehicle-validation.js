/**
 * Client-side vehicle checks with Portuguese messages, run before the request so the browser's
 * own (English) number-input tooltips never show. Keys are the form's field names.
 * `creating` adds the fields only the registration form edits (plate, axles, body type).
 */
export function validateVehicle(form, { creating = false } = {}) {
  const errors = {}
  if (creating && !form.plate?.trim()) errors.plate = "Informe a placa."
  if (!form.model?.trim()) errors.model = "Informe a marca/modelo."
  if (creating) {
    const axles = Number(form.axleCount)
    if (!(Number.isInteger(axles) && axles > 0)) errors.axleCount = "Informe a quantidade de eixos (número inteiro maior que zero)."
  }
  if (!(Number(form.weightKg) > 0)) errors.weightKg = "A capacidade de peso deve ser maior que zero."
  if (!(Number(form.volumeM3) > 0)) errors.volumeM3 = "A capacidade de volume deve ser maior que zero."
  if (creating && !form.bodyType) errors.bodyType = "Selecione o tipo de carroceria."
  const tracker = String(form.traccarDeviceId ?? "").trim()
  if (tracker && !(/^\d+$/.test(tracker) && Number(tracker) > 0 && Number.isSafeInteger(Number(tracker)))) {
    errors.traccarDeviceId = "O ID do dispositivo deve ser um número inteiro maior que zero."
  }
  return errors
}

/** API field names (vehicle DTO) to the form's field names, for server-side field errors. */
const API_FIELD_TO_FORM = { capacityWeight: "weightKg", capacityVolume: "volumeM3", currentLocation: "location" }

export function mapVehicleFieldErrors(fieldErrors) {
  const mapped = {}
  for (const [field, message] of Object.entries(fieldErrors ?? {})) {
    const key = API_FIELD_TO_FORM[field] ?? field
    mapped[key] ??= message
  }
  return mapped
}
