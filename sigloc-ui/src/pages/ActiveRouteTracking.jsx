import { useCallback, useState } from "react"
import { ArrowLeft, RefreshCw, Truck, Clock, Phone, User, Scale, ArrowRightLeft, Target, Loader2, FileText } from "lucide-react"
import { Link, useParams } from "react-router-dom"
import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { useToast } from "@/components/ui/toast"
import { TripCard } from "@/components/trips/trip-card"
import { TripEventLog } from "@/components/trips/trip-event-log"
import { TripKpiCard } from "@/components/trips/trip-kpi-card"
import { TripLocationCard } from "@/components/trips/trip-location-card"
import { NO_TRACKER_ERROR, NoTrackerNotice } from "@/components/trips/no-tracker-notice"
import { TripStopTimeline } from "@/components/trips/trip-stop-timeline"
import { useAsyncAction } from "@/hooks/use-async-action"
import { useTripRequest } from "@/hooks/use-trip-request"
import { saveBlob } from "@/lib/save-blob"
import { downloadTripProof, getTripDetails } from "@/services/trip-service"
import { formatFileSize, formatRouteProgress, formatTripDate, formatTripNumber, slaAlert, tripStatusLabel } from "@/lib/trip-formatters"

export default function ActiveRouteTracking() {
  const { tripId } = useParams()
  // Page load reads the cached snapshot; "Localizar Motorista" is a separate request for a live GPS
  // position, so a failed attempt keeps the trip on screen and only reports why.
  const load = useCallback((options) => getTripDetails(tripId, options), [tripId])
  const { data: trip, pending, error, replace } = useTripRequest(load, tripId)
  const pingAction = useAsyncAction((id) => getTripDetails(id, { refresh: true }))
  // Outcome of the last attempt, tied to its trip so it never shows on another one.
  const [ping, setPing] = useState(null)
  const pingOutcome = ping?.tripId === tripId ? ping : null
  const noTracker = trip?.hasTracker === false || pingOutcome?.code === NO_TRACKER_ERROR
  const handlePing = async () => {
    const result = await pingAction.run(tripId)
    if (result.ok) {
      replace(result.data)
      setPing(null)
    } else {
      setPing({ tripId, code: result.error?.code, error: result.error?.message || "Não foi possível localizar o motorista." })
    }
  }
  const toast = useToast()
  const downloadAction = useAsyncAction((id, proofId) => downloadTripProof(id, proofId))
  const handleDownload = async (proof) => {
    const result = await downloadAction.run(tripId, proof.id)
    if (result.ok) saveBlob(result.data, proof.fileName)
    else toast.error("Não foi possível baixar o comprovante", result.error?.message || "Tente novamente.")
  }

  if (error?.status === 404) {
    return (
      <AppShell title="Monitoramento">
        <div className="flex flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-slate-300 bg-slate-50 px-6 py-16 text-center">
          <Truck size={28} className="text-slate-300" />
          <p className="text-sm font-bold text-slate-700">Viagem não encontrada</p>
          <p className="max-w-md text-xs text-slate-500">Ela não existe ou não pertence à sua empresa.</p>
          <Link to="/active-routes">
            <Button variant="outline" className="mt-2">Ver rotas ativas</Button>
          </Link>
        </div>
      </AppShell>
    )
  }

  return (
    <AppShell title={trip?.reference ? `Monitoramento: ${trip.reference}` : "Monitoramento"}>
      <div className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden">
        <div className="mb-4 flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1">
          <Link to="/active-routes">
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Rotas Ativas
            </Button>
          </Link>
          <Button
            className="h-9 bg-emerald-600 text-xs font-bold text-white hover:bg-emerald-700"
            onClick={handlePing}
            disabled={pending || pingAction.pending || noTracker}
            title={noTracker ? "Veículo sem dispositivo Traccar: não é possível rastrear por GPS." : undefined}
          >
            <RefreshCw size={14} className={`mr-1.5 ${pingAction.pending ? "animate-spin" : ""}`} />
            {pingAction.pending ? "Localizando..." : "Localizar Motorista"}
          </Button>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto pb-6 pr-2 lg:flex lg:flex-col">
          <div className="space-y-4 lg:flex lg:flex-1 lg:flex-col lg:space-y-0 lg:gap-4">
            {pending && <p className="flex items-center gap-2 text-sm text-slate-500"><Loader2 size={16} className="animate-spin" /> Carregando monitoramento...</p>}
            {error && <p role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{error.message || "Não foi possível carregar a viagem."}</p>}
            {noTracker && <NoTrackerNotice plate={trip?.plate} />}
            {pingOutcome?.error && !noTracker && <p role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{pingOutcome.error}</p>}
            {trip?.stale && <p role="alert" className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">{trip.warning || "Dados de monitoramento desatualizados."}</p>}

            <div className="grid shrink-0 grid-cols-1 gap-4 md:grid-cols-4">
              <TripKpiCard icon={ArrowRightLeft} label="Progresso da Rota" value={formatRouteProgress(trip)} />
              <TripKpiCard icon={Target} tone="indigo" alert={slaAlert(trip?.sla)} label="Conf. SLA" value={tripStatusLabel(trip?.sla)} />
              <TripKpiCard icon={Scale} tone="purple" label="Utilização Carga" value={formatTripNumber(trip?.utilization, "%")} />
              <TripKpiCard
                icon={Clock}
                tone="emerald"
                label="ETA Previsto"
                value={formatTripDate(trip?.eta)}
                hint={trip?.lastPing ? `Último ping GPS: ${formatTripDate(trip.lastPing)}` : undefined}
              />
            </div>

            <div className="grid grid-cols-1 gap-4 lg:flex-1 lg:grid-cols-12 lg:items-stretch">
              <div className="flex flex-col gap-4 lg:col-span-8">
                <div className="shrink-0">
                  <TripStopTimeline stops={trip?.timeline ?? []} />
                </div>
                <TripEventLog
                  events={trip?.events ?? []}
                  className="min-h-[220px] flex-1"
                  bodyClassName="min-h-0 flex-1 overflow-hidden p-4"
                />
              </div>

              <div className="flex flex-col gap-4 lg:col-span-4">
                <TripLocationCard trip={trip} className="flex min-h-fit flex-1 flex-col overflow-hidden" mapClassName="min-h-[180px] flex-1" />

                <TripCard icon={Truck} title="Veículo e Motorista" className="shrink-0" bodyClassName="space-y-3 p-4">
                  <div>
                    <p className="text-xs font-bold text-slate-800">{trip?.carrier || "Não informado"}</p>
                    <p className="font-mono text-[10px] text-slate-400">Placa: {trip?.plate || "Não informado"}</p>
                  </div>
                  <div className="space-y-3 border-t border-slate-100 pt-3">
                    <div className="flex items-center gap-2 text-xs text-slate-600"><User size={14} className="text-slate-400" /> {trip?.driver?.name || "Não informado"}</div>
                    <div className="flex items-center gap-2 text-xs text-slate-600"><Phone size={14} className="text-slate-400" /> {trip?.driver?.phone || "Não informado"}</div>
                  </div>
                </TripCard>

                {trip && (
                  <TripCard icon={FileText} title="Comprovantes de Entrega" className="shrink-0" bodyClassName="space-y-2 p-4">
                    {trip.proofs.length === 0 && (
                      <div className="flex min-h-[96px] items-center justify-center rounded-lg border border-dashed border-slate-200 bg-slate-50/70 px-3 py-6 text-center text-xs font-medium text-slate-500">
                        Nenhum comprovante enviado pela transportadora ainda.
                      </div>
                    )}
                    {trip.proofs.map((proof) => (
                      <button key={proof.id} type="button" onClick={() => handleDownload(proof)} disabled={downloadAction.pending} className="flex w-full items-center gap-2 text-left text-xs font-medium text-blue-600 hover:underline disabled:opacity-50">
                        <FileText size={14} className="shrink-0" />
                        <span className="min-w-0 flex-1 truncate">{proof.fileName}</span>
                        <span className="shrink-0 font-mono text-[10px] text-slate-400">{formatFileSize(proof.sizeBytes)} · {formatTripDate(proof.uploadedAt)}</span>
                      </button>
                    ))}
                  </TripCard>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  )
}
