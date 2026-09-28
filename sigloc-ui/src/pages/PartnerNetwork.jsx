import { useCallback, useEffect, useState } from "react"
import {
  AlertCircle,
  ArrowRight,
  Check,
  Copy,
  Link2,
  Loader2,
  Plus,
  ShieldCheck,
  Star,
} from "lucide-react"
import { Link } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { ROLES } from "@/constants/roles"
import { authService } from "@/services/auth-service"
import { partnerService } from "@/services/partner-service"

const statusStyles = {
  Ativo: "border-emerald-200 bg-emerald-50 text-emerald-700",
  "Pendente de Aceite": "border-amber-200 bg-amber-50 text-amber-700",
  Encerrado: "border-slate-200 bg-slate-100 text-slate-600",
}

const dateFormatter = new Intl.DateTimeFormat("pt-BR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
})

function formatDateTime(value) {
  if (!value) {
    return "Sem interações"
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? "Sem interações" : dateFormatter.format(date)
}

function formatRating(rating) {
  return typeof rating === "number" ? rating.toFixed(1) : "—"
}

/** Humanizes the remaining lifetime of an invite for display. */
function formatExpiry({ hoursRemaining, expiresAt } = {}) {
  if (hoursRemaining === null || hoursRemaining === undefined) {
    return expiresAt ? "Expira em breve" : "Não expira"
  }

  if (hoursRemaining <= 0) {
    return "Expirado"
  }

  if (hoursRemaining < 24) {
    return `Expira em ${Math.ceil(hoursRemaining)}h`
  }

  return `Expira em ${Math.ceil(hoursRemaining / 24)} dia(s)`
}

function PartnerCard({ partner, isCarrierView }) {
  const statusClassName = statusStyles[partner.status] || "border-slate-200 bg-slate-50 text-slate-600"

  return (
    <article className="flex flex-col rounded-xl border border-slate-200 bg-white">
      <div className="flex h-[52px] items-center justify-between gap-3 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-4">
        <div className="flex min-w-0 items-center gap-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-900 text-sm font-black text-white">
            {partner.logoLetter}
          </div>
          <div className="min-w-0">
            <p className="truncate text-sm font-bold text-slate-900">{partner.name}</p>
            <p className="truncate font-mono text-[10px] font-bold uppercase tracking-wider text-slate-400">
              {isCarrierView ? partner.id : partner.cnpj}
            </p>
          </div>
        </div>

        <Badge variant="outline" className={`border text-[10px] font-bold uppercase tracking-wider ${statusClassName}`}>
          {partner.status}
        </Badge>
      </div>

      <div className="flex flex-1 flex-col space-y-4 p-4">
        {isCarrierView ? (
          <div className="grid grid-cols-2 gap-3 text-xs">
            <div className="rounded-lg border border-slate-100 bg-slate-50 p-3">
              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Ofertas abertas</p>
              <p className="mt-1 text-lg font-black text-slate-900">{partner.openOffers}</p>
            </div>
            <div className="rounded-lg border border-slate-100 bg-slate-50 p-3">
              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Corredores ativos</p>
              <p className="mt-1 text-lg font-black text-slate-900">{partner.currentLanes}</p>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-3 text-xs">
            <div className="rounded-lg border border-slate-100 bg-slate-50 p-3">
              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Veículos livres</p>
              <p className="mt-1 text-lg font-black text-slate-900">{partner.freeVehicles}</p>
            </div>
            <div className="rounded-lg border border-slate-100 bg-slate-50 p-3">
              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Viagens conosco</p>
              <p className="mt-1 text-lg font-black text-slate-900">{partner.activeTripsWithUs}</p>
            </div>
          </div>
        )}

        {isCarrierView ? (
          <div className="rounded-lg border border-slate-100 bg-white p-3">
            <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Código de conexão</p>
            <p className="mt-1 text-sm font-bold text-slate-800">{partner.inviteCode}</p>
            <p className="mt-1 text-xs text-slate-500">Use esse código para validar o convite recebido.</p>
          </div>
        ) : (
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div className="flex items-center gap-2 rounded-lg border border-slate-100 bg-white p-3">
                <Star size={14} className="shrink-0 text-amber-500" />
                <div className="min-w-0">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Nota média</p>
                  <p className="mt-0.5 text-sm font-bold text-slate-800">{formatRating(partner.averageRating)}</p>
                </div>
              </div>
              <div className="flex items-center gap-2 rounded-lg border border-slate-100 bg-white p-3">
                <ShieldCheck
                  size={14}
                  className={`shrink-0 ${partner.hasActiveInsurancePolicy ? "text-emerald-600" : "text-slate-300"}`}
                />
                <div className="min-w-0">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Seguro</p>
                  <p className="mt-0.5 text-sm font-bold text-slate-800">
                    {partner.hasActiveInsurancePolicy ? "Ativo" : "Inativo"}
                  </p>
                </div>
              </div>
            </div>

            <div className="rounded-lg border border-slate-100 bg-white p-3">
              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Última interação</p>
              <p className="mt-1 text-sm font-bold text-slate-800">{formatDateTime(partner.lastInteraction)}</p>
            </div>
          </div>
        )}

        {isCarrierView && (
          <div className="mt-auto pt-1">
            <Button asChild className="h-9 w-full bg-slate-900 text-xs font-bold text-white hover:bg-slate-800">
              <Link to={`/freights-offers-overview?partner=${encodeURIComponent(partner.name)}`}>
                Ver Ofertas Disponíveis <ArrowRight size={14} className="ml-1.5" />
              </Link>
            </Button>
          </div>
        )}
      </div>
    </article>
  )
}

function ActiveInviteBlock({ invite, pending, error, onCopy, onGenerate, copied }) {
  if (pending) {
    return (
      <div className="mt-4 flex items-center justify-center gap-2 rounded-lg border border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">
        <Loader2 size={16} className="animate-spin" /> Carregando convite...
      </div>
    )
  }

  if (!invite) {
    return (
      <div className="mt-4 space-y-3 rounded-lg border border-dashed border-slate-300 bg-slate-50 p-4 text-center">
        <p className="text-xs text-slate-500">Nenhum convite ativo no momento.</p>
        {error && (
          <p className="flex items-center justify-center gap-1.5 text-xs font-medium text-rose-600">
            <AlertCircle size={13} /> {error.message}
          </p>
        )}
        <Button
          onClick={onGenerate}
          className="h-9 w-full bg-blue-600 text-xs font-bold text-white hover:bg-blue-700"
        >
          <Plus size={14} className="mr-1.5" /> Gerar Convite
        </Button>
      </div>
    )
  }

  return (
    <div className="mt-4 space-y-2 rounded-lg border border-slate-200 bg-slate-50 p-3">
      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Convite ativo</p>
      <p className="break-all text-sm font-bold text-slate-900">{invite.link}</p>
      <div className="flex items-center justify-between gap-3 text-xs text-slate-500">
        <span>
          Código: <span className="font-mono font-bold text-slate-700">{invite.code}</span>
        </span>
        <span>{formatExpiry(invite)}</span>
      </div>
      <Button
        variant="outline"
        onClick={onCopy}
        className="mt-1 h-9 w-full border-slate-200 bg-white text-xs font-bold text-slate-700 hover:bg-slate-50"
      >
        {copied ? (
          <>
            <Check size={14} className="mr-1.5 text-emerald-600" /> Link Copiado!
          </>
        ) : (
          <>
            <Copy size={14} className="mr-1.5" /> Copiar Link de Convite
          </>
        )}
      </Button>
    </div>
  )
}

export default function PartnerNetwork() {
  const { user } = useAuth()
  const isCarrierView = user.role === ROLES.CARRIER
  const [connectionValue, setConnectionValue] = useState("")

  const [partners, setPartners] = useState([])
  const [activeInvite, setActiveInvite] = useState(null)
  const [copied, setCopied] = useState(false)
  const [connectSuccess, setConnectSuccess] = useState(null)

  const networkAction = useAsyncAction(() => partnerService.getPartnerNetwork())
  const carrierNetworkAction = useAsyncAction(() => partnerService.getCarrierNetwork())
  const activeInviteAction = useAsyncAction(() => authService.getActiveInvite())
  const createInviteAction = useAsyncAction(() => authService.createInvite())
  const connectAction = useAsyncAction((token) => authService.connectByInvite(token))

  const loadNetwork = useCallback(async () => {
    const result = await networkAction.run()
    if (result.ok) {
      setPartners(result.data.partners)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const loadCarrierNetwork = useCallback(async () => {
    const result = await carrierNetworkAction.run()
    if (result.ok) {
      setPartners(result.data.partners)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const loadActiveInvite = useCallback(async () => {
    const result = await activeInviteAction.run()
    if (result.ok) {
      setActiveInvite(result.data)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (isCarrierView) {
      loadCarrierNetwork()
      return
    }

    loadNetwork()
    loadActiveInvite()
  }, [isCarrierView, loadNetwork, loadCarrierNetwork, loadActiveInvite])

  const handleConnect = useCallback(async () => {
    if (!connectionValue.trim() || connectAction.pending) {
      return
    }

    setConnectSuccess(null)
    const result = await connectAction.run(connectionValue)
    if (result.ok) {
      setConnectSuccess(result.data?.contractorName ?? "novo embarcador")
      setConnectionValue("")
      loadCarrierNetwork()
    }
  }, [connectionValue, connectAction, loadCarrierNetwork])

  const handleCopyInvite = useCallback(async () => {
    if (!activeInvite?.link) {
      return
    }

    try {
      await navigator.clipboard.writeText(activeInvite.link)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      setCopied(false)
    }
  }, [activeInvite])

  const handleGenerateInvite = useCallback(async () => {
    const result = await createInviteAction.run()
    if (result.ok) {
      setActiveInvite(result.data)
    }
  }, [createInviteAction])

  const filteredPartners = partners

  const networkStatus = isCarrierView
    ? { pending: carrierNetworkAction.pending, error: carrierNetworkAction.error, retry: loadCarrierNetwork }
    : { pending: networkAction.pending, error: networkAction.error, retry: loadNetwork }

  return (
    <AppShell title="Rede de Parceiros">
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-7xl flex-col gap-5 overflow-hidden">
        <div className="grid shrink-0 gap-4 lg:grid-cols-[1.5fr_1fr]">

          <section className="rounded-xl border border-slate-200 bg-white p-5">
            {isCarrierView ? (
              <>
                <div className="flex items-center gap-2">
                  <Plus size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Conectar-se a um novo embarcador</h2>
                </div>
                <div className="mt-4 flex gap-2">
                  <Input
                    value={connectionValue}
                    onChange={(event) => setConnectionValue(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") {
                        handleConnect()
                      }
                    }}
                    placeholder="Ex: https://sigloc.app/invite/SIG-4821"
                    className="h-10 border-slate-200 text-sm"
                  />
                  <Button
                    onClick={handleConnect}
                    disabled={!connectionValue.trim() || connectAction.pending}
                    className="h-10 bg-blue-600 text-xs font-bold text-white hover:bg-blue-700"
                  >
                    {connectAction.pending ? (
                      <>
                        <Loader2 size={14} className="mr-1.5 animate-spin" /> Conectando...
                      </>
                    ) : (
                      "Conectar"
                    )}
                  </Button>
                </div>
                {connectAction.error && (
                  <p className="mt-2 flex items-center gap-1.5 text-xs font-medium text-rose-600">
                    <AlertCircle size={13} /> {connectAction.error.message}
                  </p>
                )}
                {connectSuccess && !connectAction.error && (
                  <p className="mt-2 flex items-center gap-1.5 text-xs font-medium text-emerald-600">
                    <Check size={13} /> Parceria com {connectSuccess} criada com sucesso!
                  </p>
                )}
              </>
            ) : (
              <>
                <div className="flex items-center gap-2">
                  <Link2 size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Convite inteligente</h2>
                </div>
                <ActiveInviteBlock
                  invite={activeInvite}
                  pending={activeInviteAction.pending || createInviteAction.pending}
                  error={createInviteAction.error}
                  copied={copied}
                  onCopy={handleCopyInvite}
                  onGenerate={handleGenerateInvite}
                />
              </>
            )}
          </section>
        </div>

        <div className="min-h-0 flex-1 overflow-hidden">
          {networkStatus.pending ? (
            <div className="flex h-full items-center justify-center gap-2 text-sm text-slate-500">
              <Loader2 size={18} className="animate-spin" /> Carregando parceiros...
            </div>
          ) : networkStatus.error ? (
            <div className="flex h-full flex-col items-center justify-center gap-3 text-sm text-slate-500">
              <AlertCircle size={20} className="text-rose-500" />
              <p>{networkStatus.error.message}</p>
              <Button
                variant="outline"
                onClick={networkStatus.retry}
                className="h-9 border-slate-200 bg-white text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Tentar novamente
              </Button>
            </div>
          ) : filteredPartners.length === 0 ? (
            <div className="flex h-full flex-col items-center justify-center gap-2 text-sm text-slate-500">
              <p className="font-medium text-slate-600">Nenhum parceiro na sua rede ainda.</p>
              <p className="text-xs">
                {isCarrierView
                  ? "Use um convite para se conectar a um novo embarcador."
                  : "Gere um convite para conectar novas transportadoras."}
              </p>
            </div>
          ) : (
            <div className="grid gap-4 overflow-y-auto pr-2 pb-6 md:grid-cols-2 xl:grid-cols-3">
              {filteredPartners.map((partner) => (
                <PartnerCard key={partner.id} partner={partner} isCarrierView={isCarrierView} />
              ))}
            </div>
          )}
        </div>
      </div>
    </AppShell>
  )
}