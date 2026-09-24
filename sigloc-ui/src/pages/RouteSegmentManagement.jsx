import { useState } from "react"
import { Plus, Search, Layers, ArrowRight, Pencil, Trash2, X, Save, AlertCircle, Scale, Box, CalendarClock, DollarSign } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { RISK } from "@/constants/risk"

// RESTAURADO: export const para não quebrar o CreateRouteWorkspace
export const availableRouteSegments = [
  { id: "TRC-1042", productName: "Peito de Frango Congelado", bodyType: "Frigorífico", loadType: "Paletizado", origin: "Curitiba, PR", destination: "São Paulo, SP", risk: RISK.CRITIC, pickupWindow: "Hoje até às 18:00h", weightKg: 12000, volumeM3: 45, distanceKm: 408, targetPrice: 4200 },
  { id: "TRC-1043", productName: "Laticínios Pasteurizados", bodyType: "Refrigerado", loadType: "Paletizado", origin: "São Paulo, SP", destination: "Campinas, SP", risk: RISK.WARNING, pickupWindow: "Amanhã até às 12:00h", weightKg: 9000, volumeM3: 28, distanceKm: 96, targetPrice: 1100 },
  { id: "TRC-1044", productName: "Eletrônicos de Alto Valor", bodyType: "Baú Sider", loadType: "Caixas Master", origin: "Campinas, SP", destination: "Ribeirão Preto, SP", risk: RISK.WARNING, pickupWindow: "07/06 às 08:00h", weightKg: 4800, volumeM3: 22, distanceKm: 223, targetPrice: 2800 },
  { id: "TRC-1045", productName: "Vacinas Influenza", bodyType: "Frigorífico", loadType: "Isotérmico", origin: "Ribeirão Preto, SP", destination: "Uberlândia, MG", risk: RISK.CRITIC, pickupWindow: "Hoje urgente até às 16:30h", weightKg: 3200, volumeM3: 18, distanceKm: 166, targetPrice: 3100 },
]

const formatWeight = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} kg`
const formatVolume = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} m³`
const formatCurrency = (value) => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value)

const getRiskDotStyle = (risk) => {
  if (risk === RISK.CRITIC) return "bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.4)]"
  if (risk === RISK.WARNING) return "bg-amber-500 shadow-[0_0_8px_rgba(245,158,11,0.4)]"
  return "bg-slate-300"
}

const getRiskTextStyle = (risk) => {
  if (risk === RISK.CRITIC) return "text-rose-600 font-bold"
  if (risk === RISK.WARNING) return "text-amber-600 font-bold"
  return "text-slate-400"
}

export default function RouteSegmentManagement() {
  const navigate = useNavigate()
  const [searchTerm, setSearchTerm] = useState("")
  const [isSelectionMode, setIsSelectionMode] = useState(false)
  const [selectedSegmentIds, setSelectedSegmentIds] = useState([])
  
  const [segments, setSegments] = useState(availableRouteSegments)
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [deletingId, setDeletingId] = useState(null)

  const visibleSegments = segments.filter((segment) => {
    const term = searchTerm.trim().toLowerCase()
    if (!term) return true
    return [segment.id, segment.productName, segment.bodyType, segment.origin, segment.destination].join(" ").toLowerCase().includes(term)
  })

  const handleToggleSegment = (segmentId) => {
    setSelectedSegmentIds((prev) => prev.includes(segmentId) ? prev.filter((id) => id !== segmentId) : [...prev, segmentId])
  }
  const handleCancelSelection = () => {
    setIsSelectionMode(false)
    setSelectedSegmentIds([])
  }
  const handleProceedToWorkspace = () => navigate("/offer-freight", { state: { selectedIds: selectedSegmentIds } })

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
    setSegments(prev => prev.map(s => s.id === editingId ? editForm : s))
    setEditingId(null)
    setEditForm(null)
  }

  const confirmDelete = (id) => {
    setSegments(prev => prev.filter(s => s.id !== id))
    setDeletingId(null)
    setSelectedSegmentIds(prev => prev.filter(selectedId => selectedId !== id))
  }

  return (
    <AppShell title="Gestão de Trechos">
      <div className="mx-auto max-w-7xl space-y-4">
        
        {/* BARRA DE TOPO */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">Trechos Disponíveis</h1>
          </div>
          
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-72">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} placeholder="Buscar por trecho, produto ou cidade..." className="h-9 border-slate-200 bg-white pl-9 text-xs" />
            </div>

            <Button variant={isSelectionMode ? "secondary" : "outline"} className={isSelectionMode ? "h-9 border-blue-200 bg-blue-50 text-xs font-semibold text-blue-700 hover:bg-blue-100" : "h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700"} onClick={() => isSelectionMode ? handleCancelSelection() : setIsSelectionMode(true)}>
              <Layers size={14} className="mr-1.5" /> {isSelectionMode ? "Cancelar" : "Criar Rota"}
            </Button>

            <Button asChild className="h-9 bg-blue-600 text-xs font-semibold text-white hover:bg-blue-700">
              <Link to="/create-route-segment"><Plus size={14} className="mr-1.5" /> Novo Trecho</Link>
            </Button>
          </div>
        </div>

        {/* BANNER CONTEXTUAL */}
        {isSelectionMode && selectedSegmentIds.length > 0 && (
          <div className="animate-in fade-in flex items-center justify-between rounded-xl border border-blue-100 bg-blue-50 p-3 px-4 text-sm duration-200">
            <div className="flex items-center gap-2.5 text-blue-900">
              <span className="flex h-5 w-5 items-center justify-center rounded-md bg-blue-600 text-[10px] font-black tracking-wider text-white">{selectedSegmentIds.length}</span>
              <span className="text-xs font-medium text-slate-700">{selectedSegmentIds.length === 1 ? "Trecho selecionado e pronto para roteirização." : "Trechos selecionados e prontos para roteirização conjunta."}</span>
            </div>
            <button onClick={handleProceedToWorkspace} className="flex items-center gap-1.5 pl-4 text-xs font-bold uppercase tracking-wider text-blue-700 hover:text-blue-800">
              Avançar para Criar Leilão <ArrowRight size={14} className="animate-pulse" />
            </button>
          </div>
        )}

        {/* LISTAGEM DE ALTA DENSIDADE */}
        <div className="space-y-2">
          {visibleSegments.map((segment) => {
            const isChecked = selectedSegmentIds.includes(segment.id)
            const isEditingThis = editingId === segment.id
            const isDeletingThis = deletingId === segment.id

            if (isDeletingThis) {
              return (
                <div key={segment.id} className="flex w-full items-center justify-between gap-4 rounded-xl border border-rose-200 bg-rose-50/50 p-3.5 animate-in fade-in">
                  <div className="flex items-center gap-3 text-rose-700 pl-2">
                    <AlertCircle size={16} />
                    <span className="text-sm font-bold">Excluir permanentemente o trecho {segment.id}?</span>
                  </div>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" onClick={() => setDeletingId(null)} className="h-8 text-xs font-semibold bg-white border-slate-200 text-slate-600">Cancelar</Button>
                    <Button size="sm" onClick={() => confirmDelete(segment.id)} className="h-8 text-xs font-bold bg-rose-600 text-white hover:bg-rose-700">Sim, Excluir</Button>
                  </div>
                </div>
              )
            }

            return (
              // Substitua o className da div principal por este:

              <div
                key={segment.id}
                className={`group relative flex w-full flex-col rounded-xl border transition-all ${
                  isChecked 
                    ? "border-blue-300 bg-blue-50/40 ring-1 ring-blue-200" 
                    : isEditingThis 
                      ? "border-blue-300 bg-white" // Destaque Azul quando em edição
                      : "border-slate-200 bg-white"
                }`}
              >
                {/* LINHA PRINCIPAL VISÍVEL */}

                <div className="flex items-center w-full p-3.5 text-left">
                  {isSelectionMode && !isEditingThis && (
                    <div className="flex items-center justify-center pl-1 mr-4">
                      <input type="checkbox" checked={isChecked} onChange={() => handleToggleSegment(segment.id)} className="h-4 w-4 cursor-pointer rounded border-slate-300 accent-blue-600" />
                    </div>
                  )}

                  {/* AJUSTE: Aumentei um pouco o espaço da última coluna (de 220px para 250px) para evitar quebra */}
                  <div className="grid min-w-0 flex-1 grid-cols-[110px_1.1fr_1.3fr_250px] items-center gap-6">
                    <div>
                      <div className="flex items-center gap-2 mb-1">
                        <span className={`h-2 w-2 rounded-full ${getRiskDotStyle(segment.risk)}`} />
                        <span className="font-mono text-xs font-bold text-slate-500">{segment.id}</span>
                      </div>
                      <Badge variant="secondary" className="text-[10px] bg-slate-100 text-slate-600 font-bold uppercase tracking-wide border-none px-1.5 py-0">{segment.bodyType}</Badge>
                    </div>

                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-slate-800">{segment.productName}</p>
                      <span className="text-[11px] text-slate-400 font-medium block mt-0.5">{segment.loadType}</span>
                    </div>

                    <div className="min-w-0">
                      <p className="truncate text-sm font-bold text-slate-700 flex items-center gap-1.5">
                        {segment.origin.split(",")[0]} <span className="text-slate-300 text-xs">➔</span> {segment.destination.split(",")[0]}
                        <span className="text-xs font-mono font-medium text-slate-400 bg-slate-50 px-1 rounded border border-slate-100">{segment.distanceKm} km</span>
                      </p>
                      <span className={`text-[11px] block mt-0.5 ${getRiskTextStyle(segment.risk)}`}>⏱️ {segment.pickupWindow}</span>
                    </div>

                    {/* AJUSTE: whitespace-nowrap aqui impede a quebra de linha */}
                    <div className="flex items-center justify-end gap-3 text-xs font-bold whitespace-nowrap">
                      <div className="flex gap-1 text-slate-500 bg-slate-50 px-2 py-1 rounded border border-slate-100">
                        <span>{formatWeight(segment.weightKg)}</span><span className="text-slate-300">•</span><span>{formatVolume(segment.volumeM3)}</span>
                      </div>
                      <div className="text-right min-w-[85px]">
                        <span className="text-[10px] text-slate-400 block font-semibold uppercase tracking-wider">Alvo</span>
                        <span className="text-sm font-black text-slate-700 font-mono">{formatCurrency(segment.targetPrice)}</span>
                      </div>
                    </div>
                  </div>

                  {/* AÇÕES: ESCONDIDAS POR PADRÃO (Opacidade 0) */}
                  {!isSelectionMode && !isEditingThis && (
                    <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1 opacity-0 group-hover:opacity-100 bg-white pl-2 transition-opacity duration-200">
                      <Button variant="ghost" size="icon" onClick={() => startEditing(segment)} className="h-8 w-8 text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0 transition-colors">
                        <Pencil size={14} />
                      </Button>
                      <Button variant="ghost" size="icon" onClick={() => setDeletingId(segment.id)} className="h-8 w-8 text-slate-400 hover:text-rose-600 hover:bg-rose-50 shrink-0 transition-colors">
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  )}
                </div>

                {/* ESTADO: FORMULÁRIO DE EDIÇÃO INLINE COMPLETO */}
                {isEditingThis && editForm && (
                  <div className="border-t border-slate-100 bg-slate-50/50 p-6 animate-in slide-in-from-top-2 fade-in duration-200 rounded-b-xl">
                    
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-x-6 gap-y-5 mb-6">
                      {/* LINHA 1: Especificações Base */}
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Produto Principal</label>
                        <Input value={editForm.productName} onChange={(e) => setEditForm({...editForm, productName: e.target.value})} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Local de Coleta (Origem)</label>
                        <Input value={editForm.origin} onChange={(e) => setEditForm({...editForm, origin: e.target.value})} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Local de Entrega (Destino)</label>
                        <Input value={editForm.destination} onChange={(e) => setEditForm({...editForm, destination: e.target.value})} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>

                      {/* LINHA 2: Cubagem e Equipamento */}
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <Scale size={12} className="text-slate-400" /> Peso Total
                        </label>
                        <div className="relative">
                          <Input type="number" value={editForm.weightKg} onChange={(e) => setEditForm({...editForm, weightKg: Number(e.target.value)})} className="h-9 border-slate-200 pr-8 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                          <span className="absolute right-2.5 top-2.5 text-[10px] font-semibold text-slate-400">kg</span>
                        </div>
                      </div>
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <Box size={12} className="text-slate-400" /> Volume Total
                        </label>
                        <div className="relative">
                          <Input type="number" value={editForm.volumeM3} onChange={(e) => setEditForm({...editForm, volumeM3: Number(e.target.value)})} className="h-9 border-slate-200 pr-8 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                          <span className="absolute right-2.5 top-2.5 text-[10px] font-semibold text-slate-400">m³</span>
                        </div>
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Restrição de Equipamento</label>
                        <Select value={editForm.bodyType} onValueChange={(v) => setEditForm({...editForm, bodyType: v})}>
                          <SelectTrigger className="h-9 border-slate-200 text-xs bg-white focus:border-blue-500 focus:ring-blue-500">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Carga Seca">Carga Seca</SelectItem>
                            <SelectItem value="Baú Sider">Baú Sider</SelectItem>
                            <SelectItem value="Frigorífico">Frigorífico</SelectItem>
                            <SelectItem value="Refrigerado">Refrigerado</SelectItem>
                            <SelectItem value="Carreta Prancha">Carreta Prancha</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>

                      {/* LINHA 3: SLA e Custo */}
                      <div className="space-y-1.5 md:col-span-2">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <CalendarClock size={12} className="text-amber-500" /> Prazos e Janelas (SLA)
                        </label>
                        <Input value={editForm.pickupWindow} onChange={(e) => setEditForm({...editForm, pickupWindow: e.target.value})} placeholder="Ex: Hoje até às 18:00h" className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <DollarSign size={12} className="text-emerald-500" /> Orçamento Teto
                        </label>
                        <div className="relative">
                          <span className="absolute left-2.5 top-2.5 text-xs font-bold text-slate-400">R$</span>
                          <Input type="number" value={editForm.targetPrice} onChange={(e) => setEditForm({...editForm, targetPrice: Number(e.target.value)})} className="h-9 border-slate-200 pl-8 text-xs font-mono font-semibold bg-white focus:border-blue-500 focus:ring-blue-500" />
                        </div>
                      </div>
                    </div>
                    
                    <div className="flex items-center justify-between border-t border-slate-200 pt-4">
                      <p className="text-[10px] text-slate-500 flex items-center gap-1.5"><AlertCircle size={12}/> As alterações refletirão imediatamente na mesa de operações.</p>
                      <div className="flex gap-2">
                        <Button variant="outline" size="sm" onClick={cancelEditing} className="h-8 text-xs font-semibold bg-white">
                          <X size={14} className="mr-1.5" /> Cancelar
                        </Button>
                        <Button size="sm" onClick={saveEdit} className="h-8 text-xs font-bold bg-blue-600 text-white hover:bg-blue-700">
                          <Save size={14} className="mr-1.5" /> Salvar Alterações
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