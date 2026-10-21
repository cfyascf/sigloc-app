import { useCallback, useEffect, useMemo, useState } from "react"
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
  Box,
  CalendarClock,
  DollarSign,
  Loader2,
} from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { FormAlert } from "@/components/auth/FormAlert"
import { useAsyncAction } from "@/hooks/use-async-action"
import {
  listRouteSegments,
  updateRouteSegment,
  deleteRouteSegment,
  toDateTimeLocal,
} from "@/services/route-segment-service"
import { previewRoute } from "@/services/route-service"
import {
  isSegmentEditable,
  segmentStatusLabel,
  SEGMENT_STATUS,
  mapRouteSegmentFieldErrors,
} from "@/constants/route-segments"

const SEARCH_DEBOUNCE_MS = 350

const numberFormat = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })
const formatWeight = (value) => `${numberFormat.format(value ?? 0)} kg`
const formatVolume = (value) => `${numberFormat.format(value ?? 0)} m³`
const formatDistance = (value) => `${numberFormat.format(value ?? 0)} km`
const formatCurrency = (value) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    maximumFractionDigits: 0,
  }).format(value ?? 0)

const cityLabel = (value) => (value ? value.split(",")[0] : "—")

function statusBadgeClass(status) {
  switch (status) {
    case SEGMENT_STATUS.AVAILABLE:
      return "bg-emerald-50 text-emerald-700"
    case SEGMENT_STATUS.ROUTED:
      return "bg-blue-50 text-blue-700"
    case SEGMENT_STATUS.IN_TRANSIT:
      return "bg-amber-50 text-amber-700"
    case SEGMENT_STATUS.COMPLETED:
      return "bg-slate-100 text-slate-600"
    default:
      return "bg-slate-100 text-slate-600"
  }
}

export default function RouteSegmentManagement() {
  const navigate = useNavigate()
  const [searchTerm, setSearchTerm] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [isSelectionMode, setIsSelectionMode] = useState(false)
  const [selectedSegmentIds, setSelectedSegmentIds] = useState([])

  const [segments, setSegments] = useState([])
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [deletingId, setDeletingId] = useState(null)

  const listAction = useAsyncAction((query) => listRouteSegments(query))
  const saveAction = useAsyncAction(({ id, payload }) =>
    updateRouteSegment(id, payload)
  )
  const deleteAction = useAsyncAction((id) => deleteRouteSegment(id))
  const previewAction = useAsyncAction((segmentIds) => previewRoute(segmentIds))

  const listPending = listAction.pending

  const mappedFieldErrors = useMemo(
    () => mapRouteSegmentFieldErrors(saveAction.fieldErrors),
    [saveAction.fieldErrors]
  )

  useEffect(() => {
    const timer = setTimeout(
      () => setDebouncedSearch(searchTerm.trim()),
      SEARCH_DEBOUNCE_MS
    )
    return () => clearTimeout(timer)
  }, [searchTerm])

  const loadList = useCallback(async () => {
    const result = await listAction.run({
      origin: debouncedSearch || undefined,
      page: 1,
      pageSize: 50,
    })
    if (result.ok) {
      setSegments(result.data.items)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch])

  useEffect(() => {
    loadList()
  }, [loadList])

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
    previewAction.reset()
  }

  const handleProceedToWorkspace = async () => {
    const result = await previewAction.run(selectedSegmentIds)
    if (result.ok) {
      navigate("/offer-freight", {
        state: { segmentIds: selectedSegmentIds, preview: result.data },
      })
    }
  }

  const startEditing = (segment) => {
    setEditingId(segment.id)
    setEditForm({
      origin: segment.origin,
      destination: segment.destination,
      budgetCeiling: segment.budgetCeiling ?? "",
      estimatedTollCost: segment.estimatedTollCost ?? "",
      pickupDeadline: toDateTimeLocal(segment.pickupDeadline),
      deliveryDeadline: toDateTimeLocal(segment.deliveryDeadline),
      items: segment.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
      })),
    })
    setDeletingId(null)
    saveAction.reset()
  }

  const cancelEditing = () => {
    setEditingId(null)
    setEditForm(null)
    saveAction.reset()
  }

  const saveEdit = async () => {
    const result = await saveAction.run({ id: editingId, payload: editForm })
    if (result.ok) {
      setSegments((prev) =>
        prev.map((s) => (s.id === result.data.id ? result.data : s))
      )
      setEditingId(null)
      setEditForm(null)
    }
  }

  const confirmDelete = async (id) => {
    const result = await deleteAction.run(id)
    if (result.ok) {
      setSegments((prev) => prev.filter((s) => s.id !== id))
      setSelectedSegmentIds((prev) => prev.filter((selectedId) => selectedId !== id))
      setDeletingId(null)
    }
  }

  return (
    <AppShell title="Gestão de Trechos">
      <div className="mx-auto max-w-7xl space-y-4">
        {/* BARRA DE TOPO */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">
              Trechos Disponíveis
            </h1>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-72">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por origem..."
                className="h-9 border-slate-200 bg-white pl-9 text-xs"
              />
            </div>

            <Button
              variant={isSelectionMode ? "secondary" : "outline"}
              className={
                isSelectionMode
                  ? "h-9 border-blue-200 bg-blue-50 text-xs font-semibold text-blue-700 hover:bg-blue-100"
                  : "h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700"
              }
              onClick={() =>
                isSelectionMode ? handleCancelSelection() : setIsSelectionMode(true)
              }
            >
              <Layers size={14} className="mr-1.5" />{" "}
              {isSelectionMode ? "Cancelar" : "Criar Rota"}
            </Button>

            <Button
              asChild
              className="h-9 bg-blue-600 text-xs font-semibold text-white hover:bg-blue-700"
            >
              <Link to="/create-route-segment">
                <Plus size={14} className="mr-1.5" /> Novo Trecho
              </Link>
            </Button>
          </div>
        </div>

        {/* BANNER CONTEXTUAL */}
        {isSelectionMode && selectedSegmentIds.length > 0 && (
          <div className="animate-in fade-in flex items-center justify-between rounded-xl border border-blue-100 bg-blue-50 p-3 px-4 text-sm duration-200">
            <div className="flex items-center gap-2.5 text-blue-900">
              <span className="flex h-5 w-5 items-center justify-center rounded-md bg-blue-600 text-[10px] font-black tracking-wider text-white">
                {selectedSegmentIds.length}
              </span>
              <span className="text-xs font-medium text-slate-700">
                {selectedSegmentIds.length === 1
                  ? "Trecho selecionado e pronto para roteirização."
                  : "Trechos selecionados e prontos para roteirização conjunta."}
              </span>
            </div>
            <button
              onClick={handleProceedToWorkspace}
              disabled={previewAction.pending}
              className="flex items-center gap-1.5 pl-4 text-xs font-bold uppercase tracking-wider text-blue-700 hover:text-blue-800 disabled:opacity-60"
            >
              {previewAction.pending ? (
                <Loader2 size={14} className="animate-spin" />
              ) : null}
              Avançar para Criar Leilão{" "}
              <ArrowRight size={14} className="animate-pulse" />
            </button>
          </div>
        )}

        {previewAction.error ? (
          <FormAlert message={previewAction.error.message} />
        ) : null}
        {deleteAction.error ? (
          <FormAlert message={deleteAction.error.message} />
        ) : null}

        {/* LISTAGEM */}
        {listPending ? (
          <div className="flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white p-10 text-sm text-slate-500">
            <Loader2 size={16} className="animate-spin" /> Carregando trechos...
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
        ) : segments.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-200 bg-white p-10 text-center text-sm text-slate-500">
            Nenhum trecho encontrado.
          </div>
        ) : (
          <div className="space-y-2">
            {segments.map((segment) => {
              const isChecked = selectedSegmentIds.includes(segment.id)
              const isEditingThis = editingId === segment.id
              const isDeletingThis = deletingId === segment.id
              const editable = isSegmentEditable(segment.status)

              if (isDeletingThis) {
                return (
                  <div
                    key={segment.id}
                    className="flex w-full items-center justify-between gap-4 rounded-xl border border-rose-200 bg-rose-50/50 p-3.5 animate-in fade-in"
                  >
                    <div className="flex items-center gap-3 text-rose-700 pl-2">
                      <AlertCircle size={16} />
                      <span className="text-sm font-bold">
                        Excluir permanentemente este trecho?
                      </span>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setDeletingId(null)}
                        disabled={deleteAction.pending}
                        className="h-8 text-xs font-semibold bg-white border-slate-200 text-slate-600"
                      >
                        Cancelar
                      </Button>
                      <Button
                        size="sm"
                        onClick={() => confirmDelete(segment.id)}
                        disabled={deleteAction.pending}
                        className="h-8 text-xs font-bold bg-rose-600 text-white hover:bg-rose-700"
                      >
                        {deleteAction.pending ? (
                          <Loader2 size={14} className="mr-1.5 animate-spin" />
                        ) : null}
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
                  {/* LINHA PRINCIPAL VISÍVEL */}
                  <div className="flex items-center w-full p-3.5 text-left">
                    {isSelectionMode && !isEditingThis && (
                      <div className="flex items-center justify-center pl-1 mr-4">
                        <input
                          type="checkbox"
                          checked={isChecked}
                          onChange={() => handleToggleSegment(segment.id)}
                          className="h-4 w-4 cursor-pointer rounded border-slate-300 accent-blue-600"
                        />
                      </div>
                    )}

                    <div className="grid min-w-0 flex-1 grid-cols-[130px_1.3fr_250px] items-center gap-6">
                      <div>
                        <div className="mb-1 font-mono text-xs font-bold text-slate-500">
                          {String(segment.id).slice(0, 8)}
                        </div>
                        <Badge
                          variant="secondary"
                          className={`text-[10px] font-bold uppercase tracking-wide border-none px-1.5 py-0 ${statusBadgeClass(
                            segment.status
                          )}`}
                        >
                          {segmentStatusLabel(segment.status)}
                        </Badge>
                      </div>

                      <div className="min-w-0">
                        <p className="truncate text-sm font-bold text-slate-700 flex items-center gap-1.5">
                          {cityLabel(segment.origin)}{" "}
                          <span className="text-slate-300 text-xs">➔</span>{" "}
                          {cityLabel(segment.destination)}
                          <span className="text-xs font-mono font-medium text-slate-400 bg-slate-50 px-1 rounded border border-slate-100">
                            {formatDistance(segment.distanceKm)}
                          </span>
                        </p>
                        <span className="text-[11px] text-slate-400 font-medium block mt-0.5">
                          {segment.items.length}{" "}
                          {segment.items.length === 1 ? "produto" : "produtos"} ·
                          ~{numberFormat.format(segment.estimatedTimeHours)}h
                        </span>
                      </div>

                      <div className="flex items-center justify-end gap-3 text-xs font-bold whitespace-nowrap">
                        <div className="flex gap-1 text-slate-500 bg-slate-50 px-2 py-1 rounded border border-slate-100">
                          <span>{formatWeight(segment.calculatedTotals.totalWeightKg)}</span>
                          <span className="text-slate-300">•</span>
                          <span>{formatVolume(segment.calculatedTotals.totalVolumeM3)}</span>
                        </div>
                        <div className="text-right min-w-[85px]">
                          <span className="text-[10px] text-slate-400 block font-semibold uppercase tracking-wider">
                            Teto
                          </span>
                          <span className="text-sm font-black text-slate-700 font-mono">
                            {segment.budgetCeiling != null
                              ? formatCurrency(segment.budgetCeiling)
                              : "—"}
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* AÇÕES */}
                    {!isSelectionMode && !isEditingThis && editable && (
                      <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1 opacity-0 group-hover:opacity-100 bg-white pl-2 transition-opacity duration-200">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => startEditing(segment)}
                          className="h-8 w-8 text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0 transition-colors"
                        >
                          <Pencil size={14} />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setDeletingId(segment.id)}
                          className="h-8 w-8 text-slate-400 hover:text-rose-600 hover:bg-rose-50 shrink-0 transition-colors"
                        >
                          <Trash2 size={14} />
                        </Button>
                      </div>
                    )}
                  </div>

                  {/* FORMULÁRIO DE EDIÇÃO INLINE */}
                  {isEditingThis && editForm && (
                    <div className="border-t border-slate-100 bg-slate-50/50 p-6 animate-in slide-in-from-top-2 fade-in duration-200 rounded-b-xl">
                      {saveAction.error ? (
                        <div className="mb-4">
                          <FormAlert message={saveAction.error.message} />
                        </div>
                      ) : null}

                      <div className="grid grid-cols-1 md:grid-cols-3 gap-x-6 gap-y-5 mb-6">
                        <div className="space-y-1.5">
                          <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
                            Local de Coleta (Origem)
                          </label>
                          <Input
                            value={editForm.origin}
                            onChange={(e) =>
                              setEditForm({ ...editForm, origin: e.target.value })
                            }
                            className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500"
                          />
                          {mappedFieldErrors.origin ? (
                            <p className="text-[10px] font-medium text-rose-600">
                              {mappedFieldErrors.origin}
                            </p>
                          ) : null}
                        </div>
                        <div className="space-y-1.5">
                          <label className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
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
                            className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500"
                          />
                          {mappedFieldErrors.destination ? (
                            <p className="text-[10px] font-medium text-rose-600">
                              {mappedFieldErrors.destination}
                            </p>
                          ) : null}
                        </div>
                        <div className="space-y-1.5">
                          <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                            <DollarSign size={12} className="text-emerald-500" />{" "}
                            Orçamento Teto
                          </label>
                          <div className="relative">
                            <span className="absolute left-2.5 top-2.5 text-xs font-bold text-slate-400">
                              R$
                            </span>
                            <Input
                              type="number"
                              min={0}
                              step="0.01"
                              value={editForm.budgetCeiling}
                              onChange={(e) =>
                                setEditForm({
                                  ...editForm,
                                  budgetCeiling: e.target.value,
                                })
                              }
                              className="h-9 border-slate-200 pl-8 text-xs font-mono font-semibold bg-white focus:border-blue-500 focus:ring-blue-500"
                            />
                          </div>
                          {mappedFieldErrors.budgetCeiling ? (
                            <p className="text-[10px] font-medium text-rose-600">
                              {mappedFieldErrors.budgetCeiling}
                            </p>
                          ) : null}
                        </div>

                        <div className="space-y-1.5">
                          <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                            <CalendarClock size={12} className="text-amber-500" />{" "}
                            Coleta Limite
                          </label>
                          <Input
                            type="datetime-local"
                            value={editForm.pickupDeadline}
                            onChange={(e) =>
                              setEditForm({
                                ...editForm,
                                pickupDeadline: e.target.value,
                              })
                            }
                            className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500"
                          />
                          {mappedFieldErrors.pickupDeadline ? (
                            <p className="text-[10px] font-medium text-rose-600">
                              {mappedFieldErrors.pickupDeadline}
                            </p>
                          ) : null}
                        </div>
                        <div className="space-y-1.5">
                          <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                            <CalendarClock size={12} className="text-amber-500" />{" "}
                            Entrega Limite
                          </label>
                          <Input
                            type="datetime-local"
                            value={editForm.deliveryDeadline}
                            onChange={(e) =>
                              setEditForm({
                                ...editForm,
                                deliveryDeadline: e.target.value,
                              })
                            }
                            className="h-9 text-xs bg-white focus:border-blue-500 focus:ring-blue-500"
                          />
                          {mappedFieldErrors.deliveryDeadline ? (
                            <p className="text-[10px] font-medium text-rose-600">
                              {mappedFieldErrors.deliveryDeadline}
                            </p>
                          ) : null}
                        </div>
                        <div className="space-y-1.5">
                          <label className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                            <DollarSign size={12} className="text-slate-400" />{" "}
                            Pedágio Estimado
                          </label>
                          <div className="relative">
                            <span className="absolute left-2.5 top-2.5 text-xs font-bold text-slate-400">
                              R$
                            </span>
                            <Input
                              type="number"
                              min={0}
                              step="0.01"
                              value={editForm.estimatedTollCost}
                              onChange={(e) =>
                                setEditForm({
                                  ...editForm,
                                  estimatedTollCost: e.target.value,
                                })
                              }
                              className="h-9 border-slate-200 pl-8 text-xs font-mono font-semibold bg-white focus:border-blue-500 focus:ring-blue-500"
                            />
                          </div>
                          {mappedFieldErrors.estimatedTollCost ? (
                            <p className="text-[10px] font-medium text-rose-600">
                              {mappedFieldErrors.estimatedTollCost}
                            </p>
                          ) : null}
                        </div>
                      </div>

                      <div className="mb-6 rounded-lg border border-slate-200 bg-white p-3">
                        <div className="mb-2 flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                          <Box size={12} className="text-slate-400" /> Produtos do
                          trecho
                        </div>
                        {editForm.items.length === 0 ? (
                          <p className="text-[11px] text-slate-500">
                            Nenhum produto vinculado.
                          </p>
                        ) : (
                          <div className="space-y-2">
                            {editForm.items.map((item, index) => {
                              const detail = segment.items.find(
                                (i) => i.productId === item.productId
                              )
                              return (
                                <div
                                  key={item.productId}
                                  className="flex items-center justify-between gap-2"
                                >
                                  <div className="flex min-w-0 items-center gap-2">
                                    <span className="rounded border border-slate-200 bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] font-bold tracking-wider text-slate-600 uppercase">
                                      {detail?.sku ?? ""}
                                    </span>
                                    <span className="truncate text-xs font-semibold text-slate-700">
                                      {detail?.name ?? item.productId}
                                    </span>
                                  </div>
                                  <div className="flex items-center gap-1">
                                    <label className="text-[10px] font-semibold text-slate-400">
                                      Qtd
                                    </label>
                                    <Input
                                      type="number"
                                      min={1}
                                      value={item.quantity}
                                      onChange={(e) => {
                                        const value =
                                          e.target.value === ""
                                            ? ""
                                            : Number(e.target.value)
                                        setEditForm((current) => ({
                                          ...current,
                                          items: current.items.map((it, i) =>
                                            i === index
                                              ? { ...it, quantity: value }
                                              : it
                                          ),
                                        }))
                                      }}
                                      className="h-7 w-16 border-slate-200 text-center text-xs bg-white focus:border-blue-500 focus:ring-blue-500"
                                    />
                                  </div>
                                </div>
                              )
                            })}
                          </div>
                        )}
                        {mappedFieldErrors.items ? (
                          <p className="mt-2 text-[10px] font-medium text-rose-600">
                            {mappedFieldErrors.items}
                          </p>
                        ) : null}
                      </div>

                      <div className="flex items-center justify-between border-t border-slate-200 pt-4">
                        <p className="text-[10px] text-slate-500 flex items-center gap-1.5">
                          <AlertCircle size={12} /> Distância, tempo e cubagem são
                          recalculados automaticamente.
                        </p>
                        <div className="flex gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={cancelEditing}
                            disabled={saveAction.pending}
                            className="h-8 text-xs font-semibold bg-white"
                          >
                            <X size={14} className="mr-1.5" /> Cancelar
                          </Button>
                          <Button
                            size="sm"
                            onClick={saveEdit}
                            disabled={saveAction.pending}
                            className="h-8 text-xs font-bold bg-blue-600 text-white hover:bg-blue-700"
                          >
                            {saveAction.pending ? (
                              <Loader2 size={14} className="mr-1.5 animate-spin" />
                            ) : (
                              <Save size={14} className="mr-1.5" />
                            )}
                            Salvar Alterações
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
