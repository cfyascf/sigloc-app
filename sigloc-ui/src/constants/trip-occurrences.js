/** Occurrence types a carrier can report; severity mirrors TripOccurrenceMappings / TripRiskEvaluator in the API. */
export const TRIP_OCCURRENCE_TYPES = [
  { value: "TRANSITO_INTENSO", label: "Trânsito Intenso", severity: "WARNING" },
  { value: "FALTA_DOCUMENTACAO", label: "Falta de Documentação", severity: "WARNING" },
  { value: "AVARIA_MECANICA", label: "Avaria Mecânica", severity: "CRITIC" },
  { value: "RISCO_SLA", label: "Risco de Extrapolar SLA", severity: "CRITIC" },
]

export const TRIP_NOTE_MAX_LENGTH = 500

export const PROOF_MAX_BYTES = 10 * 1024 * 1024
export const PROOF_ACCEPT = ".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf"
