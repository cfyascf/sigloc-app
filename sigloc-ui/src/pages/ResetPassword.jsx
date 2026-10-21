import { useState } from "react"
import { Link, useSearchParams } from "react-router-dom"
import { LoaderCircle } from "lucide-react"

import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"

export default function ResetPassword() {
  const { confirmPasswordReset } = useAuth()
  const [searchParams] = useSearchParams()
  const [password, setPassword] = useState("")
  const [confirmation, setConfirmation] = useState("")
  const [completed, setCompleted] = useState(false)
  const [formError, setFormError] = useState(null)
  const action = useAsyncAction(confirmPasswordReset)
  const token = searchParams.get("token")

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (password !== confirmation) {
      setFormError("As senhas não coincidem.")
      return
    }
    setFormError(null)
    const result = await action.run({ token, password })
    if (result.ok) setCompleted(true)
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-6">
      <section className="w-full max-w-md space-y-6 rounded-xl bg-white p-8 shadow-sm">
        <div className="space-y-2">
          <h1 className="text-2xl font-bold text-slate-900">Definir nova senha</h1>
          <p className="text-sm text-slate-500">Escolha uma senha com pelo menos 8 caracteres.</p>
        </div>
        {completed ? <p role="status" className="rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-800">Senha atualizada. Você já pode entrar.</p> : <>
          <FormAlert message={formError || action.error?.message || (!token ? "O link de redefinição é inválido ou expirou." : null)} />
          <form noValidate onSubmit={handleSubmit} className="space-y-4">
            <FormField id="password" label="Nova senha" type="password" autoComplete="new-password" required disabled={!token} value={password} onChange={(event) => setPassword(event.target.value)} error={action.fieldErrors.password} />
            <FormField id="confirmation" label="Confirmar nova senha" type="password" autoComplete="new-password" required disabled={!token} value={confirmation} onChange={(event) => setConfirmation(event.target.value)} />
            <Button type="submit" disabled={!token || action.pending} className="w-full bg-blue-600 text-white hover:bg-blue-700">
              {action.pending ? <><LoaderCircle className="animate-spin" aria-hidden="true" /> Atualizando...</> : "Atualizar senha"}
            </Button>
          </form>
        </>}
        <Link to="/login" className="block text-center text-sm font-medium text-blue-600 hover:underline">Voltar para entrar</Link>
      </section>
    </main>
  )
}
