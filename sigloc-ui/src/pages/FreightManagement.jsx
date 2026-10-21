import { useState } from "react"
import { Plus, Search, Pencil, Trash2, X, Save, AlertCircle, Scale, Box, Truck, MapPin, User, Settings2 } from "lucide-react"
import { Link } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

export const availableVehicles = [
  { id: "FRO-1042", plate: "ABC-1234", model: "Volvo FH 540", bodyType: "Frigorífico", driver: "Carlos Mendes", location: "Curitiba, PR", status: "Livre", weightKg: 25000, volumeM3: 90 },
  { id: "FRO-1043", plate: "XYZ-9876", model: "Scania R460", bodyType: "Baú Sider", driver: "Roberto Silva", location: "São Paulo, SP", status: "Em Trânsito", weightKg: 27000, volumeM3: 105 },
  { id: "FRO-1044", plate: "QWE-5544", model: "DAF XF 530", bodyType: "Refrigerado", driver: "Não Alocado", location: "Campinas, SP", status: "Manutenção", weightKg: 25000, volumeM3: 85 },
  { id: "FRO-1045", plate: "ASD-9988", model: "Mercedes Actros 2651", bodyType: "Carga Seca", driver: "João Pedro", location: "Ribeirão Preto, SP", status: "Livre", weightKg: 30000, volumeM3: 110 },
]

const formatWeight = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} kg`
const formatVolume = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} m³`

// Cores baseadas no status do veículo
const getStatusDotStyle = (status) => {
  if (status === "Livre") return "bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.4)]"
  if (status === "Em Trânsito") return "bg-blue-500 shadow-[0_0_8px_rgba(59,130,246,0.4)]"
  if (status === "Manutenção") return "bg-amber-500 shadow-[0_0_8px_rgba(245,158,11,0.4)]"
  return "bg-slate-300"
}

const getStatusTextStyle = (status) => {
  if (status === "Livre") return "text-emerald-600 font-bold"
  if (status === "Em Trânsito") return "text-blue-600 font-bold"
  if (status === "Manutenção") return "text-amber-600 font-bold"
  return "text-slate-400"
}

export default function FleetManagement() {
  const [searchTerm, setSearchTerm] = useState("")
  
  const [vehicles, setVehicles] = useState(availableVehicles)
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [deletingId, setDeletingId] = useState(null)

  const visibleVehicles = vehicles.filter((vehicle) => {
    const term = searchTerm.trim().toLowerCase()
    if (!term) return true
    return [vehicle.plate, vehicle.model, vehicle.bodyType, vehicle.driver, vehicle.location, vehicle.status].join(" ").toLowerCase().includes(term)
  })

  const startEditing = (vehicle) => {
    setEditingId(vehicle.id)
    setEditForm({ ...vehicle })
    setDeletingId(null)
  }
  
  const cancelEditing = () => {
    setEditingId(null)
    setEditForm(null)
  }

  const saveEdit = () => {
    setVehicles(prev => prev.map(v => v.id === editingId ? editForm : v))
    setEditingId(null)
    setEditForm(null)
  }

  const confirmDelete = (id) => {
    setVehicles(prev => prev.filter(v => v.id !== id))
    setDeletingId(null)
  }

  return (
    <AppShell title="Gestão de Frota">
      <div className="mx-auto max-w-7xl space-y-4">
        
        {/* BARRA DE TOPO */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">Veículos Cadastrados</h1>
          </div>
          
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-72">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} placeholder="Buscar por placa, modelo, status..." className="h-9 border-slate-200 bg-white pl-9 text-xs" />
            </div>

            <Button variant="outline" className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700">
              <Settings2 size={14} className="mr-1.5" /> Filtros
            </Button>

            <Button asChild className="h-9 bg-blue-600 text-xs font-semibold text-white hover:bg-blue-700">
              <Link to="/register-vehicle"><Plus size={14} className="mr-1.5" /> Novo Veículo</Link>
            </Button>
          </div>
        </div>

        {/* LISTAGEM DE ALTA DENSIDADE */}
        <div className="space-y-2">
          {visibleVehicles.map((vehicle) => {
            const isEditingThis = editingId === vehicle.id
            const isDeletingThis = deletingId === vehicle.id

            if (isDeletingThis) {
              return (
                <div key={vehicle.id} className="flex w-full items-center justify-between gap-4 rounded-xl border border-rose-200 bg-rose-50/50 p-3.5 animate-in fade-in">
                  <div className="flex items-center gap-3 text-rose-700 pl-2">
                    <AlertCircle size={16} />
                    <span className="text-sm font-bold">Excluir permanentemente o veículo {vehicle.plate}?</span>
                  </div>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" onClick={() => setDeletingId(null)} className="h-8 text-xs font-semibold bg-white border-slate-200 text-slate-600">Cancelar</Button>
                    <Button size="sm" onClick={() => confirmDelete(vehicle.id)} className="h-8 text-xs font-bold bg-rose-600 text-white hover:bg-rose-700">Sim, Excluir</Button>
                  </div>
                </div>
              )
            }

            return (
              <div
                key={vehicle.id}
                className={`group relative flex w-full flex-col rounded-xl border transition-all ${
                  isEditingThis 
                    ? "border-blue-300 bg-white" 
                    : "border-slate-200 bg-white"
                }`}
              >
                {/* LINHA PRINCIPAL VISÍVEL */}
                <div className="flex items-center w-full p-3.5 text-left">

                  <div className="grid min-w-0 flex-1 grid-cols-[120px_1.1fr_1.3fr_250px] items-center gap-6 pl-2">
                    {/* Col 1: Status, ID e Tipo */}
                    <div>
                      <div className="flex items-center gap-2 mb-1">
                        <span className={`h-2 w-2 rounded-full ${getStatusDotStyle(vehicle.status)}`} />
                        <span className="font-mono text-xs font-bold text-slate-500">{vehicle.id}</span>
                      </div>
                      <Badge variant="secondary" className="text-[10px] bg-slate-100 text-slate-600 font-bold uppercase tracking-wide border-none px-1.5 py-0">
                        {vehicle.bodyType}
                      </Badge>
                    </div>

                    {/* Col 2: Placa e Modelo */}
                    <div className="min-w-0">
                      <p className="truncate text-sm font-black text-slate-800 tracking-wide font-mono">{vehicle.plate}</p>
                      <span className="text-[11px] text-slate-400 font-medium flex items-center gap-1 mt-0.5">
                         <Truck size={12} className="text-slate-300"/> {vehicle.model}
                      </span>
                    </div>

                    {/* Col 3: Motorista e Localização/Status */}
                    <div className="min-w-0">
                      <p className="truncate text-sm font-bold text-slate-700 flex items-center gap-1.5">
                        <User size={14} className="text-slate-400" /> {vehicle.driver}
                      </p>
                      <span className={`text-[11px] flex items-center gap-1 mt-0.5 ${getStatusTextStyle(vehicle.status)}`}>
                        {vehicle.status} <span className="text-slate-300 mx-0.5">•</span> <MapPin size={10} /> {vehicle.location}
                      </span>
                    </div>

                    {/* Col 4: Capacidade (Peso/Vol) */}
                    <div className="flex items-center justify-end gap-3 text-xs font-bold whitespace-nowrap">
                      <div className="text-right mr-2">
                        <span className="text-[10px] text-slate-400 block font-semibold uppercase tracking-wider">Capacidade Total</span>
                      </div>
                      <div className="flex gap-1.5 text-slate-600 bg-slate-50 px-2 py-1.5 rounded border border-slate-100">
                        <span className="flex items-center gap-1"><Scale size={12} className="text-slate-400"/> {formatWeight(vehicle.weightKg)}</span>
                        <span className="text-slate-300">•</span>
                        <span className="flex items-center gap-1"><Box size={12} className="text-slate-400"/> {formatVolume(vehicle.volumeM3)}</span>
                      </div>
                    </div>
                  </div>

                  {/* AÇÕES: ESCONDIDAS POR PADRÃO */}
                  {!isEditingThis && (
                    <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1 opacity-0 group-hover:opacity-100 bg-white pl-2 transition-opacity duration-200">
                      <Button variant="ghost" size="icon" onClick={() => startEditing(vehicle)} className="h-8 w-8 text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0 transition-colors">
                        <Pencil size={14} />
                      </Button>
                      <Button variant="ghost" size="icon" onClick={() => setDeletingId(vehicle.id)} className="h-8 w-8 text-slate-400 hover:text-rose-600 hover:bg-rose-50 shrink-0 transition-colors">
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  )}
                </div>

                {/* ESTADO: FORMULÁRIO DE EDIÇÃO INLINE COMPLETO */}
                {isEditingThis && editForm && (
                  <div className="border-t border-slate-100 bg-slate-50/50 p-6 animate-in slide-in-from-top-2 fade-in duration-200 rounded-b-xl">
                    
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-x-6 gap-y-5 mb-6">
                      {/* LINHA 1: Identificação Básica */}
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Placa do Veículo</label>
                        <Input value={editForm.plate} onChange={(e) => setEditForm({...editForm, plate: e.target.value})} className="h-9 text-xs font-mono font-bold bg-white focus:border-blue-500 focus:ring-blue-500 uppercase" />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Marca / Modelo</label>
                        <Input value={editForm.model} onChange={(e) => setEditForm({...editForm, model: e.target.value})} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Motorista Atribuído</label>
                        <Input value={editForm.driver} onChange={(e) => setEditForm({...editForm, driver: e.target.value})} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>

                      {/* LINHA 2: Capacidade e Equipamento */}
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <Scale size={12} className="text-slate-400" /> Capacidade (Peso)
                        </label>
                        <div className="relative">
                          <Input type="number" value={editForm.weightKg} onChange={(e) => setEditForm({...editForm, weightKg: Number(e.target.value)})} className="h-9 border-slate-200 pr-8 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                          <span className="absolute right-2.5 top-2.5 text-[10px] font-semibold text-slate-400">kg</span>
                        </div>
                      </div>
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <Box size={12} className="text-slate-400" /> Cubagem (Volume)
                        </label>
                        <div className="relative">
                          <Input type="number" value={editForm.volumeM3} onChange={(e) => setEditForm({...editForm, volumeM3: Number(e.target.value)})} className="h-9 border-slate-200 pr-8 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                          <span className="absolute right-2.5 top-2.5 text-[10px] font-semibold text-slate-400">m³</span>
                        </div>
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Tipo de Carroceria</label>
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

                      {/* LINHA 3: Status e Localização */}
                      <div className="space-y-1.5 md:col-span-2">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <MapPin size={12} className="text-slate-400" /> Localização / Pátio Atual
                        </label>
                        <Input value={editForm.location} onChange={(e) => setEditForm({...editForm, location: e.target.value})} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Status Operacional</label>
                        <Select value={editForm.status} onValueChange={(v) => setEditForm({...editForm, status: v})}>
                          <SelectTrigger className="h-9 border-slate-200 text-xs bg-white focus:border-blue-500 focus:ring-blue-500">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Livre">Livre</SelectItem>
                            <SelectItem value="Em Trânsito">Em Trânsito</SelectItem>
                            <SelectItem value="Manutenção">Manutenção</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                    
                    <div className="flex items-center justify-between border-t border-slate-200 pt-4">
                      <p className="text-[10px] text-slate-500 flex items-center gap-1.5"><AlertCircle size={12}/> Os dados do veículo serão atualizados imediatamente no sistema.</p>
                      <div className="flex gap-2">
                        <Button variant="outline" size="sm" onClick={cancelEditing} className="h-8 text-xs font-semibold bg-white">
                          <X size={14} className="mr-1.5" /> Cancelar
                        </Button>
                        <Button size="sm" onClick={saveEdit} className="h-8 text-xs font-bold bg-blue-600 text-white hover:bg-blue-700">
                          <Save size={14} className="mr-1.5" /> Salvar Veículo
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