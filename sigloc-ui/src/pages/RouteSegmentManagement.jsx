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
  Archive,
  SlidersHorizontal,
} from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { FormAlert } from "@/components/auth/FormAlert"
import { useAsyncAction } from "@/hooks/use-async-action"
import { useInfiniteList } from "@/hooks/use-infinite-list"
import { InfiniteScrollSentinel } from "@/components/infinite-scroll-sentinel"
import { ListFilters, activeFilterCount, filtersLabel } from "@/components/list-filters"
import { RISK, RISK_OPTIONS } from "@/constants/risk"
import {
  listRouteSegments,
  updateRouteSegment,
  deleteRouteSegment,
  toDateTimeLocal,
} from "@/services/route-segment-service"
import { previewRoute } from "@/services/route-service"
import { formatDeadline } from "@/lib/deadline-formatters"
import {
  isSegmentEditable,
  segmentShortId,
  segmentStatusLabel,
  isPickupOverdue,
  CARGO_CLASS_BADGES,
  cargoSelectionBlock,
  SEGMENT_STATUS,
  mapRouteSegmentFieldErrors,
} from "@/constants/route-segments"

const SEARCH_DEBOUNCE_MS = 350
const PAGE_SIZE = 20

/** Segments shown in the "Disponíveis" tab (editable and available for new routes). */
const ACTIVE_STATUSES = [SEGMENT_STATUS.AVAILABLE]
/** Segments shown in the "Histórico" tab: in a route, in transit, or completed. */
const HISTORY_STATUSES = [
  SEGMENT_STATUS.ROUTED,
  SEGMENT_STATUS.IN_EXECUTION,
  SEGMENT_STATUS.COMPLETED,
]

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
    case SEGMENT_STATUS.IN_EXECUTION:
      return "bg-amber-50 text-amber-700"
    case SEGMENT_STATUS.COMPLETED:
      return "bg-slate-100 text-slate-600"
    default:
      return "bg-slate-100 text-slate-600"
  }
}

/** Filter config for the risk dropdown (reuses the shared ListFilters component). */
const FILTER_FIELDS = [
  { key: "risk", label: "Filtrar por risco", allLabel: "Todos os riscos", options: RISK_OPTIONS },
]
const NO_FILTERS = { risk: "" }

/** Dot color + hover message per risk level. Risk comes from pickup-deadline proximity. */
const RISK_DOT = {
  [RISK.CRITIC]: {
    dot: "bg-red-500",
    message: "Risco crítico: o prazo de coleta está muito próximo (ou já passou) e o trecho ainda não foi ofertado em leilão.",
  },
  [RISK.WARNING]: {
    dot: "bg-amber-400",
    message: "Risco de atenção: o prazo de coleta está se aproximando e o trecho ainda não foi ofertado em leilão.",
  },
  [RISK.NORMAL]: {
    dot: "bg-emerald-500",
    message: "Sem risco: o prazo de coleta está confortável ou o trecho já foi roteirizado.",
  },
}

function RiskDot({ risk }) {
  const config = RISK_DOT[risk] ?? RISK_DOT[RISK.NORMAL]
  return (
    <span
      role="img"
      aria-label={config.message}
      title={config.message}
      className={`inline-block h-2.5 w-2.5 shrink-0 rounded-full ${config.dot}`}
    />
  )
}

export default function RouteSegmentManagement() {
  const navigate = useNavigate()
  const [searchTerm, setSearchTerm] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [selectedTab, setSelectedTab] = useState("active")
  const [isSelectionMode, setIsSelectionMode] = useState(false)
  const [selectedSegmentIds, setSelectedSegmentIds] = useState([])
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [filters, setFilters] = useState(NO_FILTERS)

  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState(null)
  const [deletingId, setDeletingId] = useState(null)

  const saveAction = useAsyncAction(({ id, payload }) =>
    updateRouteSegment(id, payload)
  )
  const deleteAction = useAsyncAction((id) => deleteRouteSegment(id))
  const previewAction = useAsyncAction((segmentIds) => previewRoute(segmentIds))

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

  // Infinite scroll: a page loads the AVAILABLE segments (active tab) or merges the three history
  // statuses for that page. The filter signature resets the list when the tab or search changes.
  const filterKey = `${selectedTab}:${debouncedSearch}`
  const loadPage = useCallback(
    async (page, options) => {
      const origin = debouncedSearch || undefined
      const statuses =
        selectedTab === "history" ? HISTORY_STATUSES : ACTIVE_STATUSES
      const responses = await Promise.all(
        statuses.map((status) =>
          listRouteSegments({ origin, status, page, pageSize: PAGE_SIZE }, options)
        )
      )
      const items =
        statuses.length === 1
          ? responses[0].items
          : responses
              .flatMap((response) => response.items)
              .sort(
                (first, second) =>
                  new Date(second.createdAt ?? 0) - new Date(first.createdAt ?? 0)
              )
      const totalPages = Math.max(1, ...responses.map((response) => response.totalPages || 1))
      return { items, totalPages }
    },
    [debouncedSearch, selectedTab]
  )
  const {
    items: segments,
    pending: listPending,
    error: listError,
    hasMore,
    loadMore,
    reload,
  } = useInfiniteList(loadPage, filterKey)
  const loadingMore = listPending && segments.length > 0

  // Risk is computed server-side but not a filterable column, so filter the loaded page client-side
  // (mirrors how the auctions panel filters risk).
  const visibleSegments = useMemo(
    () => (filters.risk ? segments.filter((segment) => segment.risk === filters.risk) : segments),
    [segments, filters.risk]
  )

  const handleTabChange = (value) => {
    setSelectedTab(value)
    setEditingId(null)
    setEditForm(null)
    setDeletingId(null)
    setIsSelectionMode(false)
    setSelectedSegmentIds([])
    setFilters(NO_FILTERS)
    setFiltersOpen(false)
    saveAction.reset()
    deleteAction.reset()
    previewAction.reset()
  }

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

  // Class of the segments already ticked: only segments of the same class can join them.
  const selectedCargoClass = segments.find((segment) => selectedSegmentIds.includes(segment.id))?.cargoClass ?? null

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
      setEditingId(null)
      setEditForm(null)
      reload()
    }
  }

  const confirmDelete = async (id) => {
    const result = await deleteAction.run(id)
    if (result.ok) {
      setSelectedSegmentIds((prev) => prev.filter((selectedId) => selectedId !== id))
      setDeletingId(null)
      reload()
    }
  }

  return (
    <AppShell title="Gestão de Trechos">
      <div className="space-y-4">
        {/* BARRA DE TOPO */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">
              {selectedTab === "history" ? "Histórico de Trechos" : "Trechos Disponíveis"}
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
              variant="outline"
              aria-expanded={filtersOpen}
              onClick={() => setFiltersOpen((open) => !open)}
              className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700 hover:bg-slate-50"
            >
              <SlidersHorizontal size={14} className="mr-1.5 text-slate-500" /> {filtersLabel(filters)}
            </Button>

            {selectedTab === "active" && (
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
            )}

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

        {filtersOpen && (
          <ListFilters filters={filters} fields={FILTER_FIELDS} onChange={setFilters} />
        )}

        {/* ABAS: DISPONÍVEIS / HISTÓRICO */}
        <Tabs value={selectedTab} onValueChange={handleTabChange}>
          <TabsList className="bg-slate-100">
            <TabsTrigger value="active" className="text-xs font-semibold">
              Disponíveis
            </TabsTrigger>
            <TabsTrigger value="history" className="text-xs font-semibold">
              <Archive size={13} className="mr-1.5" /> Histórico
            </TabsTrigger>
          </TabsList>
        </Tabs>

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
          <FormAlert message={[previewAction.error.message, previewAction.fieldErrors.segmentIds].filter(Boolean).join(" ")} />
        ) : null}
        {deleteAction.error ? (
          <FormAlert message={deleteAction.error.message} />
        ) : null}

        {/* LISTAGEM */}
        {listPending && segments.length === 0 ? (
          <div className="flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white p-10 text-sm text-slate-500">
            <Loader2 size={16} className="animate-spin" /> Carregando trechos...
          </div>
        ) : listError && segments.length === 0 ? (
          <div className="space-y-3 rounded-xl border border-slate-200 bg-white p-6">
            <FormAlert message={listError.message} />
            <Button
              variant="outline"
              size="sm"
              onClick={reload}
              className="h-8 text-xs font-semibold"
            >
              Tentar novamente
            </Button>
          </div>
        ) : visibleSegments.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-200 bg-white p-10 text-center text-sm text-slate-500">
            {activeFilterCount(filters)
              ? "Nenhum trecho encontrado para os filtros selecionados."
              : selectedTab === "history"
                ? "Nenhum trecho no histórico encontrado."
                : "Nenhum trecho disponível encontrado."}
          </div>
        ) : (
          <div className="space-y-2">
            {visibleSegments.map((segment) => {
              const isChecked = selectedSegmentIds.includes(segment.id)
              const isEditingThis = editingId === segment.id
              const isDeletingThis = deletingId === segment.id
              const editable = isSegmentEditable(segment.status)
              const pickupOverdue = isPickupOverdue(segment)
              const cargoBlock = isChecked ? null : cargoSelectionBlock(segment, selectedCargoClass)
              const selectable = editable && !pickupOverdue && !cargoBlock
              const cargoBadge = CARGO_CLASS_BADGES[segment.cargoClass]

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
                          disabled={!selectable}
                          title={
                            pickupOverdue
                              ? "O prazo de coleta já passou. Edite o prazo para ofertar este trecho."
                              : !editable
                                ? "Somente trechos disponíveis podem entrar em uma nova rota."
                                : (cargoBlock ?? undefined)
                          }
                          onChange={() => handleToggleSegment(segment.id)}
                          className="h-4 w-4 cursor-pointer rounded border-slate-300 accent-blue-600 disabled:cursor-not-allowed disabled:opacity-40"
                        />
                      </div>
                    )}

                    <div className="grid min-w-0 flex-1 grid-cols-[120px_1.2fr_1.5fr_230px] items-center gap-5">
                      <div>
                        <div className="mb-1 font-mono text-xs font-bold text-slate-500">
                          {segmentShortId(segment.id)}
                        </div>
                        <div className="flex items-center gap-1.5">
                          <RiskDot risk={segment.risk} />
                          <Badge
                            variant="secondary"
                            className={`text-[10px] font-bold uppercase tracking-wide border-none px-1.5 py-0 ${
                              pickupOverdue ? "bg-rose-50 text-rose-700" : statusBadgeClass(segment.status)
                            }`}
                          >
                            {pickupOverdue ? "Coleta vencida" : segmentStatusLabel(segment.status)}
                          </Badge>
                          {cargoBadge && (
                            <Badge
                              variant="secondary"
                              title="Trechos só podem formar uma rota com outros da mesma classe de carga."
                              className={`text-[10px] font-bold uppercase tracking-wide border-none px-1.5 py-0 ${cargoBadge.className}`}
                            >
                              {cargoBadge.label}
                            </Badge>
                          )}
                        </div>
                      </div>

                      <div className="min-w-0">
                        <p title={`${segment.origin} ➔ ${segment.destination}`} className="truncate text-sm font-bold text-slate-700 flex items-center gap-1.5">
                          {cityLabel(segment.originCity ?? segment.origin)}{" "}
                          <span className="text-slate-300 text-xs">➔</span>{" "}
                          {cityLabel(segment.destinationCity ?? segment.destination)}
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

                      <div className="min-w-0 grid grid-cols-2 gap-2">
                        <div className="rounded-lg border border-slate-100 bg-slate-50 px-2.5 py-1.5">
                          <div className="flex items-center gap-1 text-[9px] font-bold uppercase tracking-wider text-slate-400">
                            <CalendarClock size={10} /> Coleta
                          </div>
                          <div className="mt-0.5 truncate text-xs font-bold text-slate-700">
                            {formatDeadline(segment.pickupDeadline)}
                          </div>
                        </div>
                        <div className="rounded-lg border border-slate-100 bg-slate-50 px-2.5 py-1.5">
                          <div className="flex items-center gap-1 text-[9px] font-bold uppercase tracking-wider text-slate-400">
                            <CalendarClock size={10} /> Entrega
                          </div>
                          <div className="mt-0.5 truncate text-xs font-bold text-slate-700">
                            {formatDeadline(segment.deliveryDeadline)}
                          </div>
                        </div>
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
                          aria-label={`Editar trecho ${segmentShortId(segment.id)}`}
                          title="Editar trecho"
                          className="h-8 w-8 text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0 transition-colors"
                        >
                          <Pencil size={14} />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setDeletingId(segment.id)}
                          aria-label={`Excluir trecho ${segmentShortId(segment.id)}`}
                          title="Excluir trecho"
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
                        {segment.cargoClass === "MISTA" && (
                          <p className="mb-2 text-[11px] font-medium text-amber-700">
                            Este trecho mistura classes de carga (perigosa, frágil e comum não podem ir juntas).
                            Remova os produtos de outra classe para salvar.
                          </p>
                        )}
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
                                      {detail?.name ?? "Produto"}
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
                                    {/* Lets an older segment that mixes cargo classes be split back to one class. */}
                                    {editForm.items.length > 1 && (
                                      <Button
                                        type="button"
                                        variant="ghost"
                                        size="icon"
                                        aria-label={`Remover ${detail?.name ?? "produto"} do trecho`}
                                        title="Remover do trecho"
                                        onClick={() =>
                                          setEditForm((current) => ({
                                            ...current,
                                            items: current.items.filter((_, i) => i !== index),
                                          }))
                                        }
                                        className="h-7 w-7 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                                      >
                                        <X size={12} />
                                      </Button>
                                    )}
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

            <InfiniteScrollSentinel
              hasMore={hasMore}
              loadingMore={loadingMore}
              onLoadMore={loadMore}
              count={visibleSegments.length}
              singular="trecho"
              plural="trechos"
            />
          </div>
        )}
      </div>
    </AppShell>
  )
}
