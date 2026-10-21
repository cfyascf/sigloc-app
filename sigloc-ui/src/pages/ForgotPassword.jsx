import { useState } from "react"
import { Link } from "react-router-dom"
import { LoaderCircle } from "lucide-react"

import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"

export default function ForgotPassword() {
  const { requestPasswordReset } = useAuth()
  const [email, setEmail] = useState("")
  const [submitted, setSubmitted] = useState(false)
  const action = useAsyncAction(requestPasswordReset)

  const handleSubmit = async (event) => {
    event.preventDefault()
    const result = await action.run({ email: email.trim() })
    if (result.ok) setSubmitted(true)
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-6">
      <section className="w-full max-w-md space-y-6 rounded-xl bg-white p-8 shadow-sm">
        <div className="space-y-2">
          <h1 className="text-2xl font-bold text-slate-900">Redefinir senha</h1>
          <p className="text-sm text-slate-500">Informe o e-mail da conta para receber um link de redefinição.</p>
        </div>
        {submitted ? <p role="status" className="rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-800">Se houver uma conta local para este e-mail, enviaremos instruções de redefinição.</p> : <>
          <FormAlert message={action.error?.message} />
          <form noValidate onSubmit={handleSubmit} className="space-y-4">
            <FormField id="email" label="E-mail" type="email" autoComplete="email" required value={email} onChange={(event) => setEmail(event.target.value)} error={action.fieldErrors.email} />
            <Button type="submit" disabled={action.pending} className="w-full bg-blue-600 text-white hover:bg-blue-700">
              {action.pending ? <><LoaderCircle className="animate-spin" aria-hidden="true" /> Enviando...</> : "Enviar link"}
            </Button>
          </form>
        </>}
        <Link to="/login" className="block text-center text-sm font-medium text-blue-600 hover:underline">Voltar para entrar</Link>
      </section>
    </main>
  )
}
