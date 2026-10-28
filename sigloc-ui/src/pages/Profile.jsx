import { useEffect, useState } from "react"
import { Building2, Headset, LoaderCircle, Save, ShieldCheck, Star, TrendingUp } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { FormField } from "@/components/auth/FormField"
import { FormAlert } from "@/components/auth/FormAlert"
import { useAsyncAction } from "@/hooks/use-async-action"
import { formatCnpj } from "@/lib/formatters"
import { profileService } from "@/services/profile-service"
import { ROLES } from "@/constants/roles"
import { useAuth } from "@/contexts/AuthContext"

function MetricTile({ icon: Icon, label, value }) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-white text-slate-700 shadow-sm">
        <Icon size={18} />
      </div>
      <div className="min-w-0">
        <p className="text-[10px] font-black tracking-widest text-slate-400 uppercase">{label}</p>
        <p className="truncate text-sm font-bold text-slate-900">{value}</p>
      </div>
    </div>
  )
}

// Same limit as the API (ProfileService.MaxCompanyNameLength).
const COMPANY_NAME_MAX_LENGTH = 150

const EMPTY_CONTACT = { logisticsContactName: "", logisticsContactPhone: "", logisticsContactEmail: "" }

const toContactForm = (profile) => ({
  logisticsContactName: profile.logisticsContactName ?? "",
  logisticsContactPhone: profile.logisticsContactPhone ?? "",
  logisticsContactEmail: profile.logisticsContactEmail ?? "",
})

export default function Profile() {
  const { setDisplayName } = useAuth()
  const [profile, setProfile] = useState(null)
  const [form, setForm] = useState({ companyName: "", tradeName: "" })
  const [successMessage, setSuccessMessage] = useState("")
  const [contactForm, setContactForm] = useState(EMPTY_CONTACT)
  const [contactSuccess, setContactSuccess] = useState("")

  const loadAction = useAsyncAction(profileService.getProfile)
  const saveAction = useAsyncAction(profileService.updateProfile)
  const contactAction = useAsyncAction(profileService.updateProfile)

  useEffect(() => {
    let active = true

    loadAction.run().then((result) => {
      if (active && result.ok && result.data) {
        setProfile(result.data)
        setForm({
          companyName: result.data.companyName ?? "",
          tradeName: result.data.tradeName ?? "",
        })
        setContactForm(toContactForm(result.data))
      }
    })

    return () => {
      active = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleChange = (event) => {
    const { id, value } = event.target
    setForm((prev) => ({ ...prev, [id]: value }))
    setSuccessMessage("")
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (saveAction.pending) return

    setSuccessMessage("")
    const result = await saveAction.run({
      companyName: form.companyName,
      tradeName: form.tradeName,
    })

    if (result.ok && result.data) {
      setProfile(result.data)
      setForm({
        companyName: result.data.companyName ?? "",
        tradeName: result.data.tradeName ?? "",
      })
      setDisplayName(result.data.tradeName || result.data.companyName)
      setSuccessMessage("Dados atualizados com sucesso.")
    }
  }

  const handleContactChange = (event) => {
    const { id, value } = event.target
    setContactForm((prev) => ({ ...prev, [id]: value }))
    setContactSuccess("")
  }

  // Saves only the contact: company fields are resent as stored, never the unsaved edits above.
  const handleContactSubmit = async (event) => {
    event.preventDefault()
    if (contactAction.pending) return

    setContactSuccess("")
    const result = await contactAction.run({
      companyName: profile.companyName,
      tradeName: profile.tradeName,
      ...contactForm,
    })

    if (result.ok && result.data) {
      setProfile(result.data)
      setContactForm(toContactForm(result.data))
      setContactSuccess("Contato logístico atualizado.")
    }
  }

  const isCarrier = profile?.role === ROLES.CARRIER
  const isContractor = profile?.role === ROLES.CONTRACTOR
  const companyNameError = saveAction.fieldErrors.razaoSocial ?? saveAction.fieldErrors.companyName
  const tradeNameError = saveAction.fieldErrors.nomeFantasia ?? saveAction.fieldErrors.tradeName

  const ratingLabel =
    profile?.averageRating === null || profile?.averageRating === undefined
      ? "—"
      : profile.averageRating.toFixed(1)
  const onTimeLabel =
    profile?.onTimeDeliveryRate === null || profile?.onTimeDeliveryRate === undefined
      ? "—"
      : `${profile.onTimeDeliveryRate}%`
  const insuranceLabel = profile?.hasActiveInsurancePolicy ? "Apólice ativa" : "Sem apólice ativa"

  return (
    <AppShell title="Meu Perfil">
      <div className="mx-auto w-full max-w-2xl space-y-6">
        <div>
          <h1 className="text-xl font-black text-slate-900">Dados da Empresa</h1>
          <p className="mt-1 text-sm text-slate-500">
            Gerencie as informações da sua empresa. O CNPJ é um identificador legal e não pode ser alterado.
          </p>
        </div>

        {loadAction.pending ? (
          <div className="flex items-center gap-2 text-sm text-slate-500">
            <LoaderCircle className="animate-spin" size={16} aria-hidden="true" />
            Carregando perfil...
          </div>
        ) : loadAction.error ? (
          <FormAlert message={loadAction.error.message} />
        ) : profile ? (
          <>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-base">
                  <Building2 size={18} /> Informações Cadastrais
                </CardTitle>
                <CardDescription>Atualize a razão social e o nome fantasia.</CardDescription>
              </CardHeader>
              <CardContent>
                <form noValidate onSubmit={handleSubmit} className="space-y-4">
                  {saveAction.error ? <FormAlert message={saveAction.error.message} /> : null}
                  {successMessage ? (
                    <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-700">
                      {successMessage}
                    </div>
                  ) : null}

                  <FormField
                    id="cnpj"
                    label="CNPJ"
                    type="text"
                    value={formatCnpj(profile.cnpj)}
                    disabled
                    readOnly
                  />
                  <FormField
                    id="companyName"
                    label="Razão Social"
                    type="text"
                    placeholder="Empresa LTDA"
                    required
                    maxLength={COMPANY_NAME_MAX_LENGTH}
                    value={form.companyName}
                    onChange={handleChange}
                    error={companyNameError}
                  />
                  <FormField
                    id="tradeName"
                    label="Nome Fantasia"
                    type="text"
                    placeholder="Nome comercial"
                    maxLength={COMPANY_NAME_MAX_LENGTH}
                    value={form.tradeName}
                    onChange={handleChange}
                    error={tradeNameError}
                  />

                  <Button
                    type="submit"
                    disabled={saveAction.pending}
                    className="mt-2 w-full bg-slate-900 text-white hover:bg-slate-800"
                  >
                    {saveAction.pending ? (
                      <>
                        <LoaderCircle className="animate-spin" aria-hidden="true" /> Salvando...
                      </>
                    ) : (
                      <>
                        <Save size={16} aria-hidden="true" /> Salvar Alterações
                      </>
                    )}
                  </Button>
                </form>
              </CardContent>
            </Card>

            {isContractor ? (
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-base">
                    <Headset size={18} /> Contato Logístico
                  </CardTitle>
                  <CardDescription>
                    Exibido às transportadoras nas viagens em andamento. Deixe em branco para não informar.
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <form noValidate onSubmit={handleContactSubmit} className="space-y-4">
                    {contactAction.error ? <FormAlert message={contactAction.error.message} /> : null}
                    {contactSuccess ? (
                      <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-700">
                        {contactSuccess}
                      </div>
                    ) : null}

                    <FormField
                      id="logisticsContactName"
                      label="Nome do Contato"
                      type="text"
                      placeholder="Ex.: Mesa de Logística"
                      maxLength={120}
                      value={contactForm.logisticsContactName}
                      onChange={handleContactChange}
                      error={contactAction.fieldErrors.logisticsContactName}
                    />
                    <div className="grid gap-4 sm:grid-cols-2">
                      <FormField
                        id="logisticsContactPhone"
                        label="Telefone"
                        type="tel"
                        placeholder="(41) 3333-0000"
                        className="font-mono"
                        value={contactForm.logisticsContactPhone}
                        onChange={handleContactChange}
                        error={contactAction.fieldErrors.logisticsContactPhone}
                      />
                      <FormField
                        id="logisticsContactEmail"
                        label="E-mail"
                        type="email"
                        placeholder="logistica@empresa.com.br"
                        value={contactForm.logisticsContactEmail}
                        onChange={handleContactChange}
                        error={contactAction.fieldErrors.logisticsContactEmail}
                      />
                    </div>

                    <Button
                      type="submit"
                      disabled={contactAction.pending}
                      className="mt-2 w-full bg-slate-900 text-white hover:bg-slate-800"
                    >
                      {contactAction.pending ? (
                        <>
                          <LoaderCircle className="animate-spin" aria-hidden="true" /> Salvando...
                        </>
                      ) : (
                        <>
                          <Save size={16} aria-hidden="true" /> Salvar Contato
                        </>
                      )}
                    </Button>
                  </form>
                </CardContent>
              </Card>
            ) : null}

            {isCarrier ? (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Indicadores Operacionais</CardTitle>
                  <CardDescription>
                    Métricas gerenciadas pelo sistema a partir do seu histórico de operações.
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="grid gap-3 sm:grid-cols-3">
                    <MetricTile icon={Star} label="Avaliação Média" value={ratingLabel} />
                    <MetricTile icon={TrendingUp} label="Entregas no Prazo" value={onTimeLabel} />
                    <MetricTile icon={ShieldCheck} label="Seguro" value={insuranceLabel} />
                  </div>
                </CardContent>
              </Card>
            ) : null}
          </>
        ) : null}
      </div>
    </AppShell>
  )
}
