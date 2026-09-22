/**
 * Product domain constants — the single source of truth for the enum wire
 * values exchanged with the Sigloc API and their user-facing labels.
 *
 * The API contract uses Portuguese wire values (see backend
 * `ProductEnumMappings`); the UI renders friendly labels while always sending
 * and comparing the exact wire strings.
 */

/** Product category wire values (`categoria`). */
export const PRODUCT_CATEGORY = {
  GENERAL: "Geral",
  SOLID_BULK: "GranelSolido",
  LIQUID_BULK: "GranelLiquido",
}

export const PRODUCT_CATEGORY_OPTIONS = [
  { value: PRODUCT_CATEGORY.GENERAL, label: "Geral" },
  { value: PRODUCT_CATEGORY.SOLID_BULK, label: "Granel Sólido" },
  { value: PRODUCT_CATEGORY.LIQUID_BULK, label: "Granel Líquido" },
]

/** Transport environment wire values (`ambienteTransporte`). */
export const TRANSPORT_ENVIRONMENT = {
  DRY: "Seco",
  CHILLED: "Resfriado",
  FROZEN: "Congelado",
}

export const TRANSPORT_ENVIRONMENT_OPTIONS = [
  { value: TRANSPORT_ENVIRONMENT.DRY, label: "Seco / Ambiente" },
  { value: TRANSPORT_ENVIRONMENT.CHILLED, label: "Resfriado (Positivo)" },
  { value: TRANSPORT_ENVIRONMENT.FROZEN, label: "Congelado (Negativo)" },
]

/** Packaging type wire values (`tipoEmbalagem`) — required only for "Geral". */
export const PACKAGING_TYPE = {
  PALLETIZED: "Paletizado",
  MASTER_CARTONS: "Caixas Master",
  BAGS_SACKS: "Sacaria/Bag",
}

export const PACKAGING_TYPE_OPTIONS = [
  { value: PACKAGING_TYPE.PALLETIZED, label: "Paletizado" },
  { value: PACKAGING_TYPE.MASTER_CARTONS, label: "Caixas Master" },
  { value: PACKAGING_TYPE.BAGS_SACKS, label: "Sacaria / Bag" },
]

/**
 * True when the given category requires a packaging type. The backend enforces
 * `tipoEmbalagem` only when category is "Geral".
 */
export function requiresPackaging(category) {
  return category === PRODUCT_CATEGORY.GENERAL
}

/**
 * True when the given transport environment requires a temperature range. The
 * backend requires `tempMin`/`tempMax` for anything other than "Seco".
 */
export function requiresTemperature(environment) {
  return Boolean(environment) && environment !== TRANSPORT_ENVIRONMENT.DRY
}

/**
 * Maps backend validation field names (Portuguese) to the form field keys used
 * by the product screens, so inline errors land on the right input.
 */
export const PRODUCT_FIELD_MAP = {
  sku: "sku",
  nome: "name",
  categoria: "category",
  ambienteTransporte: "transportEnvironment",
  tempMin: "tempMin",
  tempMax: "tempMax",
  tipoEmbalagem: "packagingType",
  pesoPadrao: "defaultWeight",
  volumePadrao: "defaultVolume",
}

/**
 * Remaps a `{ [backendField]: message }` error object (Portuguese field names)
 * to the form field keys used by the product screens.
 */
export function mapProductFieldErrors(fieldErrors) {
  if (!fieldErrors) {
    return {}
  }

  const mapped = {}
  for (const [field, message] of Object.entries(fieldErrors)) {
    const key = PRODUCT_FIELD_MAP[field] ?? field
    if (!mapped[key]) {
      mapped[key] = message
    }
  }
  return mapped
}

function findLabel(options, value) {
  return options.find((option) => option.value === value)?.label ?? value
}

export const categoryLabel = (value) =>
  findLabel(PRODUCT_CATEGORY_OPTIONS, value)

export const environmentLabel = (value) =>
  findLabel(TRANSPORT_ENVIRONMENT_OPTIONS, value)

export const packagingLabel = (value) =>
  value ? findLabel(PACKAGING_TYPE_OPTIONS, value) : ""
