import { useState } from "react"
import {
  Plus,
  Search,
  Layers,
  ArrowRight,
  Pencil,
  Trash2,
  X,
  Save,
  AlertCircle,
  Scale,
  Box,
  CalendarClock,
  DollarSign,
  Gavel,
} from "lucide-react"
import { Link, useNavigate, useLocation } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { RISK } from "@/constants/risk"

export const availableRouteSegments = [
  {
    id: "TRC-1042",
    productName: "Peito de Frango Congelado",
    bodyType: "Frigorífico",
    loadType: "Paletizado",
    origin: "Curitiba, PR",
    destination: "São Paulo, SP",
    risk: RISK.CRITIC,
    pickupWindow: "Hoje até às 18:00h",
    weightKg: 12000,
    volumeM3: 45,
    distanceKm: 408,
    targetPrice: 4200,
  },
  {
    id: "TRC-1043",
    productName: "Laticínios Pasteurizados",
    bodyType: "Refrigerado",
    loadType: "Paletizado",
    origin: "São Paulo, SP",
    destination: "Campinas, SP",
    risk: RISK.WARNING,
    pickupWindow: "Amanhã até às 12:00h",
    weightKg: 9000,
    volumeM3: 28,
    distanceKm: 96,
    targetPrice: 1100,
  },
  {
    id: "TRC-1044",
    productName: "Eletrônicos de Alto Valor",
    bodyType: "Baú Sider",
    loadType: "Caixas Master",
    origin: "Campinas, SP",
    destination: "Ribeirão Preto, SP",
    risk: RISK.WARNING,
    pickupWindow: "07/06 às 08:00h",
    weightKg: 4800,
    volumeM3: 22,
    distanceKm: 223,
    targetPrice: 2800,
  },
  {
    id: "TRC-1045",
    productName: "Vacinas Influenza",
    bodyType: "Frigorífico",
    loadType: "Isotérmico",
    origin: "Ribeirão Preto, SP",
    destination: "Uberlândia, MG",
    risk: RISK.CRITIC,
    pickupWindow: "Hoje urgente até às 16:30h",
    weightKg: 3200,
    volumeM3: 18,
    distanceKm: 166,
    targetPrice: 3100,
  },
]

const formatWeight = (value) =>
  `${new Intl.NumberFormat("pt-BR").format(value)} kg`
const formatVolume = (value) =>
  `${new Intl.NumberFormat("pt-BR").format(value)} m³`
const formatCurrency = (value) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    maximumFractionDigits: 0,
  }).format(value)

const getRiskDotStyle = (risk) => {
  if (risk === RISK.CRITIC)
    return "bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.4)]"
  if (risk === RISK.WARNING)
    return "bg-amber-500 shadow-[0_0_8px_rgba(245,158,11,0.4)]"
  return "bg-slate-300"
}

const getRiskTextStyle = (risk) => {
  if (risk === RISK.CRITIC) return "text-rose-600 font-bold"
  if (risk === RISK.WARNING) return "text-amber-600 font-bold"
  return "text-slate-400"
}

export default function LoadManagement() {
  const navigate = useNavigate()
  const location = useLocation()

  // Detecta se a intenção de abertura veio orientada a Leilão
  const isForAuction = location.state?.isSelectingForAuction || false

  const [searchTerm, setSearchTerm] = useState("")
  const [isSelectionMode, setIsSelectionMode] = useState(isForAuction)
  const [selectedSegmentIds, setSelectedSegmentIds] = useState([])

  const [segments, setSegments] = useState(availableRouteSegments)
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [deletingId, setDeletingId] = useState(null)

  const visibleSegments = segments.filter((segment) => {
    const term = searchTerm.trim().toLowerCase()
    if (!term) return true
    return [
      segment.id,
      segment.productName,
      segment.bodyType,
      segment.origin,
      segment.destination,
    ]
      .join(" ")
      .toLowerCase()
      .includes(term)
  })

  const handleToggleSegment = (segmentId) => {
    setSelectedSegmentIds((prev) =>
      prev.includes(segmentId)
        ? prev.filter((id) => id !== segmentId)
        : [...prev, segmentId]
    )
  }

  const handleCancelSelection = () => {
    setIsSelectionMode(false)
    setSelectedSegmentIds([])
  }

  const handleProceed = () => {
    if (isForAuction) {
      // Avança para a criação de leilão passando o item escolhido
      navigate("/create-freight-auction", {
        state: { selectedSegmentId: selectedSegmentIds[0] },
      })
    } else {
      // Avança para a tela de montagem de rota padrão
      navigate("/create-route-workspace", {
        state: { selectedIds: selectedSegmentIds },
      })
    }
  }

  const startEditing = (segment) => {
    setEditingId(segment.id)
    setEditForm({ ...segment })
    setDeletingId(null)
  }

  const cancelEditing = () => {
    setEditingId(null)
    setEditForm(null)
  }

  const saveEdit = () => {
    setSegments((prev) => prev.map((s) => (s.id === editingId ? editForm : s)))
    setEditingId(null)
    setEditForm(null)
  }

  const confirmDelete = (id) => {
    setSegments((prev) => prev.filter((s) => s.id !== id))
    setDeletingId(null)
    setSelectedSegmentIds((prev) =>
      prev.filter((selectedId) => selectedId !== id)
    )
  }

  return (
    <AppShell
      title={
        isForAuction ? "Selecione o Trecho para o Leilão" : "Gestão de Trechos"
      }
    >
      <div className="mx-auto max-w-7xl space-y-4">
        {/* BARRA DE TOPO */}
        <div className="flex flex-col justify-between gap-4 border-b border-slate-100 pb-4 md:flex-row md:items-center">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">
              {isForAuction
                ? "Trechos Disponíveis para Leilão"
                : "Trechos Disponíveis"}
            </h1>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-72">
              <Search className="absolute top-2.5 left-3 h-4 w-4 text-slate-400" />
              <Input
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por trecho, produto ou cidade..."
                className="h-9 border-slate-200 bg-white pl-9 text-xs"
              />
            </div>

            {/* O mesmo botão apenas troca o texto e a cor com base no contexto */}
            <Button
              variant={isSelectionMode ? "secondary" : "outline"}
              className={
                isSelectionMode
                  ? "h-9 border-blue-200 bg-blue-50 text-xs font-semibold text-blue-700 hover:bg-blue-100"
                  : "h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700"
              }
              onClick={() =>
                isSelectionMode
                  ? handleCancelSelection()
                  : setIsSelectionMode(true)
              }
            >
              <Gavel size={14} className="mr-1.5" />
              {isSelectionMode
                ? "Cancelar"
                : "Criar Leilão"
                  }
            </Button>

            <Button
              asChild
              className="h-9 bg-blue-600 text-xs font-semibold text-white hover:bg-blue-700"
            >
              <Link to="/create-load">
                <Plus size={14} className="mr-1.5" /> Novo Trecho
              </Link>
            </Button>
          </div>
        </div>

        {/* BANNER CONTEXTUAL INSTRUTIVO */}
        {isSelectionMode && (
          <div
            className={`flex animate-in flex-col justify-between rounded-xl border p-3 px-4 text-sm duration-200 fade-in md:flex-row md:items-center ${
              selectedSegmentIds.length > 0
                ? "border-emerald-200 bg-emerald-50"
                : "border-blue-100 bg-blue-50"
            }`}
          >
            <div
              className={`flex items-center gap-2.5 ${selectedSegmentIds.length > 0 ? "text-emerald-900" : "text-blue-900"}`}
            >
              {selectedSegmentIds.length > 0 ? (
                <>
                  <span className="flex h-5 w-5 items-center justify-center rounded-md bg-emerald-600 text-[10px] font-black tracking-wider text-white">
                    {selectedSegmentIds.length}
                  </span>
                  <span className="text-xs font-medium text-slate-700">
                    Trecho selecionado! Clique ao lado para prosseguir com as
                    configurações.
                  </span>
                </>
              ) : (
                <>
                  <AlertCircle size={18} className="text-blue-600" />
                  <span className="text-xs font-medium text-slate-700">
                    {isForAuction
                      ? "Por favor, escolha o trecho que deseja enviar para o leilão marcando a caixa correspondente na lista abaixo."
                      : "Por favor, selecione os trechos desejados marcando as caixas de seleção na lista abaixo."}
                  </span>
                </>
              )}
            </div>

            {selectedSegmentIds.length > 0 && (
              <button
                onClick={handleProceed}
                className="mt-3 flex items-center justify-center gap-1.5 rounded-lg bg-emerald-600 px-4 py-2 text-xs font-bold text-white transition-colors hover:bg-emerald-700 md:mt-0"
              >
                {isForAuction ? "Configurar Leilão" : "Avançar Roteirização"}{" "}
                <ArrowRight size={14} className="animate-pulse" />
              </button>
            )}
          </div>
        )}

        {/* LISTAGEM DOS TRECHOS */}
        <div className="space-y-2">
          {visibleSegments.map((segment) => {
            const isChecked = selectedSegmentIds.includes(segment.id)
            const isEditingThis = editingId === segment.id
            const isDeletingThis = deletingId === segment.id

            if (isDeletingThis) {
              return (
                <div
                  key={segment.id}
                  className="flex w-full animate-in items-center justify-between gap-4 rounded-xl border border-rose-200 bg-rose-50/50 p-3.5 fade-in"
                >
                  <div className="flex items-center gap-3 pl-2 text-rose-700">
                    <AlertCircle size={16} />
                    <span className="text-sm font-bold">
                      Excluir permanentemente o trecho {segment.id}?
                    </span>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setDeletingId(null)}
                      className="h-8 border-slate-200 bg-white text-xs font-semibold text-slate-600"
                    >
                      Cancelar
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => confirmDelete(segment.id)}
                      className="h-8 bg-rose-600 text-xs font-bold text-white hover:bg-rose-700"
                    >
                      Sim, Excluir
                    </Button>
                  </div>
                </div>
              )
            }

            return (
              <div
                key={segment.id}
                className={`group relative flex w-full flex-col rounded-xl border transition-all ${
                  isChecked
                    ? "border-blue-300 bg-blue-50/40 ring-1 ring-blue-200"
                    : isEditingThis
                      ? "border-blue-300 bg-white"
                      : "border-slate-200 bg-white"
                }`}
              >
                {/* LINHA DO ITEM */}
                <div className="flex w-full items-center p-3.5 text-left">
                  {isSelectionMode && !isEditingThis && (
                    <div className="mr-4 flex items-center justify-center pl-1">
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => handleToggleSegment(segment.id)}
                        className="h-4 w-4 cursor-pointer rounded border-slate-300 accent-blue-600"
                      />
                    </div>
                  )}

                  <div className="grid min-w-0 flex-1 grid-cols-[110px_1.1fr_1.3fr_250px] items-center gap-6">
                    <div>
                      <div className="mb-1 flex items-center gap-2">
                        <span
                          className={`h-2 w-2 rounded-full ${getRiskDotStyle(segment.risk)}`}
                        />
                        <span className="font-mono text-xs font-bold text-slate-500">
                          {segment.id}
                        </span>
                      </div>
                      <Badge
                        variant="secondary"
                        className="border-none bg-slate-100 px-1.5 py-0 text-[10px] font-bold tracking-wide text-slate-600 uppercase"
                      >
                        {segment.bodyType}
                      </Badge>
                    </div>

                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-slate-800">
                        {segment.productName}
                      </p>
                      <span className="mt-0.5 block text-[11px] font-medium text-slate-400">
                        {segment.loadType}
                      </span>
                    </div>

                    <div className="min-w-0">
                      <p className="flex items-center gap-1.5 truncate text-sm font-bold text-slate-700">
                        {segment.origin.split(",")[0]}{" "}
                        <span className="text-xs text-slate-300">➔</span>{" "}
                        {segment.destination.split(",")[0]}
                        <span className="rounded border border-slate-100 bg-slate-50 px-1 font-mono text-xs font-medium text-slate-400">
                          {segment.distanceKm} km
                        </span>
                      </p>
                      <span
                        className={`mt-0.5 block text-[11px] ${getRiskTextStyle(segment.risk)}`}
                      >
                        ⏱️ {segment.pickupWindow}
                      </span>
                    </div>

                    <div className="flex items-center justify-end gap-3 text-xs font-bold whitespace-nowrap">
                      <div className="flex gap-1 rounded border border-slate-100 bg-slate-50 px-2 py-1 text-slate-500">
                        <span>{formatWeight(segment.weightKg)}</span>
                        <span className="text-slate-300">•</span>
                        <span>{formatVolume(segment.volumeM3)}</span>
                      </div>
                      <div className="min-w-[85px] text-right">
                        <span className="block text-[10px] font-semibold tracking-wider text-slate-400 uppercase">
                          Alvo
                        </span>
                        <span className="font-mono text-sm font-black text-slate-700">
                          {formatCurrency(segment.targetPrice)}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* MENUS FLUTUANTES DE AÇÃO */}
                  {!isSelectionMode && !isEditingThis && (
                    <div className="absolute top-1/2 right-3 flex -translate-y-1/2 items-center gap-1 bg-white pl-2 opacity-0 transition-opacity duration-200 group-hover:opacity-100">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => startEditing(segment)}
                        className="h-8 w-8 shrink-0 text-slate-400 transition-colors hover:bg-blue-50 hover:text-blue-600"
                      >
                        <Pencil size={14} />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setDeletingId(segment.id)}
                        className="h-8 w-8 shrink-0 text-slate-400 transition-colors hover:bg-rose-50 hover:text-rose-600"
                      >
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  )}
                </div>

                {/* FORMULÁRIO DE EDIÇÃO INLINE */}
                {isEditingThis && editForm && (
                  <div className="animate-in rounded-b-xl border-t border-slate-100 bg-slate-50/50 p-6 duration-200 fade-in slide-in-from-top-2">
                    <div className="mb-6 grid grid-cols-1 gap-x-6 gap-y-5 md:grid-cols-3">
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          Produto Principal
                        </label>
                        <Input
                          value={editForm.productName}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              productName: e.target.value,
                            })
                          }
                          className="h-9 bg-white text-xs focus:border-blue-500 focus:ring-blue-500"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          Local de Coleta (Origem)
                        </label>
                        <Input
                          value={editForm.origin}
                          onChange={(e) =>
                            setEditForm({ ...editForm, origin: e.target.value })
                          }
                          className="h-9 bg-white text-xs focus:border-blue-500 focus:ring-blue-500"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          Local de Entrega (Destino)
                        </label>
                        <Input
                          value={editForm.destination}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              destination: e.target.value,
                            })
                          }
                          className="h-9 bg-white text-xs focus:border-blue-500 focus:ring-blue-500"
                        />
                      </div>

                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          <Scale size={12} className="text-slate-400" /> Peso
                          Total
                        </label>
                        <div className="relative">
                          <Input
                            type="number"
                            value={editForm.weightKg}
                            onChange={(e) =>
                              setEditForm({
                                ...editForm,
                                weightKg: Number(e.target.value),
                              })
                            }
                            className="h-9 border-slate-200 bg-white pr-8 text-xs focus:border-blue-500 focus:ring-blue-500"
                          />
                          <span className="absolute top-2.5 right-2.5 text-[10px] font-semibold text-slate-400">
                            kg
                          </span>
                        </div>
                      </div>
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          <Box size={12} className="text-slate-400" /> Volume
                          Total
                        </label>
                        <div className="relative">
                          <Input
                            type="number"
                            value={editForm.volumeM3}
                            onChange={(e) =>
                              setEditForm({
                                ...editForm,
                                volumeM3: Number(e.target.value),
                              })
                            }
                            className="h-9 border-slate-200 bg-white pr-8 text-xs focus:border-blue-500 focus:ring-blue-500"
                          />
                          <span className="absolute top-2.5 right-2.5 text-[10px] font-semibold text-slate-400">
                            m³
                          </span>
                        </div>
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          Restrição de Equipamento
                        </label>
                        <Select
                          value={editForm.bodyType}
                          onValueChange={(v) =>
                            setEditForm({ ...editForm, bodyType: v })
                          }
                        >
                          <SelectTrigger className="h-9 border-slate-200 bg-white text-xs focus:border-blue-500 focus:ring-blue-500">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Carga Seca">
                              Carga Seca
                            </SelectItem>
                            <SelectItem value="Baú Sider">Baú Sider</SelectItem>
                            <SelectItem value="Frigorífico">
                              Frigorífico
                            </SelectItem>
                            <SelectItem value="Refrigerado">
                              Refrigerado
                            </SelectItem>
                            <SelectItem value="Carreta Prancha">
                              Carreta Prancha
                            </SelectItem>
                          </SelectContent>
                        </Select>
                      </div>

                      <div className="space-y-1.5 md:col-span-2">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          <CalendarClock size={12} className="text-amber-500" />{" "}
                          Prazos e Janelas (SLA)
                        </label>
                        <Input
                          value={editForm.pickupWindow}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              pickupWindow: e.target.value,
                            })
                          }
                          placeholder="Ex: Hoje até às 18:00h"
                          className="h-9 bg-white text-xs focus:border-blue-500 focus:ring-blue-500"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                          <DollarSign size={12} className="text-emerald-500" />{" "}
                          Orçamento Teto
                        </label>
                        <div className="relative">
                          <span className="absolute top-2.5 left-2.5 text-xs font-bold text-slate-400">
                            R$
                          </span>
                          <Input
                            type="number"
                            value={editForm.targetPrice}
                            onChange={(e) =>
                              setEditForm({
                                ...editForm,
                                targetPrice: Number(e.target.value),
                              })
                            }
                            className="h-9 border-slate-200 bg-white pl-8 font-mono text-xs font-semibold focus:border-blue-500 focus:ring-blue-500"
                          />
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center justify-between border-t border-slate-200 pt-4">
                      <p className="flex items-center gap-1.5 text-[10px] text-slate-500">
                        <AlertCircle size={12} /> As alterações refletirão
                        imediatamente na mesa de operações.
                      </p>
                      <div className="flex gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={cancelEditing}
                          className="h-8 bg-white text-xs font-semibold"
                        >
                          <X size={14} className="mr-1.5" /> Cancelar
                        </Button>
                        <Button
                          size="sm"
                          onClick={saveEdit}
                          className="h-8 bg-blue-600 text-xs font-bold text-white hover:bg-blue-700"
                        >
                          <Save size={14} className="mr-1.5" /> Salvar
                          Alterações
                        </Button>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </div>
    </AppShell>
  )
}
