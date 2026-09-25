import { useMemo, useState } from "react"
import { Building2, Truck } from "lucide-react"

import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { usePostAuthRedirect } from "@/hooks/use-post-auth-redirect"
import { formatCnpj, onlyDigits } from "@/lib/formatters"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"
import { AuthDivider } from "@/components/auth/AuthDivider"
import { GoogleAuthButton } from "@/components/auth/GoogleAuthButton"

const EMPTY_COMPANY = { cnpj: "", companyName: "", tradeName: "" }

export function RegisterForm() {
  const { registerContractor, registerContractorWithGoogle } = useAuth()
  const redirect = usePostAuthRedirect()

  const [company, setCompany] = useState(EMPTY_COMPANY)
  const [credentials, setCredentials] = useState({ email: "", password: "" })
  const [googleError, setGoogleError] = useState(null)

  const credentialAction = useAsyncAction(registerContractor)
  const googleAction = useAsyncAction(registerContractorWithGoogle)

  const pending = credentialAction.pending || googleAction.pending
  const fieldErrors = {
    ...credentialAction.fieldErrors,
    ...googleAction.fieldErrors,
  }
  const errorMessage =
    credentialAction.error?.message ||
    googleAction.error?.message ||
    googleError

  // Google register also needs the company fields, so require them before the
  // Google button becomes actionable.
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

  const handleSubmit = async (event) => {
    event.preventDefault()
    const result = await credentialAction.run({
      ...companyPayload(),
      email: credentials.email.trim(),
      password: credentials.password,
    })
    if (result.ok) {
      redirect(result.data.user.role)
    }
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
    if (result.ok) {
      redirect(result.data.user.role)
    }
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center lg:text-left">
        <h2 className="text-2xl font-bold tracking-tight text-slate-900">
          Nova Conta
        </h2>
        <p className="text-sm text-slate-500">
          Cadastre sua empresa como operador logístico.
        </p>
      </div>

      <FormAlert message={errorMessage} />

      <form noValidate onSubmit={handleSubmit} className="space-y-4">
        {/* Perfil — apenas Contratante está disponível no momento. */}
        <div className="grid grid-cols-2 gap-4 pb-2">
          <div className="rounded-xl border border-blue-600 bg-blue-50/50 p-4 text-center shadow-sm">
            <Building2 size={24} className="mx-auto mb-2 text-blue-600" />
            <p className="text-sm font-medium text-blue-900">Operador Logístico</p>
            <p className="mt-1 text-[10px] text-slate-500">Contratar fretes</p>
          </div>

          <div
            className="cursor-not-allowed rounded-xl border border-slate-200 bg-slate-50 p-4 text-center opacity-60"
            title="Cadastro de transportadora em breve"
            aria-disabled="true"
          >
            <Truck size={24} className="mx-auto mb-2 text-slate-400" />
            <p className="text-sm font-medium text-slate-500">Transportadora</p>
            <p className="mt-1 text-[10px] text-slate-400">Em breve</p>
          </div>
        </div>

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
          placeholder="Empresa LTDA"
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
          {credentialAction.pending ? "Criando conta..." : "Criar Conta"}
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

export default RegisterForm
