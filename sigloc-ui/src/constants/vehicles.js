/**
 * Vehicle enum constants — the single source of truth for translating between
 * the backend's integer-encoded enums and the PT-BR labels the UI displays.
 *
 * The Sigloc API does not register a `JsonStringEnumConverter`, so
 * `bodyType`, `refrigerationLevel` and `status` travel over the wire as
 * integers. These maps let the vehicle service convert int -> label for display
 * and label -> int when sending updates.
 */

/** Body type labels (the backend `VehicleBodyType` [Description]s). */
export const BODY_TYPE = {
  BAU: "Baú",
  SIDER: "Sider",
  GRADE_BAIXA: "Grade Baixa",
  REFRIGERATED: "Baú Refrigerado",
  CACAMBA: "Caçamba",
  GRANELEIRO: "Graneleiro",
  PRANCHA: "Prancha",
  TANQUE: "Tanque",
  CEGONHEIRO: "Cegonheiro",
  PORTA_CONTEINER: "Porta-Contêiner",
  SILO: "Silo",
  GAIOLA: "Gaiola / Boiadeiro",
}

/** VehicleBodyType (backend enum, 1-based). Values never change; new body types are appended. */
export const BODY_TYPE_BY_VALUE = {
  1: BODY_TYPE.BAU,
  2: BODY_TYPE.SIDER,
  3: BODY_TYPE.GRADE_BAIXA,
  4: BODY_TYPE.REFRIGERATED,
  5: BODY_TYPE.CACAMBA,
  6: BODY_TYPE.GRANELEIRO,
  7: BODY_TYPE.PRANCHA,
  8: BODY_TYPE.TANQUE,
  9: BODY_TYPE.CEGONHEIRO,
  10: BODY_TYPE.PORTA_CONTEINER,
  11: BODY_TYPE.SILO,
  12: BODY_TYPE.GAIOLA,
}

/** VehicleComposition (backend enum, 1-based). Anything but a single truck needs CNH E. */
export const COMPOSITION_BY_VALUE = {
  1: "Caminhão simples",
  2: "Cavalo + carreta",
  3: "Caminhão + reboque",
  4: "Bitrem",
  5: "Rodotrem",
}
export const RIGID_COMPOSITION = 1

/** Whose RNTRC (ANTT) the vehicle runs under (backend RntrcSource). */
export const RNTRC_SOURCE = { CARRIER: 1, THIRD_PARTY: 2 }

/** RntrcCategory (backend enum, 1-based). A TAC is a person (CPF); ETC and CTC have a CNPJ. */
export const RNTRC_CATEGORY_BY_VALUE = {
  1: "TAC (Transportador Autônomo)",
  2: "ETC (Empresa de Transporte)",
  3: "CTC (Cooperativa de Transporte)",
}
export const RNTRC_CATEGORY = { TAC: 1, ETC: 2, CTC: 3 }

/** CnhCategory (backend enum, 1-based). */
export const CNH_CATEGORY_BY_VALUE = { 1: "C", 2: "D", 3: "E" }
export const CNH_E = 3

/** RefrigerationLevel (backend enum, 0-based). */
export const REFRIGERATION_BY_VALUE = {
  0: "Nenhuma",
  1: "Resfriado",
  2: "Congelado",
}

/**
 * OperationalStatus (backend enum, 0-based). 1 (in transit set by hand) is retired: "Em viagem"
 * comes from the vehicle's active trip, so it is only a label for old records, never an option.
 */
export const ON_TRIP_LABEL = "Em viagem"
export const STATUS_BY_VALUE = {
  0: "Livre",
  1: ON_TRIP_LABEL,
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
export const STATUS_OPTIONS = [STATUS_BY_VALUE[0], STATUS_BY_VALUE[2]]
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
