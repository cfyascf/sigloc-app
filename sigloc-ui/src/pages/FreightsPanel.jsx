import { useState } from "react"
import {
  Search,
  Clock,
  TrendingDown,
  ArrowRight,
  SlidersHorizontal,
  Pencil,
  Trash2,
  Check,
  X,
  AlertCircle,
  Plus,
} from "lucide-react"
import { Link } from "react-router-dom"
import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { segmentPlansMock } from "@/constants/logistics-mock"
import { RISK } from "@/constants/risk"

const formatCurrency = (value) => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(value)

const getRiskBadge = (risk) => {
  const styles = {
    [RISK.NORMAL]: "bg-emerald-50 text-emerald-700 border-emerald-200",
    [RISK.WARNING]: "bg-amber-50 text-amber-700 border-amber-200",
    [RISK.CRITIC]: "bg-rose-50 text-rose-700 border-rose-200",
  }
  return <Badge variant="outline" className={`rounded-full text-[10px] uppercase font-bold tracking-wider px-2 py-0 ${styles[risk] || "bg-slate-50"}`}>{risk}</Badge>
}

export default function FreightsPanel() {
  const [leiloes, setLeiloes] = useState(segmentPlansMock.filter((s) => s.status !== "Em montagem"))
  const [deletingId, setDeletingId] = useState(null)
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [searchTerm, setSearchTerm] = useState("")

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

  const filteredLeiloes = leiloes.filter((l) => {
    const term = searchTerm.toLowerCase()
    return l.id.toLowerCase().includes(term) || l.name.toLowerCase().includes(term)
  })

  return (
    <AppShell title="Painel de Leilões">
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-7xl flex-col gap-4 overflow-hidden">
        <div className="flex shrink-0 flex-col justify-between gap-4 border-b border-slate-100 pt-1 pb-3 md:flex-row md:items-center">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">
              Leilões em Andamento
            </h1>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-72">
              <Search className="absolute top-2.5 left-3 h-4 w-4 text-slate-400" />
              <Input
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por ID ou trecho..."
                className="h-9 border-slate-200 bg-white pl-9 text-xs"
              />
            </div>
            <Button
              variant="outline"
              className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:bg-slate-50"
            >
              <SlidersHorizontal size={14} className="mr-1.5 text-slate-500" />{" "}
              Filtros
            </Button>
            <Button
              asChild
              className="h-9 bg-blue-600 text-xs font-semibold text-white hover:bg-blue-700"
            >
              <Link
                to="/load-management"
                state={{ isSelectingForAuction: true }}
              >
                <Plus size={14} className="mr-1.5" /> Criar Leilão
              </Link>
            </Button>
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">
            <div className="grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-3">
              {filteredLeiloes.map((l) => {
                const isEditing = editingId === l.id
                const isDeleting = deletingId === l.id

                return (
                  <div
                    key={l.id}
                    className="group relative flex flex-col overflow-hidden rounded-xl border border-slate-200 bg-white transition-all"
                  >
                    {/* CABEÇALHO DO CARD - Azulado no Edit Mode */}
                    <div
                      className={`flex min-h-[44px] items-center justify-between border-b px-4 py-3 ${isEditing ? "border-blue-100 bg-blue-50/50" : "border-slate-100 bg-slate-50/50"}`}
                    >
                      <div className="relative h-5 w-full">
                        {isDeleting ? (
                          <div className="absolute inset-0 flex animate-in items-center gap-2 fade-in">
                            <span className="text-[10px] font-bold text-rose-600">
                              Excluir?
                            </span>
                            <button
                              onClick={() => setDeletingId(null)}
                              className="text-slate-400 hover:text-slate-600"
                            >
                              <X size={14} />
                            </button>
                            <button
                              onClick={() => handleDelete(l.id)}
                              className="text-[10px] font-bold text-rose-600 hover:text-rose-700"
                            >
                              Sim
                            </button>
                          </div>
                        ) : (
                          <>
                            <span className="absolute inset-0 flex items-center font-mono text-[10px] font-bold text-slate-500 opacity-100 transition-opacity duration-200 group-hover:opacity-0">
                              {l.id}
                            </span>

                            <div className="absolute inset-0 hidden items-center gap-1 transition-opacity duration-200 group-hover:flex">
                              <Button
                                size="icon"
                                variant="ghost"
                                className="h-6 w-6 text-blue-600 hover:bg-blue-100"
                                onClick={() => startEditing(l)}
                              >
                                <Pencil size={12} />
                              </Button>
                              <Button
                                size="icon"
                                variant="ghost"
                                className="h-6 w-6 text-rose-600 hover:bg-rose-100"
                                onClick={() => setDeletingId(l.id)}
                              >
                                <Trash2 size={12} />
                              </Button>
                            </div>
                          </>
                        )}
                      </div>

                      {getRiskBadge(l.risk)}
                    </div>

                    {/* CORPO DO CARD - Altura mínima fixa para estabilidade */}
                    <div className="flex min-h-[220px] flex-1 flex-col">
                      {isDeleting ? (
                        <div className="flex flex-1 items-center justify-center bg-rose-50/30 p-6 text-rose-500">
                          <AlertCircle size={32} />
                        </div>
                      ) : isEditing ? (
                        <div className="flex h-full animate-in flex-col space-y-3 bg-white p-4 duration-200 fade-in">
                          <div className="space-y-1">
                            <label className="text-[10px] font-bold text-slate-500 uppercase">
                              Nome do Leilão
                            </label>
                            <Input
                              value={editForm.name}
                              onChange={(e) =>
                                setEditForm({
                                  ...editForm,
                                  name: e.target.value,
                                })
                              }
                              className="h-8 border-slate-200 text-xs"
                            />
                          </div>
                          <div className="space-y-1">
                            <label className="text-[10px] font-bold text-slate-500 uppercase">
                              Prazo (Deadline)
                            </label>
                            <Input
                              value={editForm.bidDeadline}
                              onChange={(e) =>
                                setEditForm({
                                  ...editForm,
                                  bidDeadline: e.target.value,
                                })
                              }
                              className="h-8 border-slate-200 text-xs"
                            />
                          </div>
                          <div className="mt-auto flex justify-end gap-2 border-t border-slate-100 pt-4">
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => setEditingId(null)}
                              className="h-8 text-xs"
                            >
                              Cancelar
                            </Button>
                            <Button
                              size="sm"
                              onClick={saveEdit}
                              className="h-8 bg-blue-600 text-xs text-white hover:bg-blue-700"
                            >
                              Salvar
                            </Button>
                          </div>
                        </div>
                      ) : (
                        <div className="flex flex-1 flex-col p-4">
                          <div className="mb-4">
                            <h3 className="line-clamp-1 text-sm font-bold text-slate-800">
                              {l.name}
                            </h3>
                            <p className="mt-1 truncate text-xs font-medium text-slate-500">
                              {l.stops.join(" ➔ ")}
                            </p>
                          </div>

                          <div className="mb-4 grid grid-cols-2 gap-3">
                            <div className="rounded-lg border border-slate-100 bg-slate-50 p-2.5">
                              <p className="text-[10px] font-bold tracking-wider text-slate-400 uppercase">
                                Melhor Lance
                              </p>
                              <p className="mt-0.5 font-mono text-sm font-black text-emerald-600">
                                {l.bestBid
                                  ? formatCurrency(l.bestBid)
                                  : "Sem ofertas"}
                              </p>
                            </div>
                            <div className="rounded-lg border border-slate-100 bg-slate-50 p-2.5">
                              <p className="text-[10px] font-bold tracking-wider text-slate-400 uppercase">
                                Total de Lances
                              </p>
                              <div className="mt-0.5 flex items-center">
                                <TrendingDown
                                  size={14}
                                  className="mr-1.5 text-blue-500"
                                />
                                <p className="text-sm font-bold text-slate-700">
                                  {l.totalBids}
                                </p>
                              </div>
                            </div>
                          </div>

                          <div className="mt-auto flex items-center justify-between border-t border-slate-100 pt-4">
                            <div className="flex items-center rounded bg-amber-50 px-2 py-1 text-xs font-semibold text-amber-600">
                              <Clock size={12} className="mr-1.5" />{" "}
                              {l.bidDeadline}
                            </div>
                            <Button
                              asChild
                              size="sm"
                              variant="ghost"
                              className="h-8 px-2 text-xs font-bold text-blue-600 hover:bg-blue-50 hover:text-blue-700"
                            >
                              <Link to={`/auction-bids/${l.id}`}>
                                Analisar{" "}
                                <ArrowRight size={14} className="ml-1" />
                              </Link>
                            </Button>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  )
}