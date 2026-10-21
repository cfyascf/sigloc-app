import { useState } from "react"
import { Building2, KeyRound, LoaderCircle } from "lucide-react"

import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { usePostAuthRedirect } from "@/hooks/use-post-auth-redirect"
import { formatCnpj, onlyDigits } from "@/lib/formatters"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"
import { GoogleAuthButton } from "@/components/auth/GoogleAuthButton"

const EMPTY_COMPANY = { cnpj: "", companyName: "", tradeName: "" }

export function RegisterForm() {
  const { registerContractor, registerContractorWithGoogle } = useAuth()
  const redirect = usePostAuthRedirect()
  const [method, setMethod] = useState(null)
  const [company, setCompany] = useState(EMPTY_COMPANY)
  const [credentials, setCredentials] = useState({ email: "", password: "" })
  const [googleToken, setGoogleToken] = useState(null)
  const [googleError, setGoogleError] = useState(null)

  const credentialAction = useAsyncAction(registerContractor)
  const googleAction = useAsyncAction(registerContractorWithGoogle)
  const pending = credentialAction.pending || googleAction.pending
  const fieldErrors = { ...credentialAction.fieldErrors, ...googleAction.fieldErrors }
  const errorMessage = credentialAction.error?.message || googleAction.error?.message || googleError

  const handleCompanyChange = (event) => {
    const { id, value } = event.target
    setCompany((previous) => ({
      ...previous,
      [id]: id === "cnpj" ? formatCnpj(value) : value,
    }))
  }

  const handleCredentialsChange = (event) => {
    const { id, value } = event.target
    setCredentials((previous) => ({ ...previous, [id]: value }))
  }

  const companyPayload = () => ({
    cnpj: onlyDigits(company.cnpj),
    companyName: company.companyName.trim(),
    tradeName: company.tradeName.trim(),
  })

  const handleSubmit = async (event) => {
    event.preventDefault()
    const action = method === "google" ? googleAction : credentialAction
    const payload = method === "google"
      ? { idToken: googleToken, ...companyPayload() }
      : { ...companyPayload(), email: credentials.email.trim(), password: credentials.password }
    const result = await action.run(payload)
    if (result.ok) redirect(result.data.user.role)
  }

  const handleGoogleCredential = (idToken) => {
    setGoogleError(null)
    setGoogleToken(idToken)
    setMethod("google")
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center lg:text-left">
        <h2 className="text-2xl font-bold tracking-tight text-slate-900">Nova Conta</h2>
        <p className="text-sm text-slate-500">Cadastre sua empresa como operador logístico.</p>
      </div>

      <FormAlert message={errorMessage} />

      {!method ? (
        <div className="space-y-4">
          <p className="text-sm font-medium text-slate-700">Como deseja criar sua conta?</p>
          <Button type="button" onClick={() => setMethod("password")} className="h-auto w-full justify-start bg-slate-900 p-4 text-left text-white hover:bg-slate-800">
            <KeyRound className="size-5" aria-hidden="true" />
            <span>Usar e-mail e senha</span>
          </Button>
          <GoogleAuthButton
            text="signup_with"
            onCredential={handleGoogleCredential}
            onError={(err) => setGoogleError(err?.message || "Falha na autenticação com o Google.")}
          />
        </div>
      ) : (
        <form noValidate onSubmit={handleSubmit} className="space-y-4">
          <div className="rounded-xl border border-blue-600 bg-blue-50/50 p-4 text-center shadow-sm">
            <Building2 size={24} className="mx-auto mb-2 text-blue-600" />
            <p className="text-sm font-medium text-blue-900">Operador Logístico</p>
            <p className="mt-1 text-[10px] text-slate-500">Contratar fretes</p>
          </div>
          {method === "google" ? <p className="text-sm text-slate-600">Conta Google confirmada. Complete os dados da empresa para continuar.</p> : null}
          <FormField id="cnpj" label="CNPJ" type="text" inputMode="numeric" placeholder="00.000.000/0000-00" required value={company.cnpj} onChange={handleCompanyChange} error={fieldErrors.cnpj} />
          <FormField id="companyName" label="Razão Social" type="text" placeholder="Empresa LTDA" required value={company.companyName} onChange={handleCompanyChange} error={fieldErrors.companyName} />
          <FormField id="tradeName" label="Nome Fantasia" type="text" placeholder="Nome comercial" value={company.tradeName} onChange={handleCompanyChange} error={fieldErrors.tradeName} />
          {method === "password" ? <>
            <FormField id="email" label="Email Corporativo" type="email" placeholder="nome@empresa.com" autoComplete="email" required value={credentials.email} onChange={handleCredentialsChange} error={fieldErrors.email} />
            <FormField id="password" label="Senha" type="password" autoComplete="new-password" required value={credentials.password} onChange={handleCredentialsChange} error={fieldErrors.password} />
          </> : null}
          <Button type="submit" disabled={pending} className="mt-2 w-full bg-slate-900 text-white hover:bg-slate-800">
            {pending ? <><LoaderCircle className="animate-spin" aria-hidden="true" /> Criando conta...</> : "Criar Conta"}
          </Button>
          <Button type="button" variant="link" disabled={pending} className="w-full" onClick={() => { setMethod(null); setGoogleToken(null) }}>
            Escolher outro método
          </Button>
        </form>
      )}
    </div>
  )
}

export default RegisterForm
