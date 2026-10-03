import { useCallback, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Search, Filter, Truck, ChevronRight, RefreshCw, Loader2 } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useTripRequest } from "@/hooks/use-trip-request"
import { listActiveTrips } from "@/services/trip-service"
import { formatTripDate, tripStatusLabel } from "@/lib/trip-formatters"

export default function ActiveRoutes() {
  const navigate = useNavigate()
  const [search, setSearch] = useState("")
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [query, setQuery] = useState({ search: "", status: "", risk: "", page: 1, pageSize: 20 })
  useEffect(() => {
    const timeout = setTimeout(() => setQuery((current) => ({ ...current, search: search.trim(), page: 1 })), 300)
    return () => clearTimeout(timeout)
  }, [search])
  const load = useCallback((options) => listActiveTrips(query, options), [query])
  const { data, pending, error, reload } = useTripRequest(load, JSON.stringify(query))
  const rotas = data?.trips ?? []

  return (
    <AppShell title="Rotas Ativas">
      <div className="mx-auto max-w-5xl space-y-6">
        
        {/* HEADER */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">Rotas em Execução</h1>
          </div>
          
          <div className="flex items-center gap-3">
             <div className="relative w-64">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar rota ou transportadora..." className="h-9 border-slate-200 bg-white pl-9 text-xs" />
            </div>
            <Button variant="outline" onClick={() => setFiltersOpen((open) => !open)} className="h-9 border-slate-200 text-xs font-semibold bg-white">
               <Filter size={14} className="mr-2" /> Filtros
            </Button>
          </div>
        </div>

        {filtersOpen && <div className="flex flex-wrap items-center gap-3 rounded-xl border border-slate-200 bg-white p-3 text-xs">
          <select aria-label="Filtrar por status" value={query.status} onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, page: 1 }))} className="h-9 rounded-md border border-slate-200 px-2"><option value="">Todos os status</option><option value="AGUARDANDO_COLETA">Aguardando coleta</option><option value="EM_TRANSITO">Em trânsito</option><option value="ATRASADO">Atrasado</option></select>
          <select aria-label="Filtrar por risco" value={query.risk} onChange={(event) => setQuery((current) => ({ ...current, risk: event.target.value, page: 1 }))} className="h-9 rounded-md border border-slate-200 px-2"><option value="">Todos os riscos</option><option value="NORMAL">Normal</option><option value="CRITIC">Crítico</option><option value="NAO_MONITORADO">Sem monitoramento</option></select>
          <Button variant="outline" className="h-9 text-xs" onClick={reload} disabled={pending}>Atualizar</Button>
        </div>}

        {/* LISTAGEM EM CARDS */}
        <div className="space-y-3" aria-live="polite">
          {pending ? <p className="flex items-center gap-2 p-4 text-sm text-slate-500"><Loader2 size={16} className="animate-spin" /> Carregando viagens…</p> : error ? <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{error.message || "Não foi possível carregar as viagens."}</div> : !rotas.length ? <p className="rounded-xl border border-dashed border-slate-300 p-5 text-sm text-slate-500">Nenhuma viagem ativa encontrada.</p> : rotas.map((rota) => (
            <button
              key={rota.id}
              type="button"
              onClick={() => navigate(`/active-route-tracking/${rota.id}`)}
              className="group flex w-full items-center gap-6 rounded-xl border border-slate-200 bg-white p-4 transition-all hover:border-blue-300 hover:ring-1 hover:ring-blue-100 cursor-pointer"
            >
              {/* ID + STATUS */}
              <div className="w-[120px]">
                <div className="font-mono text-xs font-bold text-slate-500">{rota.reference || rota.id}</div>
                <Badge variant="outline" className={`mt-1 text-[10px] font-bold uppercase border-none px-1.5 py-0 ${rota.status === 'ATRASADO' ? 'bg-rose-50 text-rose-600' : 'bg-emerald-50 text-emerald-600'}`}>
                    {tripStatusLabel(rota.status)}
                </Badge>
              </div>

              {/* TRANSPORTADORA + VEÍCULO */}
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-slate-800">{rota.carrier || "Não informado"}</p>
                <div className="flex items-center gap-2 text-[11px] text-slate-400 mt-0.5">
                    <Truck size={12} /> {rota.plate || "Não informado"}
                </div>
              </div>

              {/* PROGRESSO */}
              <div className="w-[180px]">
                <div className="flex justify-between text-[10px] font-bold text-slate-400 mb-1">
                    <span>Progresso</span>
                    <span>{rota.progress == null ? "—" : `${rota.progress}%`}</span>
                </div>
                <div className="h-1.5 w-full bg-slate-100 rounded-full overflow-hidden">
                    <div className="h-full bg-blue-600" style={{ width: `${Math.max(0, Math.min(100, rota.progress ?? 0))}%` }} />
                </div>
              </div>

              {/* DADOS DE MONITORAMENTO */}
              <div className="w-[180px] grid grid-cols-2 gap-4 text-right">
                <div>
                    <span className="block text-[10px] text-slate-400 uppercase font-bold">ETA</span>
                    <span className="text-sm font-bold text-slate-700 font-mono">{formatTripDate(rota.eta)}</span>
                </div>
                <div>
                    <span className="block text-[10px] text-slate-400 uppercase font-bold">Ping</span>
                    <span className="text-sm font-bold text-slate-700 font-mono flex justify-end items-center gap-1">
                        <RefreshCw size={10} /> {formatTripDate(rota.lastPing)}
                    </span>
                </div>
              </div>

              {/* AÇÃO */}
              <div className="w-[40px] flex justify-end">
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