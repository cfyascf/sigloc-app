import { useCallback, useRef } from "react"
import { ArrowLeft, RefreshCw, Truck, MapPinned, Clock, Phone, User, Scale, ArrowRightLeft, Target, MapPin, Loader2 } from "lucide-react"
import { Link, useParams } from "react-router-dom"
import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { useTripRequest } from "@/hooks/use-trip-request"
import { getTripDetails } from "@/services/trip-service"
import { formatTripDate, formatTripNumber, tripStatusLabel } from "@/lib/trip-formatters"

export default function ActiveRouteTracking() {
  const { tripId } = useParams()
  // Page load reads the cached snapshot; only the "Localizar Motorista" button sets
  // this flag so the next request fetches a live GPS position from the provider.
  const refreshNext = useRef(false)
  const load = useCallback((options) => {
    const refresh = refreshNext.current
    refreshNext.current = false
    return getTripDetails(tripId, { ...options, refresh })
  }, [tripId])
  const { data: trip, pending, error, reload } = useTripRequest(load, tripId)
  const timeline = trip?.timeline ?? []
  const events = trip?.events ?? []
  const isLate = trip?.sla === "ATRASADO" || trip?.risk === "CRITIC"
  const dotColor = (status) => status === "CONCLUIDO" || status === "CONCLUIDA" ? "bg-emerald-500" : status === "EM_TRANSITO" ? "bg-amber-500 ring-4 ring-amber-50" : "bg-slate-300"
  const handlePing = useCallback(() => {
    refreshNext.current = true
    reload()
  }, [reload])

  return (
    <AppShell title={`Monitoramento: ${trip?.reference || tripId || "Viagem"}`}>
      <div className="mx-auto max-w-7xl space-y-6">
        
        {/* HEADER */}
        <div className="flex items-center justify-between border-b border-slate-100 pb-4">
          <Link to="/active-routes">
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Rotas Ativas
            </Button>
          </Link>
          <Button className="h-9 bg-emerald-600 text-xs font-bold text-white hover:bg-emerald-700" onClick={handlePing} disabled={pending}>
            <RefreshCw size={14} className={`mr-1.5 ${pending ? 'animate-spin' : ''}`} />
            {pending ? "Localizando..." : "Localizar Motorista"}
          </Button>
        </div>

        {pending && <p className="flex items-center gap-2 text-sm text-slate-500"><Loader2 size={16} className="animate-spin" /> Carregando monitoramento...</p>}
        {error && <p role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{error.message || "Não foi possível carregar a viagem."}</p>}
        {trip?.stale && <p role="alert" className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">{trip.warning || "Dados de monitoramento desatualizados."}</p>}

        {/* TOP KPI BAR (Incluindo SLA dinâmico) */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                <div className="h-10 w-10 bg-blue-50 text-blue-600 rounded-lg flex items-center justify-center"><ArrowRightLeft size={20}/></div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">Progresso da Rota</p>
                    <p className="text-sm font-bold text-slate-900">{formatTripNumber(trip?.traveled, " km")} / {formatTripNumber(trip?.totalDistance, " km")}</p>
                </div>
            </div>
            
            {/* KPI DE SLA DINÂMICO */}
            <div className={`bg-white border rounded-xl p-4 flex items-center gap-3 transition-colors ${isLate ? 'border-amber-200 bg-amber-50/50' : 'border-slate-200'}`}>
                <div className={`h-10 w-10 rounded-lg flex items-center justify-center ${isLate ? 'bg-amber-100 text-amber-600' : 'bg-indigo-50 text-indigo-600'}`}>
                    <Target size={20}/>
                </div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">Conf. SLA</p>
                    <p className="text-sm font-black text-slate-900">{tripStatusLabel(trip?.sla)}</p>
                </div>
            </div>

            <div className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                <div className="h-10 w-10 bg-purple-50 text-purple-600 rounded-lg flex items-center justify-center"><Scale size={20}/></div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">Utilização Carga</p>
                    <p className="text-sm font-bold text-slate-900">{formatTripNumber(trip?.utilization, "%")}</p>
                </div>
            </div>
            <div className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                <div className="h-10 w-10 bg-emerald-50 text-emerald-600 rounded-lg flex items-center justify-center"><Clock size={20}/></div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">ETA Previsto</p>
                    <p className="text-sm font-bold text-slate-900">{formatTripDate(trip?.eta)}</p>
                </div>
            </div>
        </div>

        {/* DASHBOARD GRID */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
            
            <div className="lg:col-span-8 space-y-6">
                <div className="bg-white border border-slate-200 rounded-xl p-6">
                    <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700 mb-6 flex items-center gap-2">
                        <MapPinned size={14} /> Histórico de Paradas
                    </h3>
                    <div className="relative border-l-2 border-slate-100 ml-3 space-y-8">
                        {!timeline.length ? <p className="pl-6 text-sm text-slate-500">Nenhuma parada disponível.</p> : timeline.map((stop) => <div key={stop.id} className="relative pl-6"><div className={`absolute -left-[9px] top-0 h-4 w-4 rounded-full border-4 border-white ${dotColor(stop.status)}`} />
                            <p className="text-sm font-bold text-slate-800">{stop.city}</p>
                            <p className={`text-xs font-bold ${stop.status === "EM_TRANSITO" ? "text-amber-600" : "text-slate-500"}`}>{tripStatusLabel(stop.status)} • {stop.actions.map((action) => action.type || action.product).filter(Boolean).join(" · ") || "Sem ações"}</p>
                            <p className="text-xs text-slate-500">{stop.actions[0]?.completedAt ? `Realizado: ${formatTripDate(stop.actions[0].completedAt)}` : `Prazo: ${formatTripDate(stop.actions[0]?.deadline)}`}</p>
                        </div>)}
                    </div>
                </div>

                <div className="bg-white border border-slate-200 rounded-xl p-6">
                    <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700 mb-4">Log de Eventos</h3>
                    <div className="space-y-4">
                        {!events.length ? <p className="text-sm text-slate-500">Nenhum evento registrado.</p> : events.map((event) => <div key={event.id} className="flex gap-4 items-center text-sm p-3 bg-slate-50 rounded-lg border border-slate-100">
                             <Clock className="text-slate-400" size={16}/><span className="text-slate-600 font-medium">{formatTripDate(event.timestamp)}</span><Badge variant="secondary" className="bg-indigo-100 text-indigo-700 border-none">{event.message}</Badge>
                        </div>)}
                    </div>
                </div>
            </div>

            <div className="lg:col-span-4 space-y-6">
                {/* SNAPSHOT MAP CARD (Ideia 1 implementada) */}
                <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
                    <div className="p-4 border-b border-slate-100 flex items-center justify-between">
                        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700 flex items-center gap-2">
                            <MapPin size={14} className="text-rose-500" /> Contexto Geográfico
                        </h3>
                        <span className="text-[10px] font-bold text-slate-400">Último Ping: {formatTripDate(trip?.lastPing)}</span>
                    </div>
                    {/* Visualização estilizada do mapa */}
                    <div className="h-[200px] bg-slate-50 relative flex items-center justify-center">
                        <div className="absolute inset-0 opacity-20" style={{ backgroundImage: 'radial-gradient(#cbd5e1 1px, transparent 1px)', backgroundSize: '20px 20px' }} />
                        <div className="relative w-full h-full flex items-center justify-center">
                             {/* Linha da Rota */}
                             <svg className="absolute w-full h-full" viewBox="0 0 400 200">
                                <path d="M50 150 Q 200 50 350 150" fill="none" stroke="#94a3b8" strokeWidth="4" strokeDasharray="6 6" />
                             </svg>
                             {/* Ponto do Motorista */}
                             <div className="absolute animate-pulse flex flex-col items-center">
                                <div className="h-4 w-4 bg-rose-600 rounded-full border-2 border-white shadow-lg" />
                                <span className="bg-white text-[9px] font-bold px-2 py-0.5 rounded shadow mt-1 text-rose-700">{trip?.latitude == null ? "Sem posição" : `${trip.latitude.toFixed(4)}, ${trip.longitude?.toFixed(4)}`}</span>
                             </div>
                        </div>
                    </div>
                </div>

                <div className="bg-white border border-slate-200 rounded-xl p-5 space-y-4">
                    <div className="flex items-center gap-3">
                        <div className="h-10 w-10 bg-slate-100 rounded-lg flex items-center justify-center text-slate-500"><Truck size={20} /></div>
                        <div>
                            <p className="text-xs font-bold text-slate-800">{trip?.carrier || "Não informado"}</p>
                            <p className="text-[10px] text-slate-400 font-mono">Placa: {trip?.plate || "Não informado"}</p>
                        </div>
                    </div>
                    <div className="space-y-3 pt-4 border-t border-slate-100">
                        <div className="flex items-center gap-2 text-xs text-slate-600"><User size={14} className="text-slate-400" /> {trip?.driver?.name || "Não informado"}</div>
                        <div className="flex items-center gap-2 text-xs text-slate-600"><Phone size={14} className="text-slate-400" /> {trip?.driver?.phone || "Não informado"}</div>
                    </div>
                </div>
            </div>
        </div>
      </div>
    </AppShell>
  )
}