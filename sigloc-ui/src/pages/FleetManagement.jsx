import { useEffect, useState } from "react"
import { Plus, Search, Pencil, Trash2, X, Save, AlertCircle, Scale, Box, Truck, MapPin, User, Settings2, Loader2, ChevronDown } from "lucide-react"
import { Link } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useAsyncAction } from "@/hooks/use-async-action"
import { vehicleService } from "@/services/vehicle-service"
import { BODY_TYPE_OPTIONS, ON_TRIP_LABEL, REFRIGERATION_BY_VALUE, STATUS_OPTIONS } from "@/constants/vehicles"
import { ListFilters, filtersLabel } from "@/components/list-filters"
import { VEHICLE_LIMITS, mapVehicleFieldErrors, validateVehicle } from "@/lib/vehicle-validation"
import { shortId } from "@/lib/utils"
import VehicleForm from "@/pages/VehicleForm"
import { BodyTypeField, EquipmentChecks, RefrigerationField } from "@/components/vehicles/VehicleEquipmentFields"
import { normalizeVehicleEquipment } from "@/constants/cargo-compatibility"
import { CivCippField, CompositionField, DriverDocumentFields, RntrcFields } from "@/components/vehicles/VehicleDocumentFields"
import { BodyTypeIcon, ComplianceShield, EquipmentIcons, VehicleDetails } from "@/components/vehicles/fleet-row"

const formatWeight = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} kg`
const formatVolume = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} m³`


// A "Livre" vehicle already committed to an active trip is not free; the stored status is the
// carrier-managed value, so this only changes what the list shows.
const displayStatus = (vehicle) =>
  vehicle.onActiveTrip && vehicle.status === "Livre" ? ON_TRIP_LABEL : vehicle.status

// Cores baseadas no status do veículo
const getStatusDotStyle = (status) => {
  if (status === "Livre") return "bg-emerald-500"
  if (status === ON_TRIP_LABEL) return "bg-blue-500"
  if (status === "Manutenção") return "bg-amber-500"
  return "bg-slate-300"
}

// Mirrors the API limits so an invalid edit never reaches the server.
const toOptions = (labels) => labels.map((label) => ({ value: label, label }))
const FILTER_FIELDS = [
  { key: "status", label: "Filtrar por status", allLabel: "Todos os status", options: toOptions(["Livre", ON_TRIP_LABEL, "Manutenção"]) },
  { key: "bodyType", label: "Filtrar por carroceria", allLabel: "Todas as carrocerias", options: toOptions(BODY_TYPE_OPTIONS) },
  { key: "refrigeration", label: "Filtrar por refrigeração", allLabel: "Qualquer refrigeração", options: Object.entries(REFRIGERATION_BY_VALUE).map(([value, label]) => ({ value, label })) },
]
const NO_FILTERS = { status: "", bodyType: "", refrigeration: "" }

const FieldError = ({ message }) =>
  message ? <p className="text-[10px] font-semibold text-rose-600">{message}</p> : null

// Pill (chip) colours for the dedicated status column.
const getStatusChipStyle = (status) => {
  if (status === "Livre") return "border-emerald-200 bg-emerald-50 text-emerald-700"
  if (status === ON_TRIP_LABEL) return "border-blue-200 bg-blue-50 text-blue-700"
  if (status === "Manutenção") return "border-amber-200 bg-amber-50 text-amber-700"
  return "border-slate-200 bg-slate-50 text-slate-500"
}

export default function FleetManagement() {
  const [searchTerm, setSearchTerm] = useState("")
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [filters, setFilters] = useState(NO_FILTERS)

  const [vehicles, setVehicles] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState(null)
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [expandedId, setExpandedId] = useState(null)
  const [editErrors, setEditErrors] = useState({})
  const [deletingId, setDeletingId] = useState(null)

  const saveAction = useAsyncAction(vehicleService.updateVehicle)
  const deleteAction = useAsyncAction(vehicleService.deleteVehicle)

  useEffect(() => {
    const controller = new AbortController()

    async function loadVehicles() {
      setIsLoading(true)
      setLoadError(null)
      try {
        const data = await vehicleService.listVehicles({ signal: controller.signal })
        setVehicles(data)
      } catch (error) {
        if (error?.name === "AbortError") return
        setLoadError(error?.message || "Não foi possível carregar os veículos.")
      } finally {
        if (!controller.signal.aborted) setIsLoading(false)
      }
    }

    loadVehicles()
    return () => controller.abort()
  }, [])

  const visibleVehicles = vehicles.filter((vehicle) => {
    if (filters.status && displayStatus(vehicle) !== filters.status) return false
    if (filters.bodyType && vehicle.bodyType !== filters.bodyType) return false
    if (filters.refrigeration && String(vehicle.refrigerationLevel ?? 0) !== filters.refrigeration) return false
    const term = searchTerm.trim().toLowerCase()
    if (!term) return true
    return [vehicle.plate, vehicle.model, vehicle.bodyType, vehicle.driver, vehicle.location, displayStatus(vehicle)].join(" ").toLowerCase().includes(term)
  })

  const startEditing = (vehicle) => {
    setEditingId(vehicle.id)
    // A record still holding the retired manual "in transit" status is edited as Livre.
    // Vehicles saved before refrigeration was tied to the body type open already corrected.
    setEditForm(normalizeVehicleEquipment({ ...vehicle, status: vehicle.status === ON_TRIP_LABEL ? "Livre" : vehicle.status }))
    setEditErrors({})
    setDeletingId(null)
    saveAction.reset()
  }

  const cancelEditing = () => {
    setEditingId(null)
    setEditForm(null)
    setEditErrors({})
    saveAction.reset()
  }

  const saveEdit = async () => {
    const errors = validateVehicle(editForm)
    setEditErrors(errors)
    if (Object.keys(errors).length > 0) return

    const result = await saveAction.run(editingId, editForm)
    if (!result.ok) return

    setVehicles(prev => prev.map(v => v.id === editingId ? {
      ...editForm,
      driverPhone: editForm.driverPhone?.trim() ?? v.driverPhone,
      traccarDeviceId: String(editForm.traccarDeviceId ?? "").trim() ? Number(editForm.traccarDeviceId) : v.traccarDeviceId,
    } : v))
    setEditingId(null)
    setEditForm(null)
    // The document warnings are worked out by the API, so refresh them from it.
    vehicleService.listVehicles().then(setVehicles).catch(() => {})
  }

  const confirmDelete = async (id) => {
    const result = await deleteAction.run(id)
    if (!result.ok) return

    setVehicles(prev => prev.filter(v => v.id !== id))
    setDeletingId(null)
  }

  return (
    <AppShell title="Gestão de Frota">
      <div className="space-y-4">
        
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

            <Button variant="outline" aria-expanded={filtersOpen} onClick={() => setFiltersOpen((open) => !open)} className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700">
              <Settings2 size={14} className="mr-1.5" /> {filtersLabel(filters)}
            </Button>

            <Button asChild className="h-9 bg-blue-600 text-xs font-semibold text-white hover:bg-blue-700">
              <Link to="/register-vehicle"><Plus size={14} className="mr-1.5" /> Novo Veículo</Link>
            </Button>
          </div>
        </div>

        {filtersOpen && <ListFilters filters={filters} fields={FILTER_FIELDS} onChange={setFilters} />}

        {/* ESTADO: CARREGANDO */}
        {isLoading && (
          <div className="flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white py-16 text-sm font-medium text-slate-500">
            <Loader2 size={16} className="animate-spin text-blue-600" /> Carregando veículos...
          </div>
        )}

        {/* ESTADO: ERRO DE CARREGAMENTO */}
        {!isLoading && loadError && (
          <div className="flex items-center gap-3 rounded-xl border border-rose-200 bg-rose-50/50 p-4 text-sm font-semibold text-rose-700">
            <AlertCircle size={16} /> {loadError}
          </div>
        )}

        {/* ESTADO: LISTA VAZIA */}
        {!isLoading && !loadError && visibleVehicles.length === 0 && (
          <div className="flex flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-slate-200 bg-white py-16 text-center">
            <Truck size={24} className="text-slate-300" />
            <p className="text-sm font-semibold text-slate-600">
              {vehicles.length === 0 ? "Nenhum veículo cadastrado." : "Nenhum veículo encontrado para a busca ou os filtros."}
            </p>
          </div>
        )}

        {/* LISTAGEM DE ALTA DENSIDADE */}
        {!isLoading && !loadError && visibleVehicles.length > 0 && (
        <div className="space-y-2">
          {visibleVehicles.map((vehicle) => {
            const isEditingThis = editingId === vehicle.id
            const isExpandedThis = expandedId === vehicle.id && !isEditingThis
            const isDeletingThis = deletingId === vehicle.id

            if (isDeletingThis) {
              return (
                <div key={vehicle.id} className="flex w-full items-center justify-between gap-4 rounded-xl border border-rose-200 bg-rose-50/50 p-3.5 animate-in fade-in">
                  <div className="flex items-center gap-3 text-rose-700 pl-2">
                    <AlertCircle size={16} />
                    <span className="text-sm font-bold">
                      {deleteAction.error ? deleteAction.error.message : `Excluir permanentemente o veículo ${vehicle.plate}?`}
                    </span>
                  </div>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={deleteAction.pending} onClick={() => { setDeletingId(null); deleteAction.reset() }} className="h-8 text-xs font-semibold bg-white border-slate-200 text-slate-600">Cancelar</Button>
                    <Button size="sm" disabled={deleteAction.pending} onClick={() => confirmDelete(vehicle.id)} className="h-8 text-xs font-bold bg-rose-600 text-white hover:bg-rose-700">
                      {deleteAction.pending ? <><Loader2 size={14} className="mr-1.5 animate-spin" /> Excluindo...</> : "Sim, Excluir"}
                    </Button>
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
                {/* Relative so the hover actions stay on this line when the details open below it. */}
                <div className="relative flex w-full items-center p-3.5 text-left">
                  <div className="grid min-w-0 flex-1 grid-cols-[36px_minmax(0,1.1fr)_minmax(0,1.3fr)_112px_96px_104px_28px] items-center gap-4 pl-1 pr-28">
                    <BodyTypeIcon bodyType={vehicle.bodyType} />

                    {/* Placa e modelo */}
                    <div className="min-w-0">
                      <p className="truncate font-mono text-sm font-black tracking-wide text-slate-800">{vehicle.plate}</p>
                      <p className="truncate text-[11px] font-medium text-slate-400">{vehicle.model}</p>
                    </div>

                    {/* Motorista e localização */}
                    <div className="min-w-0">
                      <p className="flex items-center gap-1.5 truncate text-sm font-semibold text-slate-700">
                        <User size={13} className="shrink-0 text-slate-400" />
                        <span className="truncate">{vehicle.driver || "Sem motorista"}</span>
                      </p>
                      <p className="flex items-center gap-1.5 truncate text-[11px] text-slate-400">
                        <MapPin size={12} className="shrink-0" />
                        <span className="truncate">{vehicle.location || "—"}</span>
                      </p>
                    </div>

                    <span className={`inline-flex w-fit items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-bold ${getStatusChipStyle(displayStatus(vehicle))}`}>
                      <span className={`h-1.5 w-1.5 rounded-full ${getStatusDotStyle(displayStatus(vehicle))}`} />
                      {displayStatus(vehicle)}
                    </span>

                    <EquipmentIcons vehicle={vehicle} />

                    {/* Capacidade */}
                    <div className="font-mono text-xs leading-tight">
                      <p className="font-bold text-slate-700">{formatWeight(vehicle.weightKg)}</p>
                      <p className="text-slate-400">{formatVolume(vehicle.volumeM3)}</p>
                    </div>

                    <ComplianceShield issues={vehicle.compliance} />
                  </div>

                  {/* AÇÕES: ESCONDIDAS POR PADRÃO */}
                  {!isEditingThis && (
                    <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1 opacity-0 group-hover:opacity-100 bg-white pl-2 transition-opacity duration-200">
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={`${isExpandedThis ? "Recolher" : "Ver detalhes do veículo"} ${vehicle.plate}`}
                        aria-expanded={isExpandedThis}
                        title={isExpandedThis ? "Recolher" : "Ver detalhes"}
                        onClick={() => setExpandedId(isExpandedThis ? null : vehicle.id)}
                        className="h-8 w-8 text-slate-400 hover:text-slate-700 hover:bg-slate-100 shrink-0 transition-colors"
                      >
                        <ChevronDown size={15} className={`transition-transform ${isExpandedThis ? "rotate-180" : ""}`} />
                      </Button>
                      <Button variant="ghost" size="icon" aria-label={`Editar veículo ${vehicle.plate}`} title="Editar" onClick={() => startEditing(vehicle)} className="h-8 w-8 text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0 transition-colors">
                        <Pencil size={14} />
                      </Button>
                      <Button variant="ghost" size="icon" aria-label={`Excluir veículo ${vehicle.plate}`} title="Excluir" onClick={() => { setDeletingId(vehicle.id); deleteAction.reset() }} className="h-8 w-8 text-slate-400 hover:text-rose-600 hover:bg-rose-50 shrink-0 transition-colors">
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  )}
                </div>

                {isExpandedThis && <VehicleDetails vehicle={vehicle} />}

                {/* ESTADO: FORMULÁRIO DE EDIÇÃO INLINE COMPLETO */}
                {isEditingThis && editForm && (
                  <div className="border-t border-slate-100 bg-slate-50/50 p-6 animate-in slide-in-from-top-2 fade-in duration-200 rounded-b-xl">
                    
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-x-6 gap-y-5 mb-6">
                      {/* LINHA 1: Identificação Básica */}
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Placa do Veículo</label>
                        <Input value={editForm.plate} readOnly disabled title="A placa não pode ser alterada" className="h-9 text-xs font-mono font-bold bg-slate-100 text-slate-500 cursor-not-allowed uppercase" />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Marca / Modelo</label>
                        <Input value={editForm.model} maxLength={VEHICLE_LIMITS.model} onChange={(e) => setEditForm({...editForm, model: e.target.value})} aria-invalid={Boolean(editErrors.model)} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                        <FieldError message={editErrors.model ?? saveAction.fieldErrors.model} />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Motorista Atribuído</label>
                        <Input value={editForm.driver} onChange={(e) => setEditForm({...editForm, driver: e.target.value})} className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                      </div>

                      <VehicleForm value={editForm} onChange={setEditForm} disabled={saveAction.pending} idPrefix={`vehicle-${vehicle.id}`} preserveTrackerOnBlank errors={{ ...mapVehicleFieldErrors(saveAction.fieldErrors), ...editErrors }} />

                      {/* LINHA 2: Capacidade e Equipamento */}
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <Scale size={12} className="text-slate-400" /> Capacidade (Peso)
                        </label>
                        <div className="relative">
                          <Input type="number" step="any" value={editForm.weightKg} onChange={(e) => setEditForm({...editForm, weightKg: Number(e.target.value)})} aria-invalid={Boolean(editErrors.weightKg || saveAction.fieldErrors.capacityWeight)} className="h-9 border-slate-200 pr-8 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                          <span className="absolute right-2.5 top-2.5 text-[10px] font-semibold text-slate-400">kg</span>
                        </div>
                        <FieldError message={editErrors.weightKg ?? saveAction.fieldErrors.capacityWeight} />
                      </div>
                      <div className="space-y-1.5">
                        <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <Box size={12} className="text-slate-400" /> Cubagem (Volume)
                        </label>
                        <div className="relative">
                          <Input type="number" step="any" value={editForm.volumeM3} onChange={(e) => setEditForm({...editForm, volumeM3: Number(e.target.value)})} aria-invalid={Boolean(editErrors.volumeM3 || saveAction.fieldErrors.capacityVolume)} className="h-9 border-slate-200 pr-8 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                          <span className="absolute right-2.5 top-2.5 text-[10px] font-semibold text-slate-400">m³</span>
                        </div>
                        <FieldError message={editErrors.volumeM3 ?? saveAction.fieldErrors.capacityVolume} />
                      </div>
                      <div className="space-y-1.5">
                        <label htmlFor={`vehicle-${vehicle.id}-axles`} className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Quantidade de Eixos</label>
                        <Input id={`vehicle-${vehicle.id}-axles`} type="number" step="1" value={editForm.axleCount ?? ""} onChange={(e) => setEditForm({...editForm, axleCount: e.target.value})} aria-invalid={Boolean(editErrors.axleCount || saveAction.fieldErrors.axleCount)} className="h-9 border-slate-200 text-xs bg-white focus:border-blue-500 focus:ring-blue-500" />
                        <FieldError message={editErrors.axleCount ?? saveAction.fieldErrors.axleCount} />
                      </div>
                      <BodyTypeField
                        value={editForm}
                        onChange={setEditForm}
                        errors={{ ...mapVehicleFieldErrors(saveAction.fieldErrors), ...editErrors }}
                        disabled={saveAction.pending}
                        className="space-y-1.5"
                        labelClassName="text-[10px] font-bold uppercase tracking-wider text-slate-500"
                        triggerClassName="h-9 border-slate-200 text-xs bg-white focus:border-blue-500 focus:ring-blue-500"
                      />
                      <CompositionField
                        value={editForm}
                        onChange={setEditForm}
                        errors={mapVehicleFieldErrors(saveAction.fieldErrors)}
                        disabled={saveAction.pending}
                        idPrefix={`vehicle-${vehicle.id}`}
                        labelClassName="block text-[10px] font-bold uppercase tracking-wider text-slate-500"
                        selectClassName="h-9 text-xs"
                      />
                      <RefrigerationField
                        value={editForm}
                        onChange={setEditForm}
                        errors={mapVehicleFieldErrors(saveAction.fieldErrors)}
                        disabled={saveAction.pending}
                        idPrefix={`vehicle-${vehicle.id}`}
                        className="space-y-1.5"
                        labelClassName="block text-[10px] font-bold uppercase tracking-wider text-slate-500"
                        selectClassName="h-9 text-xs"
                      />

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
                            {STATUS_OPTIONS.map((option) => (
                              <SelectItem key={option} value={option}>{option}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                      <EquipmentChecks
                        value={editForm}
                        onChange={setEditForm}
                        errors={mapVehicleFieldErrors(saveAction.fieldErrors)}
                        disabled={saveAction.pending}
                        idPrefix={`vehicle-${vehicle.id}`}
                        className="border-t border-slate-100 pt-4 md:col-span-3"
                      />
                      <CivCippField
                        value={editForm}
                        onChange={setEditForm}
                        errors={mapVehicleFieldErrors(saveAction.fieldErrors)}
                        disabled={saveAction.pending}
                        idPrefix={`vehicle-${vehicle.id}`}
                        labelClassName="block text-[10px] font-bold uppercase tracking-wider text-slate-500"
                        inputClassName="h-9 text-xs bg-white"
                      />

                      {/* Documents: missing ones only warn (see the badge), so nothing here is required on edit. */}
                      <div className="grid gap-6 border-t border-slate-100 pt-4 md:col-span-3 md:grid-cols-2">
                        <div className="space-y-2">
                          <p className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Registro ANTT (RNTRC)</p>
                          <RntrcFields
                            value={editForm}
                            onChange={setEditForm}
                            errors={mapVehicleFieldErrors(saveAction.fieldErrors)}
                            disabled={saveAction.pending}
                            idPrefix={`vehicle-${vehicle.id}`}
                            labelClassName="block text-[10px] font-bold uppercase tracking-wider text-slate-500"
                            inputClassName="h-9 text-xs bg-white"
                            selectClassName="h-9 text-xs"
                          />
                        </div>
                        <div className="space-y-2">
                          <p className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Documentos do motorista</p>
                          <DriverDocumentFields
                            value={editForm}
                            onChange={setEditForm}
                            errors={mapVehicleFieldErrors(saveAction.fieldErrors)}
                            disabled={saveAction.pending}
                            idPrefix={`vehicle-${vehicle.id}`}
                            labelClassName="block text-[10px] font-bold uppercase tracking-wider text-slate-500"
                            inputClassName="h-9 text-xs bg-white"
                            selectClassName="h-9 text-xs"
                          />
                        </div>
                      </div>
                    </div>
                    
                    <div className="flex items-center justify-between border-t border-slate-200 pt-4">
                      {saveAction.error ? (
                        <p className="text-[10px] font-semibold text-rose-600 flex items-center gap-1.5"><AlertCircle size={12}/> {saveAction.error.message}</p>
                      ) : (
                        <p className="text-[10px] text-slate-500 flex items-center gap-1.5"><AlertCircle size={12}/> Os dados do veículo serão atualizados imediatamente no sistema.</p>
                      )}
                      <div className="flex gap-2">
                        <Button variant="outline" size="sm" disabled={saveAction.pending} onClick={cancelEditing} className="h-8 text-xs font-semibold bg-white">
                          <X size={14} className="mr-1.5" /> Cancelar
                        </Button>
                        <Button size="sm" disabled={saveAction.pending} onClick={saveEdit} className="h-8 text-xs font-bold bg-blue-600 text-white hover:bg-blue-700">
                          {saveAction.pending ? <><Loader2 size={14} className="mr-1.5 animate-spin" /> Salvando...</> : <><Save size={14} className="mr-1.5" /> Salvar Veículo</>}
                        </Button>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
        )}

      </div>
    </AppShell>
  )
}