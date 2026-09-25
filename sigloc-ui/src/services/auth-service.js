/**
 * Authentication service — the single boundary between the UI and the Sigloc
 * auth API. Each function maps 1:1 to a backend endpoint and returns a
 * normalized session object: `{ token, user }`.
 *
 * The `user` shape from the API is:
 *   { id, email, profileType, companyId }
 * We enrich it with an app-level `role` derived from `profileType` so callers
 * never need to know the backend vocabulary.
 */

import { apiClient } from "@/lib/api-client"
import { roleFromProfileType } from "@/constants/roles"

const ENDPOINTS = {
  login: "/api/auth/login",
  loginGoogle: "/api/auth/login/google",
  registerContractor: "/api/auth/register/contratante",
  registerContractorGoogle: "/api/auth/register/contratante/google",
}

/**
 * Normalizes the raw API auth payload into the session object consumed by the
 * app, attaching a derived `role`.
 */
function deriveDisplayName(user) {
  if (typeof user?.email === "string" && user.email.includes("@")) {
    return user.email.split("@")[0]
  }

  return "Usuário"
}

function toSession(payload) {
  const user = payload?.user ?? {}

  return {
    token: payload?.token ?? null,
    user: {
      id: user.id ?? null,
      email: user.email ?? null,
      companyId: user.companyId ?? null,
      profileType: user.profileType ?? null,
      role: roleFromProfileType(user.profileType),
      name: deriveDisplayName(user),
    },
  }
}

/**
 * Authenticate with corporate email + password.
 * @param {{ email: string, password: string }} credentials
 */
export async function login(credentials, options) {
  const payload = await apiClient.post(ENDPOINTS.login, credentials, {
    auth: false,
    ...options,
  })
  return toSession(payload)
}

/**
 * Authenticate with a Google-issued idToken.
 * @param {{ idToken: string }} params
 */
export async function loginWithGoogle(params, options) {
  const payload = await apiClient.post(ENDPOINTS.loginGoogle, params, {
    auth: false,
    ...options,
  })
  return toSession(payload)
}

/**
 * Register a new contractor company with email + password.
 * @param {{ cnpj, companyName, tradeName, email, password }} data
 */
export async function registerContractor(data, options) {
  const payload = await apiClient.post(ENDPOINTS.registerContractor, data, {
    auth: false,
    ...options,
  })
  return toSession(payload)
}

/**
 * Register a new contractor company using a Google-issued idToken.
 * @param {{ idToken, cnpj, companyName, tradeName }} data
 */
export async function registerContractorWithGoogle(data, options) {
  const payload = await apiClient.post(
    ENDPOINTS.registerContractorGoogle,
    data,
    { auth: false, ...options }
  )
  return toSession(payload)
}

export const authService = {
  login,
  loginWithGoogle,
  registerContractor,
  registerContractorWithGoogle,
}
