/**
 * Thin, framework-agnostic HTTP client for the Sigloc API.
 *
 * Responsibilities:
 *   - Prefix requests with the configured API base URL.
 *   - Serialize/parse JSON and set the right headers.
 *   - Attach the bearer token when one is available.
 *   - Normalize every failure into a single {@link ApiError} shape, regardless
 *     of whether the backend returned `{ error, message }` or an RFC 9457
 *     `application/problem+json` validation payload.
 *
 * Everything here is dependency-free so it can be reused by any service module
 * and unit-tested without React.
 */

import { API_BASE_URL } from "@/config/env"

/**
 * Error thrown for any non-2xx response (or a network/parse failure).
 * Carries a machine-readable `code`, a user-facing `message`, the HTTP
 * `status`, and optional field-level `details`.
 */
export class ApiError extends Error {
  constructor(message, { code, status, details } = {}) {
    super(message)
    this.name = "ApiError"
    this.code = code ?? "UNKNOWN_ERROR"
    this.status = status ?? 0
    this.details = details ?? null
  }
}

// Resolves the bearer token lazily so the client stays decoupled from where the
// token is stored (context, localStorage, tests, …).
let tokenProvider = () => null

/**
 * Registers the function used to resolve the current auth token.
 * Called once during app bootstrap by the auth layer.
 */
export function setAuthTokenProvider(provider) {
  tokenProvider = typeof provider === "function" ? provider : () => null
}

function buildUrl(path) {
  if (/^https?:\/\//i.test(path)) {
    return path
  }

  return `${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`
}

async function parseBody(response) {
  const contentType = response.headers.get("content-type") ?? ""

  if (response.status === 204 || !contentType) {
    return null
  }

  if (contentType.includes("json")) {
    try {
      return await response.json()
    } catch {
      return null
    }
  }

  try {
    return await response.text()
  } catch {
    return null
  }
}

/**
 * Flattens an RFC 9457 `errors` map ({ field: [messages] }) into a flat list of
 * `{ field, message }` objects used by the UI for field-level feedback.
 */
function normalizeProblemDetails(body) {
  const details = []

  if (body?.errors && typeof body.errors === "object") {
    for (const [field, messages] of Object.entries(body.errors)) {
      const list = Array.isArray(messages) ? messages : [messages]
      for (const message of list) {
        details.push({ field, message })
      }
    }
  }

  const message =
    details[0]?.message ||
    body?.title ||
    "Ocorreu um erro ao processar a solicitação."

  return { message, details }
}

/**
 * Flattens the domain validation payload used by resource endpoints
 * (`details: [{ field, reason }]`) into the `{ field, message }` shape the UI
 * consumes for inline field errors.
 */
function normalizeDomainDetails(body) {
  if (!Array.isArray(body?.details)) {
    return []
  }

  return body.details
    .filter((detail) => detail && typeof detail === "object")
    .map((detail) => ({
      field: detail.field ?? detail.Field ?? null,
      message: detail.reason ?? detail.message ?? detail.Reason ?? "",
    }))
    .filter((detail) => detail.field)
}

function toApiError(status, body) {
  // Backend domain errors: { error: "CODE", message: "...", details?: [...] }
  if (body && typeof body === "object" && (body.error || body.message)) {
    if (body.errors && typeof body.errors === "object") {
      const { message, details } = normalizeProblemDetails(body)
      return new ApiError(message, { code: "VALIDATION_ERROR", status, details })
    }

    const domainDetails = normalizeDomainDetails(body)

    return new ApiError(body.message || "Erro inesperado.", {
      code: body.error,
      status,
      details: domainDetails.length > 0 ? domainDetails : null,
    })
  }

  // RFC 9457 problem+json without a top-level `error`/`message`.
  if (body?.errors || body?.title) {
    const { message, details } = normalizeProblemDetails(body)
    return new ApiError(message, { code: "VALIDATION_ERROR", status, details })
  }

  return new ApiError("Ocorreu um erro inesperado. Tente novamente.", {
    code: "UNKNOWN_ERROR",
    status,
  })
}

/**
 * Performs an HTTP request and returns the parsed JSON body.
 *
 * @param {string} path Absolute URL or path relative to the API base URL.
 * @param {object} [options]
 * @param {string} [options.method="GET"]
 * @param {unknown} [options.body] JSON-serializable request body.
 * @param {boolean} [options.auth=true] Attach the bearer token when available.
 * @param {object} [options.headers] Extra headers.
 * @param {AbortSignal} [options.signal]
 * @throws {ApiError}
 */
export async function apiRequest(
  path,
  { method = "GET", body, auth = true, headers = {}, signal } = {}
) {
  const requestHeaders = { Accept: "application/json", ...headers }

  if (body !== undefined) {
    requestHeaders["Content-Type"] = "application/json"
  }

  if (auth) {
    const token = tokenProvider()
    if (token) {
      requestHeaders.Authorization = `Bearer ${token}`
    }
  }

  let response
  try {
    response = await fetch(buildUrl(path), {
      method,
      headers: requestHeaders,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    })
  } catch (error) {
    if (error?.name === "AbortError") {
      throw error
    }

    throw new ApiError(
      "Não foi possível conectar ao servidor. Verifique sua conexão.",
      { code: "NETWORK_ERROR", status: 0 }
    )
  }

  const parsed = await parseBody(response)

  if (!response.ok) {
    throw toApiError(response.status, parsed)
  }

  return parsed
}

export const apiClient = {
  get: (path, options) => apiRequest(path, { ...options, method: "GET" }),
  post: (path, body, options) =>
    apiRequest(path, { ...options, method: "POST", body }),
  put: (path, body, options) =>
    apiRequest(path, { ...options, method: "PUT", body }),
  patch: (path, body, options) =>
    apiRequest(path, { ...options, method: "PATCH", body }),
  delete: (path, options) => apiRequest(path, { ...options, method: "DELETE" }),
}
