import { useCallback, useState } from "react"
import { AlertTriangle, ArrowLeft, ArrowRightLeft, Building2, CheckCircle2, Clock, FileText, Loader2, Mail, Navigation, Package, Phone, RefreshCw, Scale, Snowflake, Target, Truck, User } from "lucide-react"
import { Link, useNavigate, useParams } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { FormAlert } from "@/components/auth/FormAlert"
import { CompleteStopDialog } from "@/components/trips/complete-stop-dialog"
import { ReportOccurrenceDialog } from "@/components/trips/report-occurrence-dialog"
import { TripCard } from "@/components/trips/trip-card"
import { TripEventLog } from "@/components/trips/trip-event-log"
import { TripKpiCard } from "@/components/trips/trip-kpi-card"
import { TripLocationCard } from "@/components/trips/trip-location-card"
import { NO_TRACKER_ERROR, NoTrackerNotice } from "@/components/trips/no-tracker-notice"
import { TripStopTimeline } from "@/components/trips/trip-stop-timeline"
import { Button } from "@/components/ui/button"
import { useToast } from "@/components/ui/toast"
import { useAsyncAction } from "@/hooks/use-async-action"
import { useCountdown } from "@/hooks/use-countdown"
import { useTripRequest } from "@/hooks/use-trip-request"
import { saveBlob } from "@/lib/save-blob"
import {
  UNKNOWN, formatCountdown, formatFileSize, formatRouteProgress, formatTripDate, formatTripNumber, slaAlert, stopActionLabel, tripStatusLabel, tripStatusStyle,
} from "@/lib/trip-formatters"
import { completeTripStop, downloadTripProof, getCarrierTripDetails, reportTripOccurrence } from "@/services/carrier-trip-service"

const BASE_PATH = "/carrier-route-tracking"
const HOUR_MS = 60 * 60 * 1000

function countdownAlert(remainingMs, sla) {
  if (typeof remainingMs === "number" && remainingMs < 0) return "rose"
  if (typeof remainingMs === "number" && remainingMs < HOUR_MS) return "amber"
  return slaAlert(sla)
}

function temperatureRange({ refrigerated, tempMin, tempMax }) {
  if (tempMin != null && tempMax != null) return `${formatTripNumber(tempMin, " °C")} a ${formatTripNumber(tempMax, " °C")}`
  if (tempMin != null) return `A partir de ${formatTripNumber(tempMin, " °C")}`
  if (tempMax != null) return `Até ${formatTripNumber(tempMax, " °C")}`
  return refrigerated ? UNKNOWN : "Não se aplica"
}

function Spec({ label, value, mono = false }) {
  return (
    <div className="min-w-0">
      <dt className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{label}</dt>
      <dd className={`truncate text-sm font-bold text-slate-800 ${mono ? "font-mono" : ""}`}>{value || UNKNOWN}</dd>
    </div>
  )
}

function ContactRow({ icon: Icon, value, href, mono = false }) {
  const text = <span className={mono ? "font-mono" : ""}>{value || UNKNOWN}</span>
  return (
    <div className="flex items-center gap-2 text-xs text-slate-600">
      <Icon size={14} className="shrink-0 text-slate-400" />
      {href ? <a href={href} className="truncate font-medium text-blue-600 hover:underline">{text}</a> : <span className="truncate">{text}</span>}
    </div>
  )
}

function Flag({ className, children }) {
  return <span className={`inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[10px] font-bold uppercase ${className}`}>{children}</span>
}

function EmptyState({ title, description, children }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-slate-300 bg-slate-50 px-6 py-16 text-center">
      <Navigation size={28} className="text-slate-300" />
      <p className="text-sm font-bold text-slate-700">{title}</p>
      {description && <p className="max-w-md text-xs text-slate-500">{description}</p>}
      {children}
    </div>
  )
}

export default function CarrierRouteTracking() {
  const { tripId } = useParams()
  const navigate = useNavigate()
  const toast = useToast()

  const loadDetail = useCallback((options) => getCarrierTripDetails(tripId, options), [tripId])
  const detail = useTripRequest(loadDetail, tripId)
  const trip = detail.data

  const pingAction = useAsyncAction((id) => getCarrierTripDetails(id, { refresh: true }))
  const completeAction = useAsyncAction((id, stopId, payload) => completeTripStop(id, stopId, payload))
  const occurrenceAction = useAsyncAction((id, payload) => reportTripOccurrence(id, payload))
  const downloadAction = useAsyncAction((id, proofId) => downloadTripProof(id, proofId))
  const [dialog, setDialog] = useState(null)
  // Outcome of the last ping, tied to its trip so it never shows on another one.
  const [ping, setPing] = useState(null)
  const pingOutcome = ping?.tripId === tripId ? ping : null

  const isActive = Boolean(trip) && !trip.lifecycle
  const noTracker = trip?.hasTracker === false || pingOutcome?.code === NO_TRACKER_ERROR
  const busy = pingAction.pending || completeAction.pending || occurrenceAction.pending
  const deadline = trip?.currentAction?.deadline ?? trip?.nextDeadline
  const remaining = useCountdown(isActive ? deadline : null)
  const currentStop = trip?.stops.find((stop) => stop.id === trip.currentAction?.stopId)

  const openDialog = (name) => {
    ;(name === "complete" ? completeAction : occurrenceAction).reset()
    setDialog(name)
  }

  const handlePing = async () => {
    const result = await pingAction.run(tripId)
    if (!result.ok) {
      setPing({ tripId, code: result.error?.code, error: result.error?.message || "Não foi possível transmitir a posição." })
      return
    }
    detail.replace(result.data)
    setPing({ tripId, renewed: result.data.cacheRenewed })
    if (result.data.cacheRenewed) toast.success("Posição transmitida", "Localização, ETA e risco recalculados.")
  }

  const handleComplete = async (payload) => {
    const action = trip.currentAction
    const result = await completeAction.run(tripId, action.stopId, payload)
    if (!result.ok) {
      // 409: the stop or trip changed since this screen loaded; show the current state instead.
      if (result.error?.status === 409) {
        setDialog(null)
        toast.error("Parada não confirmada", result.error.message)
        detail.reload()
      }
      return
    }
    detail.replace(result.data)
    setDialog(null)
    toast.success("Parada confirmada", result.data.lifecycle === "ENTREGUE" ? "Todas as paradas concluídas: viagem entregue." : "A próxima parada já está liberada.")
  }

  const handleReport = async (payload) => {
    const result = await occurrenceAction.run(tripId, payload)
    if (!result.ok) return
    detail.replace(result.data)
    setDialog(null)
    toast.success("Ocorrência registrada", "O embarcador já vê a ocorrência no monitoramento.")
  }

  const handleDownload = async (proof) => {
    const result = await downloadAction.run(tripId, proof.id)
    if (result.ok) saveBlob(result.data, proof.fileName)
    else toast.error("Não foi possível baixar o comprovante", result.error?.message || "Tente novamente.")
  }

  const renderStopActions = (stop) => {
    const proofs = trip.proofs.filter((proof) => proof.stopId === stop.id)
    const actionable = isActive && trip.currentAction?.stopId === stop.id
    if (!actionable && !proofs.length) return null
    return (
      <div className="flex flex-col items-end gap-1.5">
        {actionable && (
          <Button size="sm" className="bg-blue-600 text-white hover:bg-blue-700" onClick={() => openDialog("complete")} disabled={busy}>
            <CheckCircle2 /> {stopActionLabel(trip.currentAction.type)}
          </Button>
        )}
        {proofs.map((proof) => (
          <button key={proof.id} type="button" onClick={() => handleDownload(proof)} disabled={downloadAction.pending} className="flex items-center gap-1 text-xs font-medium text-blue-600 hover:underline disabled:opacity-50">
            <FileText size={12} /> {proof.fileName} <span className="font-mono text-slate-400">{formatFileSize(proof.sizeBytes)}</span>
          </button>
        ))}
      </div>
    )
  }

  const renderBody = () => {
    if (detail.pending) return <p className="flex items-center gap-2 text-sm text-slate-500"><Loader2 size={16} className="animate-spin" /> Carregando viagem...</p>
    if (detail.error?.status === 404) {
      return (
        <EmptyState title="Viagem não encontrada" description="Ela não existe ou não está atribuída à sua transportadora.">
          <Button variant="outline" className="mt-2" onClick={() => navigate(BASE_PATH)}>Ver minhas viagens</Button>
        </EmptyState>
      )
    }
    if (detail.error || !trip) {
      return (
        <div className="space-y-3">
          <FormAlert message={detail.error?.message || "Não foi possível carregar a viagem."} />
          <Button variant="outline" onClick={detail.reload}>Tentar novamente</Button>
        </div>
      )
    }

    const { cargo, shipper } = trip

    return (
      <>
        {trip.stale && <p role="alert" className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">{trip.warning || "Dados de monitoramento desatualizados."}</p>}
        {trip.lifecycle && (
          <p className={`rounded-xl border p-4 text-sm ${trip.lifecycle === "ENTREGUE" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-slate-200 bg-slate-50 text-slate-700"}`}>
            {trip.lifecycle === "ENTREGUE" ? "Viagem entregue: todas as paradas foram confirmadas." : "Viagem cancelada: não há ações pendentes."}
          </p>
        )}

        <div className="grid shrink-0 grid-cols-1 gap-4 md:grid-cols-4">
          <TripKpiCard
            icon={ArrowRightLeft}
            label="Progresso da Rota"
            value={formatRouteProgress(trip)}
            hint={trip.progress != null ? `${formatTripNumber(trip.progress, "%")} percorrido` : undefined}
          />
          <TripKpiCard
            icon={Target}
            tone="indigo"
            alert={isActive ? countdownAlert(remaining, trip.sla) : undefined}
            label="SLA da Próxima Parada"
            value={isActive && deadline ? <span className="font-mono">{formatCountdown(remaining)}</span> : tripStatusLabel(trip.sla)}
            hint={isActive && deadline ? `${tripStatusLabel(trip.sla)} · prazo ${formatTripDate(deadline)}` : undefined}
          />
          <TripKpiCard icon={Scale} tone="purple" label="Utilização Carga" value={formatTripNumber(trip.utilization, "%")} />
          <TripKpiCard icon={Clock} tone="emerald" label="ETA Previsto" value={formatTripDate(trip.eta)} hint={!trip.eta && isActive ? "Recalculado no próximo ping" : undefined} />
        </div>

        <div className="grid grid-cols-1 gap-4 lg:min-h-0 lg:flex-1 lg:grid-cols-12 lg:items-stretch">
          <div className="flex min-h-0 flex-col gap-4 lg:col-span-8">
            <div className="shrink-0">
              <TripStopTimeline stops={trip.stops} renderStopActions={renderStopActions} />
            </div>

            <TripEventLog
              events={trip.events}
              className="min-h-[220px] flex-1"
              bodyClassName="min-h-0 flex-1 overflow-hidden p-4"
              aside={isActive && (
                <Button size="sm" variant="outline" className="border-amber-200 text-amber-700 hover:bg-amber-50" onClick={() => openDialog("occurrence")} disabled={busy}>
                  <AlertTriangle size={14} className="mr-1.5" /> Reportar Ocorrência
                </Button>
              )}
            />

            <div className="grid shrink-0 grid-cols-1 gap-4 md:grid-cols-2">
              <TripCard icon={Building2} iconClassName="text-slate-600" title="Contato do Embarcador" bodyClassName="space-y-3 p-4">
                <div>
                  <p className="text-sm font-bold text-slate-800">{shipper?.tradeName || shipper?.companyName || UNKNOWN}</p>
                  {shipper?.tradeName && shipper?.companyName && <p className="text-[11px] text-slate-500">{shipper.companyName}</p>}
                </div>
                <div className="space-y-2 border-t border-slate-100 pt-3">
                  <ContactRow icon={User} value={shipper?.contactName} />
                  <ContactRow icon={Phone} value={shipper?.phone} href={shipper?.phoneHref} mono />
                  <ContactRow icon={Mail} value={shipper?.email} href={shipper?.emailHref} />
                </div>
              </TripCard>

              <TripCard icon={Truck} iconClassName="text-emerald-600" title="Veículo e Motorista" bodyClassName="space-y-3 p-4">
                <div>
                  <p className="text-sm font-bold text-slate-800">{trip.driver?.name || UNKNOWN}</p>
                  <p className="text-[11px] text-slate-500">Motorista</p>
                </div>
                <div className="space-y-2 border-t border-slate-100 pt-3">
                  <ContactRow icon={Truck} value={trip.plate} mono />
                  <ContactRow icon={Phone} value={trip.driver?.phone} mono />
                </div>
              </TripCard>
            </div>
          </div>

          <div className="flex min-h-0 flex-col gap-4 lg:col-span-4">
            <TripLocationCard trip={trip} className="flex min-h-[140px] flex-1 flex-col overflow-hidden" mapClassName="min-h-0 flex-1">
              {noTracker && <NoTrackerNotice plate={trip.plate} isCarrier />}
              <Button
                className="w-full bg-emerald-600 text-xs font-bold text-white hover:bg-emerald-700"
                onClick={handlePing}
                disabled={!isActive || busy || noTracker}
                title={noTracker ? "Veículo sem dispositivo Traccar: não é possível rastrear por GPS." : undefined}
              >
                <RefreshCw className={pingAction.pending ? "animate-spin" : ""} />
                {pingAction.pending ? "Transmitindo..." : "Transmitir Posição GPS (Ping)"}
              </Button>
              {pingOutcome?.error && !noTracker && <p className="text-xs font-medium text-rose-600">{pingOutcome.error}</p>}
              {pingOutcome?.renewed === false && (
                <p className="text-xs text-amber-700">Nenhuma nova posição foi calculada agora: exibindo a posição em cache.</p>
              )}
              {pingOutcome?.renewed && <p className="text-xs text-emerald-700">Posição atualizada em <span className="font-mono">{formatTripDate(trip.lastPing)}</span>.</p>}
            </TripLocationCard>

            <TripCard icon={Package} iconClassName="text-blue-600" title="Especificação da Carga" className="shrink-0" bodyClassName="space-y-4 p-4">
              {(cargo.refrigerated || cargo.palletized || cargo.dangerous || cargo.fragile) && (
                <div className="flex flex-wrap gap-1.5">
                  {cargo.refrigerated && <Flag className="bg-blue-50 text-blue-700"><Snowflake size={10} /> Refrigerada</Flag>}
                  {cargo.palletized && <Flag className="bg-slate-100 text-slate-600">Paletizada</Flag>}
                  {cargo.dangerous && <Flag className="bg-rose-50 text-rose-700">Perigosa</Flag>}
                  {cargo.fragile && <Flag className="bg-amber-50 text-amber-700">Frágil</Flag>}
                </div>
              )}
              <dl className="grid grid-cols-2 gap-3">
                <Spec label="Ambiente" value={cargo.environment} />
                <Spec label="Temperatura" value={temperatureRange(cargo)} mono />
                <Spec label="Carroceria" value={cargo.bodywork} />
                <Spec label="Embalagens" value={cargo.packagings.join(", ")} />
                <Spec label="Peso Total" value={formatTripNumber(cargo.totalWeightKg, " kg")} mono />
                <Spec label="Volume Total" value={formatTripNumber(cargo.totalVolumeM3, " m³")} mono />
              </dl>
              {cargo.products.length > 0 && (
                <div className="space-y-1.5 border-t border-slate-100 pt-3">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Produtos</p>
                  <ul className="space-y-1">
                    {cargo.products.map((product, index) => (
                      <li key={`${product.name}-${index}`} className="flex justify-between gap-2 text-xs text-slate-700">
                        <span className="truncate">{product.name}</span>
                        <span className="shrink-0 font-mono text-slate-500">{product.quantity ?? "—"} un.</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </TripCard>
          </div>
        </div>
      </>
    )
  }

  return (
    <AppShell title="Rota em Trânsito">
      <div className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden">
        <div className="mb-4 flex shrink-0 flex-wrap items-center justify-between gap-3 border-b border-slate-200 pb-3 pt-1">
          <Link to={BASE_PATH}>
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Rotas em Trânsito
            </Button>
          </Link>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <Navigation size={18} className="shrink-0 text-blue-600" />
              <h2 className="truncate font-mono text-lg font-black text-slate-900">{trip?.reference || "Rota em Trânsito"}</h2>
              {trip && <Flag className={tripStatusStyle(trip.status)}>{tripStatusLabel(trip.status)}</Flag>}
            </div>
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto pb-6 pr-2 lg:flex lg:flex-col lg:overflow-hidden">
          <div className="space-y-4 lg:flex lg:min-h-0 lg:flex-1 lg:flex-col lg:space-y-0 lg:gap-4">{renderBody()}</div>
        </div>
      </div>

      {trip && (
        <>
          <CompleteStopDialog
            open={dialog === "complete"}
            onOpenChange={(open) => setDialog(open ? "complete" : null)}
            action={isActive ? trip.currentAction : null}
            stop={currentStop}
            pending={completeAction.pending}
            error={completeAction.error}
            fieldErrors={completeAction.fieldErrors}
            onSubmit={handleComplete}
          />
          <ReportOccurrenceDialog
            open={dialog === "occurrence"}
            onOpenChange={(open) => setDialog(open ? "occurrence" : null)}
            pending={occurrenceAction.pending}
            error={occurrenceAction.error}
            fieldErrors={occurrenceAction.fieldErrors}
            onSubmit={handleReport}
          />
        </>
      )}
    </AppShell>
  )
}
