import { useCallback, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Search, Filter, Truck, ChevronRight, RefreshCw, Loader2, Archive } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { useTripRequest } from "@/hooks/use-trip-request"
import { formatTripDate, tripStatusLabel } from "@/lib/trip-formatters"

/** Badge colour per trip status: late is rose, finished states are neutral, everything else emerald. */
function statusBadgeClass(status) {
  if (status === "ATRASADO") return "bg-rose-50 text-rose-600"
  if (status === "ENTREGUE" || status === "CONCLUIDO") return "bg-emerald-50 text-emerald-600"
  if (status === "CANCELADA") return "bg-slate-100 text-slate-500"
  return "bg-emerald-50 text-emerald-600"
}

/**
 * Paged list of trips shared by the contractor and carrier screens.
 * `loadTrips(query, options)` fetches a page and `getDetailPath(trip)` returns where a row navigates to.
 * When `historyLoadTrips` is provided, an "Ativas"/"Histórico" tab bar is shown and the history tab
 * loads finished trips (delivered/cancelled) with the status/risk filters hidden.
 */
export function TripListView({ title, heading, historyHeading, searchPlaceholder, loadTrips, historyLoadTrips, getDetailPath, getTitle }) {
  const navigate = useNavigate()
  const [selectedTab, setSelectedTab] = useState("active")
  const [search, setSearch] = useState("")
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [query, setQuery] = useState({ search: "", status: "", risk: "", page: 1, pageSize: 20 })
  const hasHistory = typeof historyLoadTrips === "function"
  const isHistory = hasHistory && selectedTab === "history"
  useEffect(() => {
    const timeout = setTimeout(() => setQuery((current) => ({ ...current, search: search.trim(), page: 1 })), 300)
    return () => clearTimeout(timeout)
  }, [search])
  const handleTabChange = (value) => {
    setSelectedTab(value)
    setSearch("")
    setFiltersOpen(false)
    setQuery({ search: "", status: "", risk: "", page: 1, pageSize: 20 })
  }
  const activeLoader = isHistory ? historyLoadTrips : loadTrips
  const load = useCallback((options) => activeLoader(query, options), [activeLoader, query])
  const { data, pending, error, reload } = useTripRequest(load, `${selectedTab}:${JSON.stringify(query)}`)
  const rotas = data?.trips ?? []

  return (
    <AppShell title={title}>
      <div className="space-y-6">

        {/* HEADER */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">{isHistory ? (historyHeading || "Histórico de Viagens") : heading}</h1>
          </div>

          <div className="flex items-center gap-3">
             <div className="relative w-64">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder={searchPlaceholder} className="h-9 border-slate-200 bg-white pl-9 text-xs" />
            </div>
            {!isHistory && (
              <Button variant="outline" onClick={() => setFiltersOpen((open) => !open)} className="h-9 border-slate-200 text-xs font-semibold bg-white">
                 <Filter size={14} className="mr-2" /> Filtros
              </Button>
            )}
          </div>
        </div>

        {hasHistory && (
          <Tabs value={selectedTab} onValueChange={handleTabChange}>
            <TabsList className="bg-slate-100">
              <TabsTrigger value="active" className="text-xs font-semibold">Ativas</TabsTrigger>
              <TabsTrigger value="history" className="text-xs font-semibold">
                <Archive size={13} className="mr-1.5" /> Histórico
              </TabsTrigger>
            </TabsList>
          </Tabs>
        )}

        {!isHistory && filtersOpen && <div className="flex flex-wrap items-center gap-3 rounded-xl border border-slate-200 bg-white p-3 text-xs">
          <select aria-label="Filtrar por status" value={query.status} onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, page: 1 }))} className="h-9 rounded-md border border-slate-200 px-2"><option value="">Todos os status</option><option value="AGUARDANDO_COLETA">Aguardando coleta</option><option value="EM_TRANSITO">Em trânsito</option><option value="ATRASADO">Atrasado</option></select>
          <select aria-label="Filtrar por risco" value={query.risk} onChange={(event) => setQuery((current) => ({ ...current, risk: event.target.value, page: 1 }))} className="h-9 rounded-md border border-slate-200 px-2"><option value="">Todos os riscos</option><option value="NORMAL">Normal</option><option value="WARNING">Atenção</option><option value="CRITIC">Crítico</option><option value="NAO_MONITORADO">Sem monitoramento</option></select>
          <Button variant="outline" className="h-9 text-xs" onClick={reload} disabled={pending}>Atualizar</Button>
        </div>}

        {/* LISTAGEM EM CARDS */}
        <div className="space-y-3" aria-live="polite">
          {pending ? <p className="flex items-center gap-2 p-4 text-sm text-slate-500"><Loader2 size={16} className="animate-spin" /> Carregando viagens…</p> : error ? <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{error.message || "Não foi possível carregar as viagens."}</div> : !rotas.length ? <p className="rounded-xl border border-dashed border-slate-300 p-5 text-sm text-slate-500">{isHistory ? "Nenhuma viagem no histórico." : "Nenhuma viagem ativa encontrada."}</p> : rotas.map((rota) => (
            <button
              key={rota.id}
              type="button"
              onClick={() => navigate(getDetailPath(rota))}
              className="group flex w-full items-center gap-4 rounded-xl border border-slate-200 bg-white py-4 pr-4 pl-6 transition-all hover:border-blue-300 hover:ring-1 hover:ring-blue-100 cursor-pointer"
            >
              {/* ID + STATUS */}
              <div className="flex w-[140px] shrink-0 flex-col items-start gap-1 text-left">
                <span className="font-mono text-sm font-black text-slate-900">{rota.reference || "Viagem"}</span>
                <Badge variant="outline" className={`text-[10px] font-bold uppercase border-none px-1.5 py-0 ${statusBadgeClass(rota.status)}`}>
                    {tripStatusLabel(rota.status)}
                </Badge>
              </div>

              {/* IDENTIFICAÇÃO + VEÍCULO */}
              <div className="w-[200px] min-w-0 shrink-0 text-left">
                <p className="truncate text-sm font-semibold text-slate-800">{getTitle(rota)}</p>
                <div className="flex items-center gap-2 text-[11px] text-slate-400 mt-0.5">
                    <Truck size={12} /> {rota.plate || "Não informado"}
                </div>
              </div>

              {/* PROGRESSO */}
              <div className="w-[160px] shrink-0">
                <div className="flex justify-between text-[10px] font-bold text-slate-400 mb-1">
                    <span>Progresso</span>
                    <span>{rota.progress == null ? "—" : `${rota.progress}%`}</span>
                </div>
                <div className="h-1.5 w-full bg-slate-100 rounded-full overflow-hidden">
                    <div className="h-full bg-blue-600" style={{ width: `${Math.max(0, Math.min(100, rota.progress ?? 0))}%` }} />
                </div>
              </div>

              {/* DADOS DE MONITORAMENTO */}
              <div className="ml-auto flex shrink-0 gap-4 whitespace-nowrap text-right">
                <div className="w-[140px]">
                    <span className="block text-[10px] text-slate-400 uppercase font-bold">ETA</span>
                    <span className="text-sm font-bold text-slate-700 font-mono">{formatTripDate(rota.eta)}</span>
                </div>
                <div className="w-[160px]">
                    <span className="block text-[10px] text-slate-400 uppercase font-bold">Ping</span>
                    <span className="text-sm font-bold text-slate-700 font-mono flex justify-end items-center gap-1">
                        <RefreshCw size={10} /> {formatTripDate(rota.lastPing)}
                    </span>
                </div>
              </div>

              {/* AÇÃO */}
              <div className="flex w-[40px] shrink-0 justify-end">
                <div className="h-8 w-8 rounded-full bg-slate-50 flex items-center justify-center group-hover:bg-blue-50 transition-colors">
                    <ChevronRight size={16} className="text-slate-400 group-hover:text-blue-600" />
                </div>
              </div>
            </button>
          ))}
        </div>
        <div className="flex items-center justify-between text-xs text-slate-500">
          <span>Página {data?.page ?? query.page} de {Math.max(1, data?.totalPages ?? 1)} • {data?.total ?? 0} viagens</span>
          <div className="flex gap-2"><Button variant="outline" size="sm" disabled={pending || query.page <= 1} onClick={() => setQuery((current) => ({ ...current, page: current.page - 1 }))}>Anterior</Button><Button variant="outline" size="sm" disabled={pending || !data || query.page >= data.totalPages} onClick={() => setQuery((current) => ({ ...current, page: current.page + 1 }))}>Próxima</Button></div>
        </div>
      </div>
    </AppShell>
  )
}
