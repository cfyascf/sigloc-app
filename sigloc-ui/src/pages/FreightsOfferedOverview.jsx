import { useCallback, useEffect, useState } from "react"
import { Search, Clock, TrendingDown, ArrowRight, SlidersHorizontal, Pencil, Trash2, X, AlertCircle, Loader2 } from "lucide-react"
import { Link } from "react-router-dom"
import AppShell from "@/components/app-shell"
import { FormAlert } from "@/components/auth/FormAlert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useAsyncAction } from "@/hooks/use-async-action"
import { listAuctions } from "@/services/auction-service"
import { RISK } from "@/constants/risk"

const SEARCH_DEBOUNCE_MS = 350
const PAGE_SIZE = 50

const formatCurrency = (value) => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(value)

const formatDeadline = (value) => {
  if (!value) {
    return "Sem prazo"
  }

  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) {
    return "Sem prazo"
  }

  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date)
}

/** Splits the itinerary summary ("A → B → C") into individual stops. */
const toStops = (summary) => {
  if (!summary) {
    return []
  }

  return summary
    .split(/\s*(?:➔|→|->|,)\s*/)
    .map((stop) => stop.trim())
    .filter(Boolean)
}

/** Maps an auction list item DTO to the card view model used by the JSX. */
const toCardModel = (item) => ({
  id: item.id,
  name: item.itinerarySummary || item.itineraryWithStates || item.id,
  stops: toStops(item.itineraryWithStates || item.itinerarySummary),
  bestBid: item.bidMetrics?.bestBid ?? null,
  totalBids: item.bidMetrics?.totalBids ?? 0,
  bidDeadline: formatDeadline(item.expiresAt),
  risk: item.riskIndicator || RISK.NORMAL,
})

const getRiskBadge = (risk) => {
  const styles = {
    [RISK.NORMAL]: "bg-emerald-50 text-emerald-700 border-emerald-200",
    [RISK.WARNING]: "bg-amber-50 text-amber-700 border-amber-200",
    [RISK.CRITIC]: "bg-rose-50 text-rose-700 border-rose-200",
  }
  return <Badge variant="outline" className={`rounded-full text-[10px] uppercase font-bold tracking-wider px-2 py-0 ${styles[risk] || "bg-slate-50"}`}>{risk}</Badge>
}

export default function FreightsOfferedOverview() {
  const [leiloes, setLeiloes] = useState([])
  const [deletingId, setDeletingId] = useState(null)
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [searchTerm, setSearchTerm] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")

  const listAction = useAsyncAction((query) => listAuctions(query))
  const listPending = listAction.pending

  useEffect(() => {
    const timer = setTimeout(
      () => setDebouncedSearch(searchTerm.trim()),
      SEARCH_DEBOUNCE_MS
    )
    return () => clearTimeout(timer)
  }, [searchTerm])

  const loadList = useCallback(async () => {
    const result = await listAction.run({
      search: debouncedSearch || undefined,
      page: 1,
      pageSize: PAGE_SIZE,
    })
    if (result.ok) {
      setLeiloes(result.data.items.map(toCardModel))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch])

  useEffect(() => {
    loadList()
  }, [loadList])

  const handleDelete = (id) => {
    setLeiloes(prev => prev.filter(l => l.id !== id))
    setDeletingId(null)
  }

  const startEditing = (l) => {
    setEditingId(l.id)
    setEditForm({ ...l })
  }

  const saveEdit = () => {
    setLeiloes(prev => prev.map(l => l.id === editingId ? { ...l, ...editForm } : l))
    setEditingId(null)
    setEditForm(null)
  }

  const filteredLeiloes = leiloes

  return (
    <AppShell title="Painel de Leilões">
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-7xl flex-col gap-4 overflow-hidden">
        
        <div className="flex shrink-0 flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-3 pt-1">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">Leilões em Andamento</h1>
          </div>
          
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-72">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por ID ou trecho..."
                className="h-9 border-slate-200 bg-white pl-9 text-xs"
              />
            </div>
            <Button variant="outline" className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:bg-slate-50">
              <SlidersHorizontal size={14} className="mr-1.5 text-slate-500" /> Filtros
            </Button>
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">

            {listPending ? (
              <div className="flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white p-10 text-sm text-slate-500">
                <Loader2 size={16} className="animate-spin" /> Carregando leilões...
              </div>
            ) : listAction.error ? (
              <div className="space-y-3 rounded-xl border border-slate-200 bg-white p-6">
                <FormAlert message={listAction.error.message} />
                <Button
                  variant="outline"
                  size="sm"
                  onClick={loadList}
                  className="h-8 text-xs font-semibold"
                >
                  Tentar novamente
                </Button>
              </div>
            ) : filteredLeiloes.length === 0 ? (
              <div className="rounded-xl border border-dashed border-slate-200 bg-white p-10 text-center text-sm text-slate-500">
                Nenhum leilão encontrado.
              </div>
            ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              {filteredLeiloes.map((l) => {
                const isEditing = editingId === l.id
                const isDeleting = deletingId === l.id

                return (
                  <div key={l.id} className="group relative flex flex-col rounded-xl border border-slate-200 bg-white transition-all overflow-hidden">
                    
                    {/* CABEÇALHO DO CARD - Azulado no Edit Mode */}
                    <div className={`flex items-center justify-between border-b px-4 py-3 min-h-[44px] ${isEditing ? "border-blue-100 bg-blue-50/50" : "border-slate-100 bg-slate-50/50"}`}>
                      
                      <div className="relative h-5 w-full">
                        {isDeleting ? (
                          <div className="absolute inset-0 flex items-center gap-2 animate-in fade-in">
                            <span className="text-[10px] font-bold text-rose-600">Excluir?</span>
                            <button onClick={() => setDeletingId(null)} className="text-slate-400 hover:text-slate-600"><X size={14}/></button>
                            <button onClick={() => handleDelete(l.id)} className="text-rose-600 hover:text-rose-700 font-bold text-[10px]">Sim</button>
                          </div>
                        ) : (
                          <>
                            <span className="absolute inset-0 flex items-center font-mono text-[10px] font-bold text-slate-500 opacity-100 group-hover:opacity-0 transition-opacity duration-200">
                              {l.id}
                            </span>
                            
                            <div className="absolute inset-0 hidden group-hover:flex items-center gap-1 transition-opacity duration-200">
                              <Button size="icon" variant="ghost" className="h-6 w-6 text-blue-600 hover:bg-blue-100" onClick={() => startEditing(l)}>
                                <Pencil size={12} />
                              </Button>
                              <Button size="icon" variant="ghost" className="h-6 w-6 text-rose-600 hover:bg-rose-100" onClick={() => setDeletingId(l.id)}>
                                <Trash2 size={12} />
                              </Button>
                            </div>
                          </>
                        )}
                      </div>

                      {getRiskBadge(l.risk)}
                    </div>

                    {/* CORPO DO CARD - Altura mínima fixa para estabilidade */}
                    <div className="flex-1 min-h-[220px] flex flex-col">
                      {isDeleting ? (
                        <div className="flex-1 p-6 flex items-center justify-center text-rose-500 bg-rose-50/30">
                          <AlertCircle size={32} />
                        </div>
                      ) : isEditing ? (
                        <div className="p-4 space-y-3 bg-white animate-in fade-in duration-200 h-full flex flex-col">
                          <div className="space-y-1">
                            <label className="text-[10px] font-bold uppercase text-slate-500">Nome do Leilão</label>
                            <Input value={editForm.name} onChange={(e) => setEditForm({...editForm, name: e.target.value})} className="h-8 text-xs border-slate-200" />
                          </div>
                          <div className="space-y-1">
                            <label className="text-[10px] font-bold uppercase text-slate-500">Prazo (Deadline)</label>
                            <Input value={editForm.bidDeadline} onChange={(e) => setEditForm({...editForm, bidDeadline: e.target.value})} className="h-8 text-xs border-slate-200" />
                          </div>
                          <div className="flex gap-2 justify-end mt-auto pt-4 border-t border-slate-100">
                            <Button size="sm" variant="ghost" onClick={() => setEditingId(null)} className="h-8 text-xs">Cancelar</Button>
                            <Button size="sm" onClick={saveEdit} className="h-8 text-xs bg-blue-600 hover:bg-blue-700 text-white">Salvar</Button>
                          </div>
                        </div>
                      ) : (
                        <div className="flex flex-col flex-1 p-4">
                          <div className="mb-4">
                            <h3 className="text-sm font-bold text-slate-800 line-clamp-1">{l.name}</h3>
                            <p className="text-xs font-medium text-slate-500 mt-1 truncate">{l.stops.join(" ➔ ")}</p>
                          </div>

                          <div className="grid grid-cols-2 gap-3 mb-4">
                            <div className="rounded-lg border border-slate-100 bg-slate-50 p-2.5">
                              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Melhor Lance</p>
                              <p className="mt-0.5 font-mono text-sm font-black text-emerald-600">
                                {l.bestBid ? formatCurrency(l.bestBid) : "Sem ofertas"}
                              </p>
                            </div>
                            <div className="rounded-lg border border-slate-100 bg-slate-50 p-2.5">
                              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Total de Lances</p>
                              <div className="flex items-center mt-0.5">
                                <TrendingDown size={14} className="text-blue-500 mr-1.5" />
                                <p className="text-sm font-bold text-slate-700">{l.totalBids}</p>
                              </div>
                            </div>
                          </div>

                          <div className="mt-auto pt-4 border-t border-slate-100 flex items-center justify-between">
                            <div className="flex items-center text-xs font-semibold text-amber-600 bg-amber-50 px-2 py-1 rounded">
                              <Clock size={12} className="mr-1.5" /> {l.bidDeadline}
                            </div>
                            <Button asChild size="sm" variant="ghost" className="h-8 text-xs font-bold text-blue-600 hover:text-blue-700 hover:bg-blue-50 px-2">
                              <Link to={`/auction-bids/${l.id}`}>Analisar <ArrowRight size={14} className="ml-1" /></Link>
                            </Button>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
            )}
          </div>
        </div>
      </div>
    </AppShell>
  )
}