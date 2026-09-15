export const ROLES = {
  CONTRACTOR: "contractor",
  CARRIER: "carrier",
  DEVELOPER: "developer",
}

export const ROLE_OPTIONS = [
  { value: ROLES.CONTRACTOR, label: "Contratante" },
  { value: ROLES.CARRIER, label: "Transportador" },
  { value: ROLES.DEVELOPER, label: "Desenvolvedor" },
]

/**
 * Maps the backend `profileType` (returned by the auth endpoints) to the app's
 * internal role. Keeps the domain vocabulary of the API isolated from the UI.
 */
const PROFILE_TYPE_TO_ROLE = {
  CONTRATANTE: ROLES.CONTRACTOR,
  TRANSPORTADORA: ROLES.CARRIER,
  ADMINISTRADOR: ROLES.DEVELOPER,
}

/**
 * Resolves an app role from a backend `profileType`, falling back to CONTRACTOR
 * for unknown values so the user always lands on a valid workspace.
 */
export function roleFromProfileType(profileType) {
  if (typeof profileType !== "string") {
    return ROLES.CONTRACTOR
  }

  return PROFILE_TYPE_TO_ROLE[profileType.toUpperCase()] ?? ROLES.CONTRACTOR
}
