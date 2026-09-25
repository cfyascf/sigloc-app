import { useState } from "react"
import { Link } from "react-router-dom"

import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { usePostAuthRedirect } from "@/hooks/use-post-auth-redirect"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"
import { AuthDivider } from "@/components/auth/AuthDivider"
import { GoogleAuthButton } from "@/components/auth/GoogleAuthButton"

export function LoginForm() {
  const { login, loginWithGoogle } = useAuth()
  const redirect = usePostAuthRedirect()

  const [form, setForm] = useState({ email: "", password: "" })
  const [googleError, setGoogleError] = useState(null)

  const credentialAction = useAsyncAction(login)
  const googleAction = useAsyncAction(loginWithGoogle)

  const pending = credentialAction.pending || googleAction.pending
  const errorMessage =
    credentialAction.error?.message || googleAction.error?.message || googleError

  const handleChange = (event) => {
    const { id, value } = event.target
    setForm((prev) => ({ ...prev, [id]: value }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const result = await credentialAction.run({
      email: form.email.trim(),
      password: form.password,
    })
    if (result.ok) {
      redirect(result.data.user.role)
    }
  }

  const handleGoogle = async (idToken) => {
    setGoogleError(null)
    const result = await googleAction.run({ idToken })
    if (result.ok) {
      redirect(result.data.user.role)
    }
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center lg:text-left">
        <h2 className="text-2xl font-bold tracking-tight text-slate-900">
          Bem-vindo de volta
        </h2>
        <p className="text-sm text-slate-500">
          Insira suas credenciais para acessar o painel.
        </p>
      </div>

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

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <label
              className="text-sm font-medium text-slate-700"
              htmlFor="password"
            >
              Senha
            </label>
            <Link
              to="/auth"
              className="text-xs font-medium text-blue-600 hover:underline"
            >
              Esqueceu a senha?
            </Link>
          </div>
          <FormField
            id="password"
            label={null}
            type="password"
            autoComplete="current-password"
            required
            value={form.password}
            onChange={handleChange}
            error={credentialAction.fieldErrors.password}
          />
        </div>

        <Button
          type="submit"
          disabled={pending}
          className="mt-2 w-full bg-blue-600 text-white hover:bg-blue-700"
        >
          {credentialAction.pending ? "Entrando..." : "Entrar no Sistema"}
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

export default LoginForm
