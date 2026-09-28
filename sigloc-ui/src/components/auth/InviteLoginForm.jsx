/* eslint-disable react/prop-types */
import { useState } from "react"

import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { ROLES } from "@/constants/roles"
import { authService } from "@/services/auth-service"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"
import { AuthDivider } from "@/components/auth/AuthDivider"
import { GoogleAuthButton } from "@/components/auth/GoogleAuthButton"

/**
 * Login form used inside the invite landing page. After a successful login as a
 * carrier, it consumes the invite token to create the partnership, then calls
 * `onConnected(contractorName)`. A contractor logging in here is rejected with a
 * message, since invites are meant for carriers.
 */
export function InviteLoginForm({ token, onConnected }) {
  const { login, loginWithGoogle } = useAuth()

  const [form, setForm] = useState({ email: "", password: "" })
  const [googleError, setGoogleError] = useState(null)
  const [wrongRole, setWrongRole] = useState(null)

  const credentialAction = useAsyncAction(login)
  const googleAction = useAsyncAction(loginWithGoogle)
  const connectAction = useAsyncAction(() => authService.connectByInvite(token))

  const pending =
    credentialAction.pending || googleAction.pending || connectAction.pending
  const errorMessage =
    credentialAction.error?.message ||
    googleAction.error?.message ||
    connectAction.error?.message ||
    googleError ||
    wrongRole

  const handleChange = (event) => {
    const { id, value } = event.target
    setForm((prev) => ({ ...prev, [id]: value }))
  }

  // After authenticating, only carriers can accept an invite; then consume the
  // token to create the partnership.
  const finishConnection = async (session) => {
    if (session?.user?.role !== ROLES.CARRIER) {
      setWrongRole(
        "Este convite é destinado a transportadoras. Entre com uma conta de transportadora."
      )
      return
    }

    const result = await connectAction.run()
    if (result.ok || result.error?.code === "PARCERIA_JA_EXISTE") {
      onConnected?.(result.data?.contractorName ?? null)
    }
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setWrongRole(null)
    const result = await credentialAction.run({
      email: form.email.trim(),
      password: form.password,
    })
    if (result.ok) {
      await finishConnection(result.data)
    }
  }

  const handleGoogle = async (idToken) => {
    setGoogleError(null)
    setWrongRole(null)
    const result = await googleAction.run({ idToken })
    if (result.ok) {
      await finishConnection(result.data)
    }
  }

  return (
    <div className="space-y-6">
      <FormAlert message={errorMessage} />

      <form noValidate onSubmit={handleSubmit} className="space-y-4">
        <FormField
          id="email"
          label="Email Corporativo"
          type="email"
          placeholder="nome@empresa.com"
          autoComplete="email"
          required
          value={form.email}
          onChange={handleChange}
          error={credentialAction.fieldErrors.email}
        />

        <FormField
          id="password"
          label="Senha"
          type="password"
          autoComplete="current-password"
          required
          value={form.password}
          onChange={handleChange}
          error={credentialAction.fieldErrors.password}
        />

        <Button
          type="submit"
          disabled={pending}
          className="mt-2 w-full bg-blue-600 text-white hover:bg-blue-700"
        >
          {pending ? "Conectando..." : "Entrar e Conectar"}
        </Button>
      </form>

      <AuthDivider />

      <GoogleAuthButton
        onCredential={handleGoogle}
        onError={(err) =>
          setGoogleError(err?.message || "Falha na autenticação com o Google.")
        }
        disabled={pending}
      />
    </div>
  )
}

export default InviteLoginForm
