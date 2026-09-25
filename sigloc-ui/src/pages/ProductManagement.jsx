import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import {
  Box,
  Flame,
  Info,
  Loader2,
  PackageOpen,
  Pencil,
  Plus,
  Save,
  Search,
  Snowflake,
  Tag,
  Trash2,
  Truck,
  X,
} from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { FormAlert } from "@/components/auth/FormAlert"
import { ProductFormFields } from "@/components/products/ProductFormFields"
import { useProductForm } from "@/hooks/use-product-form"
import { useAsyncAction } from "@/hooks/use-async-action"
import { productService } from "@/services/product-service"
import {
  categoryLabel,
  environmentLabel,
  mapProductFieldErrors,
  TRANSPORT_ENVIRONMENT,
} from "@/constants/products"

const SEARCH_DEBOUNCE_MS = 350

function ListItem({ product, selected, onSelect }) {
  const isChilled = product.transportEnvironment !== TRANSPORT_ENVIRONMENT.DRY

  return (
    <button
      onClick={() => onSelect(product.id)}
      className={`flex w-full items-start gap-4 p-4 text-left transition-colors focus:outline-none ${
        selected ? "bg-blue-50/50" : "bg-white hover:bg-slate-50/70"
      }`}
    >
      <div
        className={`mt-1 h-2 w-2 shrink-0 rounded-full ${
          selected ? "bg-blue-600" : "bg-transparent"
        }`}
      />
      <div className="min-w-0 flex-1">
        <div className="mb-1.5 flex items-center gap-2">
          <span className="font-mono text-xs font-bold text-slate-500">
            {product.sku}
          </span>
          <div className="flex gap-1">
            {product.dangerous && (
              <Badge
                variant="secondary"
                className="flex items-center border-none bg-amber-100 px-1.5 py-0 text-[9px] font-black uppercase tracking-wider text-amber-700"
              >
                <Flame size={10} className="mr-1" /> Hazmat
              </Badge>
            )}
            {isChilled && (
              <Badge
                variant="secondary"
                className="flex items-center border-none bg-sky-100 px-1.5 py-0 text-[9px] font-black uppercase tracking-wider text-sky-700"
              >
                <Snowflake size={10} className="mr-1" />
                {environmentLabel(product.transportEnvironment)}
              </Badge>
            )}
            {product.fragile && (
              <Badge
                variant="secondary"
                className="border-none bg-rose-50 px-1.5 py-0 text-[9px] font-black uppercase tracking-wider text-rose-600"
              >
                Frágil
              </Badge>
            )}
          </div>
        </div>
        <h3
          className={`truncate text-sm ${
            selected ? "font-bold text-blue-900" : "font-semibold text-slate-800"
          }`}
        >
          {product.name}
        </h3>
        <p className="mt-1 flex items-center gap-1.5 text-[11px] font-medium text-slate-400">
          <Tag size={12} /> {categoryLabel(product.category)} •{" "}
          {product.defaultWeight}kg
        </p>
      </div>
    </button>
  )
}

function VehicleRequirementCard({ requirement }) {
  if (!requirement) {
    return null
  }

  const rows = [
    ["Carroceria base", requirement.baseBodyworkType],
    ["Refrigeração mínima", requirement.minRefrigerationLevel],
    ["Exige MOPP", requirement.requiresMopp ? "Sim" : "Não"],
    ["Fixação de carga", requirement.requiresCargoFixing ? "Sim" : "Não"],
  ]

  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50/50 md:col-span-2">
      <div className="flex h-[52px] items-center gap-2 rounded-t-xl border-b border-slate-100 px-5">
        <Truck size={16} className="text-slate-600" />
        <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">
          Requisito de Veículo (calculado)
        </h2>
      </div>
      <div className="grid grid-cols-2 gap-4 p-5">
        {rows.map(([label, value]) => (
          <div key={label} className="space-y-1">
            <p className="text-[11px] font-bold uppercase tracking-wider text-slate-400">
              {label}
            </p>
            <p className="text-sm font-semibold text-slate-800">{value}</p>
          </div>
        ))}
      </div>
    </div>
  )
}

export default function ProductManagement() {
  const navigate = useNavigate()

  const [searchTerm, setSearchTerm] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [items, setItems] = useState([])
  const [totalItems, setTotalItems] = useState(0)
  const [selectedId, setSelectedId] = useState(null)
  const [detail, setDetail] = useState(null)
  const [isEditing, setIsEditing] = useState(false)

  const listAction = useAsyncAction((query) => productService.listProducts(query))
  const detailAction = useAsyncAction((id) => productService.getProduct(id))
  const saveAction = useAsyncAction(({ id, payload }) =>
    productService.updateProduct(id, payload)
  )
  const deleteAction = useAsyncAction((id) => productService.deleteProduct(id))

  const { form, setField, reset, showTemperature, showPackaging } =
    useProductForm(detail)

  const mappedFieldErrors = useMemo(
    () => mapProductFieldErrors(saveAction.fieldErrors),
    [saveAction.fieldErrors]
  )

  // Debounce the search input.
  useEffect(() => {
    const handle = setTimeout(
      () => setDebouncedSearch(searchTerm.trim()),
      SEARCH_DEBOUNCE_MS
    )
    return () => clearTimeout(handle)
  }, [searchTerm])

  const loadList = useCallback(async () => {
    const result = await listAction.run({
      search: debouncedSearch || undefined,
      page: 1,
      pageSize: 50,
    })
    if (result.ok) {
      setItems(result.data.items)
      setTotalItems(result.data.totalItems)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch])

  useEffect(() => {
    loadList()
  }, [loadList])

  const loadDetail = useCallback(
    async (id) => {
      const result = await detailAction.run(id)
      if (result.ok) {
        setDetail(result.data)
        reset(result.data)
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [reset]
  )

  const handleSelect = (id) => {
    setSelectedId(id)
    setIsEditing(false)
    saveAction.reset()
    deleteAction.reset()
    loadDetail(id)
  }

  const handleStartEdit = () => {
    reset(detail)
    saveAction.reset()
    setIsEditing(true)
  }

  const handleCancelEdit = () => {
    reset(detail)
    saveAction.reset()
    setIsEditing(false)
  }

  const handleSave = async () => {
    const result = await saveAction.run({ id: selectedId, payload: form })
    if (result.ok) {
      setDetail(result.data)
      reset(result.data)
      setIsEditing(false)
      // Reflect edits (name/sku/flags) in the list without a full refetch.
      setItems((current) =>
        current.map((item) =>
          item.id === result.data.id
            ? {
                ...item,
                sku: result.data.sku,
                name: result.data.name,
                category: result.data.category,
                transportEnvironment: result.data.transportEnvironment,
                dangerous: result.data.dangerous,
                fragile: result.data.fragile,
                defaultWeight: result.data.defaultWeight,
                defaultVolume: result.data.defaultVolume,
              }
            : item
        )
      )
    }
  }

  const handleDelete = async () => {
    if (!selectedId) {
      return
    }
    const result = await deleteAction.run(selectedId)
    if (result.ok) {
      setItems((current) => current.filter((item) => item.id !== selectedId))
      setTotalItems((count) => Math.max(0, count - 1))
      setSelectedId(null)
      setDetail(null)
      setIsEditing(false)
    }
  }

  const listPending = listAction.pending
  const detailPending = detailAction.pending

  return (
    <AppShell title="Catálogo de Produtos">
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-7xl gap-6 overflow-hidden">
        {/* PAINEL ESQUERDO: LISTAGEM */}
        <div className="flex w-full flex-col rounded-xl border border-slate-200 bg-white lg:w-1/2 xl:w-[45%]">
          <div className="flex shrink-0 flex-col gap-3 border-b border-slate-100 p-4">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-bold text-slate-800">
                Catálogo de SKUs ({totalItems})
              </h2>
              <Button
                size="sm"
                onClick={() => navigate("/register-product")}
                className="h-8 bg-blue-600 text-xs font-bold text-white hover:bg-blue-700"
              >
                <Plus size={14} className="mr-1.5" /> Novo SKU
              </Button>
            </div>
            <div className="relative">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por nome ou código..."
                className="h-9 border-slate-200 bg-slate-50 pl-9 text-xs focus:bg-white"
              />
            </div>
          </div>

          <div className="flex-1 overflow-y-auto">
            {listPending ? (
              <div className="flex items-center justify-center gap-2 p-8 text-sm text-slate-500">
                <Loader2 size={16} className="animate-spin" /> Carregando
                produtos...
              </div>
            ) : listAction.error ? (
              <div className="p-6">
                <FormAlert message={listAction.error.message} />
                <Button
                  variant="outline"
                  size="sm"
                  onClick={loadList}
                  className="mt-4 h-8 text-xs"
                >
                  Tentar novamente
                </Button>
              </div>
            ) : items.length === 0 ? (
              <div className="p-8 text-center text-sm text-slate-500">
                Nenhum produto encontrado.
              </div>
            ) : (
              <div className="flex flex-col divide-y divide-slate-100">
                {items.map((product) => (
                  <ListItem
                    key={product.id}
                    product={product}
                    selected={selectedId === product.id}
                    onSelect={handleSelect}
                  />
                ))}
              </div>
            )}
          </div>
        </div>

        {/* PAINEL DIREITO: DETALHES / EDIÇÃO */}
        <div className="hidden w-full flex-col rounded-xl border border-slate-200 bg-white lg:flex lg:w-1/2 xl:w-[55%]">
          {!selectedId ? (
            <div className="flex flex-1 flex-col items-center justify-center p-8 text-center text-slate-500">
              <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-slate-50">
                <PackageOpen size={32} className="text-slate-300" />
              </div>
              <p className="text-sm font-semibold text-slate-700">
                Nenhum item selecionado
              </p>
              <p className="mt-1 text-xs">
                Selecione um produto na lista ao lado para visualizar e editar
                seus parâmetros.
              </p>
            </div>
          ) : detailPending ? (
            <div className="flex flex-1 items-center justify-center gap-2 text-sm text-slate-500">
              <Loader2 size={16} className="animate-spin" /> Carregando
              detalhes...
            </div>
          ) : detailAction.error ? (
            <div className="p-6">
              <FormAlert message={detailAction.error.message} />
            </div>
          ) : detail ? (
            <>
              <div className="flex shrink-0 items-center justify-between border-b border-slate-100 bg-slate-50/50 p-4">
                <div className="flex items-center gap-2">
                  <Box size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">
                    Raio-X do Produto
                  </h2>
                  {!isEditing && (
                    <Badge
                      variant="outline"
                      className="ml-2 bg-white text-[9px] text-slate-400"
                    >
                      Somente Leitura
                    </Badge>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {!isEditing && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleStartEdit}
                      className="h-8 bg-white text-xs font-semibold text-slate-700 hover:bg-slate-100"
                    >
                      <Pencil size={14} className="mr-1.5" /> Editar
                    </Button>
                  )}
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={deleteAction.pending}
                    onClick={handleDelete}
                    className="h-8 text-xs font-semibold text-rose-600 hover:bg-rose-50 hover:text-rose-700"
                  >
                    {deleteAction.pending ? (
                      <Loader2 size={14} className="mr-1.5 animate-spin" />
                    ) : (
                      <Trash2 size={14} className="mr-1.5" />
                    )}
                    Excluir
                  </Button>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto p-6">
                {saveAction.error ? (
                  <div className="mb-5">
                    <FormAlert message={saveAction.error.message} />
                  </div>
                ) : null}
                {deleteAction.error ? (
                  <div className="mb-5">
                    <FormAlert message={deleteAction.error.message} />
                  </div>
                ) : null}

                <ProductFormFields
                  form={form}
                  setField={setField}
                  fieldErrors={mappedFieldErrors}
                  showTemperature={showTemperature}
                  showPackaging={showPackaging}
                  disabled={!isEditing}
                />

                {!isEditing && (
                  <div className="mt-6">
                    <VehicleRequirementCard
                      requirement={detail.vehicleRequirement}
                    />
                  </div>
                )}
              </div>

              {isEditing && (
                <div className="flex shrink-0 items-center justify-between border-t border-slate-100 bg-blue-50/50 p-4 duration-300 animate-in slide-in-from-bottom-4">
                  <div className="flex items-center gap-1.5 text-[10px] font-semibold text-blue-600">
                    <Info size={14} /> Modo de Edição Ativo
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      disabled={saveAction.pending}
                      className="h-8 bg-white text-xs font-semibold text-slate-700"
                      onClick={handleCancelEdit}
                    >
                      <X size={14} className="mr-1.5" /> Cancelar
                    </Button>
                    <Button
                      disabled={saveAction.pending}
                      className="h-8 bg-blue-600 text-xs font-bold text-white hover:bg-blue-700"
                      onClick={handleSave}
                    >
                      {saveAction.pending ? (
                        <Loader2 size={14} className="mr-1.5 animate-spin" />
                      ) : (
                        <Save size={14} className="mr-1.5" />
                      )}
                      Salvar Mudanças
                    </Button>
                  </div>
                </div>
              )}
            </>
          ) : null}
        </div>
      </div>
    </AppShell>
  )
}
