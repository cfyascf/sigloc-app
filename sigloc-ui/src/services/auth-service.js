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
  invite: "/api/auth/invite",
  activeInvite: "/api/auth/invite/active",
}

/**
 * Extracts the raw invite token from either a full invite URL
 * (e.g. `https://sigloc.app/invite/SIG-4821`) or a bare code the user pastes.
 * Returns the trimmed input when no URL path segment is found.
 */
export function extractInviteToken(input) {
  const value = (input ?? "").trim()
  if (!value) {
    return ""
  }

  const withoutQuery = value.split(/[?#]/)[0]
  const segments = withoutQuery.split("/").filter(Boolean)
  return segments.length > 0 ? segments[segments.length - 1] : value
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

/**
 * Normalizes an invite payload (`ActiveInviteDto` or `InviteCreatedDto`) into a
 * single UI shape. The two backend DTOs differ slightly, so we read both spellings.
 */
function toInvite(payload) {
  if (!payload) {
    return null
  }

  return {
    id: payload.id ?? null,
    code: payload.codigo ?? payload.token ?? null,
    link: payload.linkCompleto ?? payload.inviteLink ?? null,
    expiresAt: payload.expiraEm ?? payload.expiresAt ?? null,
    hoursRemaining: payload.horasRestantes ?? null,
  }
}

/**
 * Fetches the contractor's most recent active (not expired, not used) invite.
 * Resolves to `null` when the backend returns 204 (no active invite).
 */
export async function getActiveInvite(options) {
  const payload = await apiClient.get(ENDPOINTS.activeInvite, options)
  return toInvite(payload)
}

/**
 * Creates a new smart invite for the authenticated contractor.
 * @param {{ inviteeEmail?: string, expiresInDays?: number }} [data]
 */
export async function createInvite(data = {}, options) {
  const payload = await apiClient.post(ENDPOINTS.invite, data, options)
  return toInvite(payload)
}

/**
 * Connects the authenticated carrier to the contractor that issued the invite,
 * creating an active partnership. Accepts a full invite URL or a bare code.
 * @param {string} tokenOrUrl
 * @returns {Promise<{ connectionId, contractorName, status } | null>}
 */
export async function connectByInvite(tokenOrUrl, options) {
  const token = extractInviteToken(tokenOrUrl)
  const payload = await apiClient.post(
    `${ENDPOINTS.invite}/${encodeURIComponent(token)}/connect`,
    {},
    options
  )

  if (!payload) {
    return null
  }

  return {
    connectionId: payload.conexaoId ?? null,
    contractorName: payload.contratante ?? null,
    status: payload.statusParceria ?? null,
  }
}

export const authService = {
  login,
  loginWithGoogle,
  registerContractor,
  registerContractorWithGoogle,
  getActiveInvite,
  createInvite,
  connectByInvite,
}
