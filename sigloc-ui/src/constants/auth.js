export const Auth = {
  PUBLIC: "public",
  AUTHENTICATED: "authenticated"
}

export function getDefaultRouteForRole(role) {
  if (role === "carrier") {
    return "/carrier-dashboard"
  }
  if (role === "contractor") {
    return "/contractor-dashboard"
  }
  // Developer ou qualquer outra role vai para contractor dashboard como fallback
  return "/contractor-dashboard"
}

// The auth API reports some fields under their Portuguese names; the forms use English ids.
const AUTH_FIELD_MAP = {
  razaoSocial: "companyName",
  nomeFantasia: "tradeName",
  senha: "password",
}

/** Re-keys API field errors (`{ razaoSocial: "…" }`) to the auth form field ids. */
export function mapAuthFieldErrors(fieldErrors) {
  const mapped = {}
  for (const [field, message] of Object.entries(fieldErrors ?? {})) {
    const key = AUTH_FIELD_MAP[field] ?? field
    if (!mapped[key]) mapped[key] = message
  }
  return mapped
}

export const AUTH_OPTIONS = [
  { value: Auth.PUBLIC, label: "Público" },
  { value: Auth.AUTHENTICATED, label: "Autenticado" },
]
