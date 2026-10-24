export const RISK = {
  NORMAL: "NORMAL",
  WARNING: "WARNING",
  CRITIC: "CRITIC",
}

const RISK_LABELS = {
  [RISK.NORMAL]: "Normal",
  [RISK.WARNING]: "Atenção",
  [RISK.CRITIC]: "Crítico",
}

/** Portuguese label for a risk code; unknown codes are shown as received. */
export const riskLabel = (risk) => RISK_LABELS[risk] ?? risk

/** Risk filter options, lowest to highest. */
export const RISK_OPTIONS = Object.values(RISK).map((value) => ({ value, label: riskLabel(value) }))
