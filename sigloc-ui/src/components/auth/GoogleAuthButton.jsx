/* eslint-disable react/prop-types */
import { GoogleLogin } from "@react-oauth/google"

import { isGoogleAuthEnabled } from "@/config/env"

/**
 * Reusable Google sign-in button.
 *
 * Wraps `@react-oauth/google`'s `GoogleLogin`, which uses the credential flow
 * and yields a Google-issued `idToken` (the `credential` field) — exactly what
 * the Sigloc backend expects for `/login/google` and `/register/.../google`.
 *
 * When no Client ID is configured (`VITE_GOOGLE_CLIENT_ID`), a disabled
 * placeholder is rendered instead so the layout stays intact.
 *
 * @param {(idToken: string) => void} onCredential Called with the idToken.
 * @param {(error?: unknown) => void} [onError]
 * @param {string} [text="continue_with"] Google button label variant.
 * @param {boolean} [disabled=false]
 */
export function GoogleAuthButton({
  onCredential,
  onError,
  text = "continue_with",
  disabled = false,
}) {
  if (!isGoogleAuthEnabled) {
    return (
      <button
        type="button"
        disabled
        title="Configure VITE_GOOGLE_CLIENT_ID para habilitar o login com Google"
        className="flex h-9 w-full cursor-not-allowed items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white text-sm font-medium text-slate-400"
      >
        <GoogleGlyph className="opacity-40" />
        Continuar com Google
      </button>
    )
  }

  return (
    <div
      className={
        disabled ? "pointer-events-none opacity-60" : undefined
      }
      aria-disabled={disabled}
    >
      <GoogleLogin
        onSuccess={(response) => {
          if (response?.credential) {
            onCredential(response.credential)
          } else {
            onError?.(new Error("Credencial do Google ausente."))
          }
        }}
        onError={() => onError?.(new Error("Falha na autenticação com o Google."))}
        text={text}
        width="100%"
        theme="outline"
        shape="rectangular"
        logo_alignment="center"
      />
    </div>
  )
}

function GoogleGlyph({ className }) {
  return (
    <svg
      className={className}
      width="16"
      height="16"
      viewBox="0 0 48 48"
      aria-hidden="true"
    >
      <path
        fill="#EA4335"
        d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"
      />
      <path
        fill="#4285F4"
        d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"
      />
      <path
        fill="#FBBC05"
        d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"
      />
      <path
        fill="#34A853"
        d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"
      />
    </svg>
  )
}

export default GoogleAuthButton
