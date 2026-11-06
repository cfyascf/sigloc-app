import { RNTRC_SOURCE } from "@/constants/vehicles"

// Same limits as the API (VehicleService): generous, they only catch typos like 999.999.999 kg.
export const VEHICLE_LIMITS = { axles: 12, weightKg: 100_000, volumeM3: 500, model: 100, phone: 40 }

/**
 * Client-side vehicle checks with Portuguese messages, run before the request so the browser's
 * own (English) number-input tooltips never show. Keys are the form's field names.
 * `creating` adds the fields only the registration form edits (plate, axles, body type).
 */
export function validateVehicle(form, { creating = false } = {}) {
  const errors = {}
  if (creating && !form.plate?.trim()) errors.plate = "Informe a placa."
  if (!form.model?.trim()) errors.model = "Informe a marca/modelo."
  else if (form.model.trim().length > VEHICLE_LIMITS.model) errors.model = `A marca/modelo deve ter no máximo ${VEHICLE_LIMITS.model} caracteres.`
  if (creating || (form.axleCount !== undefined && form.axleCount !== null && form.axleCount !== "")) {
    const axles = Number(form.axleCount)
    if (!(Number.isInteger(axles) && axles > 0)) errors.axleCount = "Informe a quantidade de eixos (número inteiro maior que zero)."
    else if (axles > VEHICLE_LIMITS.axles) errors.axleCount = `A quantidade de eixos deve ser no máximo ${VEHICLE_LIMITS.axles}.`
  }
  const weight = Number(form.weightKg)
  if (!(weight > 0)) errors.weightKg = "A capacidade de peso deve ser maior que zero."
  else if (weight > VEHICLE_LIMITS.weightKg) errors.weightKg = "A capacidade de peso deve ser no máximo 100.000 kg."
  const volume = Number(form.volumeM3)
  if (!(volume > 0)) errors.volumeM3 = "A capacidade de volume deve ser maior que zero."
  else if (volume > VEHICLE_LIMITS.volumeM3) errors.volumeM3 = "A capacidade de volume deve ser no máximo 500 m³."
  if (creating && !form.bodyType) errors.bodyType = "Selecione o tipo de carroceria."
  if (creating) Object.assign(errors, validateDocuments(form))
  const tracker = String(form.traccarDeviceId ?? "").trim()
  if (tracker && !(/^\d+$/.test(tracker) && Number(tracker) > 0 && Number.isSafeInteger(Number(tracker)))) {
    errors.traccarDeviceId = "O ID do dispositivo deve ser um número inteiro maior que zero."
  }
  const phone = String(form.driverPhone ?? "").trim()
  if (phone && !/^[\d\s+\-().]+$/.test(phone)) {
    errors.driverPhone = "Telefone inválido: use apenas números, espaços e + - ( ) ."
  }
  return errors
}

/**
 * Documents a new vehicle must have (the API checks the same, plus CPF/CNPJ check digits). The
 * driver's CNH is only asked when a driver is assigned; on edit, missing documents stay optional.
 */
function validateDocuments(form) {
  const errors = {}
  if (!form.composition) errors.composition = "Informe a composição do veículo."
  if (!form.rntrcSource) errors.rntrcSource = "Informe de quem é o RNTRC do veículo."
  if (Number(form.rntrcSource) === RNTRC_SOURCE.THIRD_PARTY) {
    if (!/^0?\d{8}$/.test(String(form.rntrcNumber ?? "").replace(/[\s.-]/g, ""))) errors.rntrcNumber = "Informe o RNTRC com 8 dígitos."
    if (!form.rntrcCategory) errors.rntrcCategory = "Informe a categoria do RNTRC."
    if (!form.rntrcHolderName?.trim()) errors.rntrcHolderName = "Informe o titular do RNTRC."
    const digits = String(form.rntrcHolderDocument ?? "").replace(/\D/g, "")
    if (digits.length !== 11 && digits.length !== 14) errors.rntrcHolderDocument = "Informe o CPF ou CNPJ do titular."
    if (!form.rntrcValidUntil) errors.rntrcValidUntil = "Informe a data de revalidação do RNTRC."
  }
  if (form.driver?.trim()) {
    if (!form.driverCnhCategory) errors.driverCnhCategory = "Informe a categoria da CNH."
    if (typeof form.driverHasEar !== "boolean") errors.driverHasEar = "Informe se a CNH tem EAR."
    if (!form.driverToxicologicalExamValidUntil) errors.driverToxicologicalExamValidUntil = "Informe a validade do exame toxicológico."
  }
  if (form.hasMopp && !form.moppValidUntil) errors.moppValidUntil = "Informe a validade do MOPP."
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
