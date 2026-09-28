/**
 * Vehicle enum constants — the single source of truth for translating between
 * the backend's integer-encoded enums and the PT-BR labels the UI displays.
 *
 * The Sigloc API does not register a `JsonStringEnumConverter`, so
 * `bodyType`, `refrigerationLevel` and `status` travel over the wire as
 * integers. These maps let the vehicle service convert int -> label for display
 * and label -> int when sending updates.
 */

/** VehicleBodyType (backend enum, 1-based). */
export const BODY_TYPE_BY_VALUE = {
  1: "Carga Seca",
  2: "Baú Sider",
  3: "Grade Baixa",
  4: "Frigorífico",
  5: "Carreta Prancha",
}

/** RefrigerationLevel (backend enum, 0-based). */
export const REFRIGERATION_BY_VALUE = {
  0: "Nenhuma",
  1: "Resfriado",
  2: "Congelado",
}

/** OperationalStatus (backend enum, 0-based). */
export const STATUS_BY_VALUE = {
  0: "Livre",
  1: "Em Trânsito",
  2: "Manutenção",
}

/** Reverse maps (label -> integer value), built from the forward maps. */
function invert(map) {
  return Object.fromEntries(
    Object.entries(map).map(([value, label]) => [label, Number(value)])
  )
}

export const BODY_TYPE_BY_LABEL = invert(BODY_TYPE_BY_VALUE)
export const REFRIGERATION_BY_LABEL = invert(REFRIGERATION_BY_VALUE)
export const STATUS_BY_LABEL = invert(STATUS_BY_VALUE)

/** Options for the Select controls, in display order. */
export const BODY_TYPE_OPTIONS = Object.values(BODY_TYPE_BY_VALUE)
export const STATUS_OPTIONS = Object.values(STATUS_BY_VALUE)
export const REFRIGERATION_OPTIONS = Object.values(REFRIGERATION_BY_VALUE)

/** int -> label helpers (fall back to empty string for unknown values). */
export const bodyTypeLabel = (value) => BODY_TYPE_BY_VALUE[value] ?? ""
export const refrigerationLabel = (value) => REFRIGERATION_BY_VALUE[value] ?? ""
export const statusLabel = (value) => STATUS_BY_VALUE[value] ?? ""

/** label -> int helpers (return null when the label is unknown). */
export const bodyTypeValue = (label) =>
  label in BODY_TYPE_BY_LABEL ? BODY_TYPE_BY_LABEL[label] : null
export const refrigerationValue = (label) =>
  label in REFRIGERATION_BY_LABEL ? REFRIGERATION_BY_LABEL[label] : null
export const statusValue = (label) =>
  label in STATUS_BY_LABEL ? STATUS_BY_LABEL[label] : null
