/**
 * Centralized, validated access to Vite environment variables.
 *
 * Keeping every `import.meta.env` lookup here means the rest of the app never
 * touches raw env vars directly. That makes the surface easy to audit, mock in
 * tests, and evolve without hunting through the codebase.
 */

const DEFAULT_API_BASE_URL =
  "https://sigloc-api-hbfzcfd6ghephmcc.centralus-01.azurewebsites.net"

function readString(value, fallback = "") {
  if (typeof value !== "string") {
    return fallback
  }

  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : fallback
}

/**
 * Base URL of the Sigloc API. Trailing slashes are stripped so callers can
 * safely concatenate paths that start with "/".
 */
export const API_BASE_URL = readString(
  import.meta.env.VITE_API_BASE_URL,
  DEFAULT_API_BASE_URL
).replace(/\/+$/, "")

/**
 * Google OAuth Client ID. Empty when unset — consumers should treat an empty
 * value as "Google auth is not configured" and degrade gracefully.
 */
export const GOOGLE_CLIENT_ID = readString(import.meta.env.VITE_GOOGLE_CLIENT_ID)

/** Whether Google-based auth flows can be offered to the user. */
export const isGoogleAuthEnabled = GOOGLE_CLIENT_ID.length > 0
