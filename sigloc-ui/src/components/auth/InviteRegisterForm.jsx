/* eslint-disable react/prop-types */
import { useMemo, useState } from "react"

import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { formatCnpj, onlyDigits } from "@/lib/formatters"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"
import { AuthDivider } from "@/components/auth/AuthDivider"
import { GoogleAuthButton } from "@/components/auth/GoogleAuthButton"

const EMPTY_COMPANY = { cnpj: "", companyName: "", tradeName: "" }

/**
 * Carrier onboarding form used inside the invite landing page. Registers a new
 * carrier through the invite token and creates the partnership with the inviting
 * contractor. On success, calls `onSuccess(session)`.
 *
 * When the backend reports the carrier already exists (CarrierAlreadyRegistered),
 * `onExistingCarrier` is invoked so the page can steer the user to login.
 */
export function InviteRegisterForm({ token, onSuccess, onExistingCarrier }) {
  const { registerCarrierByInvite, registerCarrierByInviteWithGoogle } = useAuth()

  const [company, setCompany] = useState(EMPTY_COMPANY)
  const [credentials, setCredentials] = useState({ email: "", password: "" })
  const [googleError, setGoogleError] = useState(null)

  const credentialAction = useAsyncAction((data) => registerCarrierByInvite(token, data))
  const googleAction = useAsyncAction((data) => registerCarrierByInviteWithGoogle(token, data))

  const pending = credentialAction.pending || googleAction.pending
  const fieldErrors = {
    ...credentialAction.fieldErrors,
    ...googleAction.fieldErrors,
  }
  const errorMessage =
    credentialAction.error?.message || googleAction.error?.message || googleError

  const companyComplete = useMemo(
    () =>
      company.cnpj.trim() !== "" &&
      company.companyName.trim() !== "" &&
      company.tradeName.trim() !== "",
    [company]
  )

  const handleCompanyChange = (event) => {
    const { id, value } = event.target
    setCompany((prev) => ({
      ...prev,
      [id]: id === "cnpj" ? formatCnpj(value) : value,
    }))
  }

  const handleCredentialsChange = (event) => {
    const { id, value } = event.target
    setCredentials((prev) => ({ ...prev, [id]: value }))
  }

  const companyPayload = () => ({
    cnpj: onlyDigits(company.cnpj),
    companyName: company.companyName.trim(),
    tradeName: company.tradeName.trim(),
  })

  // The carrier may already exist — the backend aborts with a 409 and asks the
  // front-end to redirect to login. Surface that to the parent page.
  const handleResult = (result) => {
    if (result.ok) {
      onSuccess?.(result.data)
      return
    }

    if (result.error?.code === "TRANSPORTADORA_JA_CADASTRADA") {
      onExistingCarrier?.(result.error.message)
    }
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const result = await credentialAction.run({
      ...companyPayload(),
      email: credentials.email.trim(),
      password: credentials.password,
    })
    handleResult(result)
  }

  const handleGoogle = async (idToken) => {
    setGoogleError(null)
    if (!companyComplete) {
      setGoogleError(
        "Preencha CNPJ, razão social e nome fantasia antes de continuar com o Google."
      )
      return
    }

    const result = await googleAction.run({ idToken, ...companyPayload() })
    handleResult(result)
  }

  return (
    <div className="space-y-6">
      <FormAlert message={errorMessage} />

      <form noValidate onSubmit={handleSubmit} className="space-y-4">
        <FormField
          id="cnpj"
          label="CNPJ"
          type="text"
          inputMode="numeric"
          placeholder="00.000.000/0000-00"
          required
          value={company.cnpj}
          onChange={handleCompanyChange}
          error={fieldErrors.cnpj}
        />

        <FormField
          id="companyName"
          label="Razão Social"
          type="text"
          placeholder="Transportadora LTDA"
          required
          value={company.companyName}
          onChange={handleCompanyChange}
          error={fieldErrors.companyName}
        />

        <FormField
          id="tradeName"
          label="Nome Fantasia"
          type="text"
          placeholder="Nome comercial"
          required
          value={company.tradeName}
          onChange={handleCompanyChange}
          error={fieldErrors.tradeName}
        />

        <FormField
          id="email"
          label="Email Corporativo"
          type="email"
          placeholder="nome@empresa.com"
          autoComplete="email"
          required
          value={credentials.email}
          onChange={handleCredentialsChange}
          error={fieldErrors.email}
        />

        <FormField
          id="password"
          label="Senha"
          type="password"
          autoComplete="new-password"
          required
          value={credentials.password}
          onChange={handleCredentialsChange}
          error={fieldErrors.password}
        />

        <Button
          type="submit"
          disabled={pending}
          className="mt-2 w-full bg-slate-900 text-white hover:bg-slate-800"
        >
          {credentialAction.pending ? "Criando conta..." : "Criar Conta e Conectar"}
        </Button>
      </form>

      <AuthDivider />

      <GoogleAuthButton
        text="signup_with"
        onCredential={handleGoogle}
        onError={(err) =>
          setGoogleError(err?.message || "Falha na autenticação com o Google.")
        }
        disabled={pending}
      />
      <p className="text-center text-[11px] text-slate-400">
        Para cadastrar com o Google, preencha os dados da empresa acima.
      </p>
    </div>
  )
}

export default InviteRegisterForm
