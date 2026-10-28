import { useMemo, useState } from "react"
import {
  ArrowLeft,
  PackageOpen,
  MapPinned,
  CalendarClock,
  DollarSign,
  Loader2,
  Save,
  X,
  Plus,
  Check,
  Search,
  Flame,
  Snowflake,
  Tag,
} from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { FormAlert } from "@/components/auth/FormAlert"
import { useProductOptions } from "@/hooks/use-product-options"
import { useAsyncAction } from "@/hooks/use-async-action"
import { createRouteSegment } from "@/services/route-segment-service"
import { cargoClassName, cargoClassOfProduct, CARGO_CLASS, mapRouteSegmentFieldErrors } from "@/constants/route-segments"
import {
  categoryLabel,
  environmentLabel,
  TRANSPORT_ENVIRONMENT,
} from "@/constants/products"

/**
 * Catalog row styled like the product-management list: SKU code, hazard/thermal
 * badges, bold name and a category·weight subline. Clicking toggles the product
 * in or out of the cargo so the same list serves browsing and selection.
 */
function CatalogItem({ product, selected, onToggle, blockedReason = null }) {
  const isChilled =
    product.transportEnvironment &&
    product.transportEnvironment !== TRANSPORT_ENVIRONMENT.DRY

  return (
    <button
      type="button"
      onClick={() => onToggle(product.value)}
      disabled={Boolean(blockedReason)}
      title={blockedReason ?? undefined}
      className={`flex w-full items-start gap-3 border-b border-slate-100 p-3 text-left transition-colors last:border-b-0 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 ${
        selected ? "bg-blue-50/60" : "bg-white hover:bg-slate-50/70 disabled:hover:bg-white"
      }`}
    >
      <span
        className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-md border transition-colors ${
          selected
            ? "border-blue-600 bg-blue-600 text-white"
            : "border-slate-300 bg-white text-transparent"
        }`}
      >
        <Check size={13} strokeWidth={3} />
      </span>

      <div className="min-w-0 flex-1">
        <div className="mb-1 flex items-center gap-2">
          <span
            title={product.sku}
            className="min-w-0 truncate font-mono text-[10px] font-bold tracking-wider text-slate-500 uppercase"
          >
            {product.sku}
          </span>
          <div className="flex shrink-0 gap-1">
            {product.dangerous && (
              <Badge
                variant="secondary"
                className="flex items-center border-none bg-amber-100 px-1.5 py-0 text-[9px] font-black tracking-wider text-amber-700 uppercase"
              >
                <Flame size={10} className="mr-1" /> Hazmat
              </Badge>
            )}
            {isChilled && (
              <Badge
                variant="secondary"
                className="flex items-center border-none bg-sky-100 px-1.5 py-0 text-[9px] font-black tracking-wider text-sky-700 uppercase"
              >
                <Snowflake size={10} className="mr-1" />
                {environmentLabel(product.transportEnvironment)}
              </Badge>
            )}
            {product.fragile && (
              <Badge
                variant="secondary"
                className="border-none bg-rose-50 px-1.5 py-0 text-[9px] font-black tracking-wider text-rose-600 uppercase"
              >
                Frágil
              </Badge>
            )}
          </div>
        </div>
        <h3
          title={product.name}
          className={`truncate text-sm ${
            selected ? "font-bold text-blue-900" : "font-semibold text-slate-800"
          }`}
        >
          {product.name}
        </h3>
        <p className="mt-0.5 flex items-center gap-1.5 text-[11px] font-medium text-slate-400">
          <Tag size={12} /> {categoryLabel(product.category)}
          {product.defaultWeight != null ? ` • ${product.defaultWeight}kg` : ""}
        </p>
        {blockedReason && <p className="mt-1 text-[10px] font-semibold text-slate-500">{blockedReason}</p>}
      </div>
    </button>
  )
}

export default function CreateRouteSegment() {
  const navigate = useNavigate()

  const { options: productOptions } = useProductOptions()

  const [origin, setOrigin] = useState("")
  const [destination, setDestination] = useState("")
  const [pickupDeadline, setPickupDeadline] = useState("")
  const [deliveryDeadline, setDeliveryDeadline] = useState("")
  const [budgetCeiling, setBudgetCeiling] = useState("")
  const [estimatedTollCost, setEstimatedTollCost] = useState("")

  // Selected products carry a quantity so the backend can compute the physical
  // totals and the consolidated vehicle requirement.
  const [selectedProducts, setSelectedProducts] = useState([])
  const [productSearch, setProductSearch] = useState("")
  const [itemsError, setItemsError] = useState(null)

  const { run, pending, error, fieldErrors } = useAsyncAction((form) =>
    createRouteSegment(form)
  )

  const mappedFieldErrors = useMemo(
    () => mapRouteSegmentFieldErrors(fieldErrors),
    [fieldErrors]
  )

  const filteredProductOptions = productOptions.filter((product) => {
    const search = productSearch.trim().toLowerCase()
    if (!search) return true

    return [product.sku, product.name, product.details]
      .filter(Boolean)
      .some((field) => field.toLowerCase().includes(search))
  })

  const selectedValues = useMemo(
    () => new Set(selectedProducts.map((p) => p.value)),
    [selectedProducts]
  )

  // A segment carries a single cargo class: once a product is in, only products of its class
  // (dangerous, fragile or common) can be added.
  const loadClass = selectedProducts.length > 0 ? cargoClassOfProduct(selectedProducts[0]) : null
  const blockedReasonFor = (product) =>
    loadClass && !selectedValues.has(product.value) && cargoClassOfProduct(product) !== loadClass
      ? `Não pode ir junto com ${cargoClassName(loadClass)}: crie outro trecho para ${cargoClassName(cargoClassOfProduct(product))}.`
      : null

  const handleToggleProduct = (productId) => {
    setSelectedProducts((current) => {
      if (current.some((p) => p.value === productId)) {
        return current.filter((p) => p.value !== productId)
      }
      const productToAdd = productOptions.find((p) => p.value === productId)
      return productToAdd
        ? [...current, { ...productToAdd, quantity: 1 }]
        : current
    })
  }

  const handleRemoveProduct = (productId) => {
    setSelectedProducts(selectedProducts.filter((p) => p.value !== productId))
  }

  const handleQuantityChange = (productId, quantity) => {
    setSelectedProducts((current) =>
      current.map((p) =>
        p.value === productId ? { ...p, quantity } : p
      )
    )
  }

  const handleSubmit = async (event) => {
    event.preventDefault()

    // The API only binds whole quantities; anything else would come back as a generic error.
    const badItem = selectedProducts.findIndex((p) => !(Number.isInteger(Number(p.quantity)) && Number(p.quantity) > 0))
    setItemsError(badItem >= 0 ? `A quantidade do item ${badItem + 1} deve ser um número inteiro maior que zero.` : null)
    if (badItem >= 0) return

    const form = {
      origin,
      destination,
      pickupDeadline,
      deliveryDeadline,
      budgetCeiling,
      estimatedTollCost,
      items: selectedProducts.map((p) => ({
        productId: p.value,
        quantity: p.quantity,
      })),
    }

    const result = await run(form)
    if (result.ok) {
      navigate("/route-segment-management")
    }
  }

  return (
    <AppShell title="Cadastro de Trecho">
      <form
        noValidate
        onSubmit={handleSubmit}
        className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden"
      >
        {/* HEADER LIMPO E TEXTUAL */}
        <div className="mb-4 flex shrink-0 items-center justify-between border-b border-slate-200 pt-1 pb-3">
          <Link to="/route-segment-management">
            <Button
              type="button"
              variant="ghost"
              className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900"
            >
              <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de
              Trechos
            </Button>
          </Link>
        </div>

        {error ? (
          <div className="mb-4 shrink-0">
            <FormAlert message={error.message} />
          </div>
        ) : null}

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2">
            <div className="grid gap-4 md:grid-cols-2 md:grid-rows-[auto_auto_auto]">
              {/* ========================================================
                  COLUNA ESQUERDA: ESPECIFICAÇÕES (ocupa toda a altura)
                  COLUNA DIREITA: ITINERÁRIO + PRAZOS + BALIZAMENTO
                  ======================================================== */}

              {/* Bloco 1: Composição da Carga (Esquerda, altura total) */}
              <div className="flex min-h-0 flex-col rounded-xl border border-slate-200 bg-white md:row-span-3 md:h-full">
                <div className="flex h-[52px] shrink-0 items-center justify-between gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
                  <div className="flex items-center gap-2">
                    <PackageOpen size={16} className="text-blue-600" />
                    <h2 className="text-xs font-bold tracking-wider text-slate-700 uppercase">
                      Composição da Carga
                    </h2>
                  </div>
                  <Badge
                    variant="secondary"
                    className="border-none bg-blue-100 px-2 py-0.5 text-[10px] font-bold text-blue-700"
                  >
                    {selectedProducts.length}{" "}
                    {selectedProducts.length === 1 ? "item" : "itens"}
                  </Badge>
                </div>

                <div className="flex min-h-0 flex-1 flex-col p-4">
                  {/* Busca no catálogo */}
                  <div className="relative shrink-0">
                    <Search className="absolute top-3 left-3 h-4 w-4 text-slate-400" />
                    <Input
                      value={productSearch}
                      onChange={(event) =>
                        setProductSearch(event.target.value)
                      }
                      placeholder="Buscar por nome ou código..."
                      className="h-10 border-slate-200 bg-slate-50 pl-9 text-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>

                  {/* Catálogo clicável (mesmo estilo de /product-management). md:h-0 keeps a long catalog from stretching the grid rows. */}
                  <div className="mt-3 min-h-0 flex-1 overflow-y-auto rounded-lg border border-slate-200 md:h-0">
                    {filteredProductOptions.length === 0 ? (
                      <div className="flex h-full items-center justify-center p-4 text-center text-xs text-slate-500">
                        Nenhum produto encontrado.
                      </div>
                    ) : (
                      filteredProductOptions.map((product) => (
                        <CatalogItem
                          key={product.id}
                          product={product}
                          selected={selectedValues.has(product.value)}
                          onToggle={handleToggleProduct}
                          blockedReason={blockedReasonFor(product)}
                        />
                      ))
                    )}
                  </div>

                  {/* Quantidades dos itens selecionados */}
                  <div className="mt-3 shrink-0">
                    <div className="flex items-center justify-between">
                      <label className="text-xs font-bold text-slate-600">
                        Itens na carga ({selectedProducts.length})
                      </label>
                      <button
                        type="button"
                        onClick={() => navigate("/register-product")}
                        className="flex items-center gap-1 text-[11px] font-semibold text-blue-600 hover:text-blue-700 hover:underline"
                      >
                        <Plus size={12} /> Cadastrar produto
                      </button>
                    </div>
                    {loadClass && loadClass !== CARGO_CLASS.COMUM && (
                      <p className="mt-1 text-[11px] font-medium text-slate-500">
                        Trecho de {cargoClassName(loadClass)}: só produtos da mesma classe podem ser adicionados.
                      </p>
                    )}
                    <div className="mt-2 h-[132px] overflow-y-auto pr-1">
                    {selectedProducts.length > 0 ? (
                      <div className="flex flex-col gap-2">
                        {selectedProducts.map((product) => (
                          <div
                            key={product.value}
                            className="flex items-center justify-between gap-2 rounded-lg border border-slate-200 bg-white p-2 transition-all hover:border-slate-300"
                          >
                            <div className="flex min-w-0 items-center gap-2.5">
                              <span className="rounded border border-slate-200 bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] font-bold tracking-wider text-slate-600 uppercase">
                                {product.sku}
                              </span>
                              <div className="flex min-w-0 flex-col">
                                <span className="truncate text-xs font-bold text-slate-700">
                                  {product.name}
                                </span>
                              </div>
                            </div>
                            <div className="flex shrink-0 items-center gap-2">
                              <div className="flex items-center gap-1">
                                <label className="text-[10px] font-semibold text-slate-400">
                                  Qtd
                                </label>
                                <Input
                                  type="number"
                                  min={1}
                                  value={product.quantity}
                                  onChange={(event) =>
                                    handleQuantityChange(
                                      product.value,
                                      event.target.value === ""
                                        ? ""
                                        : Number(event.target.value)
                                    )
                                  }
                                  className="h-7 w-16 border-slate-200 text-center text-xs focus:border-blue-500 focus:ring-blue-500"
                                />
                              </div>
                              <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                onClick={() => handleRemoveProduct(product.value)}
                                className="h-6 w-6 shrink-0 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                              >
                                <X size={14} />
                              </Button>
                            </div>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div className="flex h-full items-center justify-center rounded-lg border border-dashed border-slate-200 bg-slate-50/70 px-3 py-2.5 text-center text-[11px] font-medium text-slate-500">
                        Selecione produtos no catálogo acima para compor a carga.
                      </div>
                    )}
                    </div>

                    {itemsError ?? mappedFieldErrors.items ? (
                      <p className="mt-2 text-[11px] font-medium text-rose-600">
                        {itemsError ?? mappedFieldErrors.items}
                      </p>
                    ) : null}
                  </div>
                </div>
              </div>

              {/* Bloco 3: Malha Logística (Direita, Topo) */}
              <div className="flex min-h-0 flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] shrink-0 items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
                  <MapPinned size={16} className="text-emerald-600" />
                  <h2 className="text-xs font-bold tracking-wider text-slate-700 uppercase">
                    Itinerário Físico
                  </h2>
                </div>

                <div className="flex flex-col p-5">
                  <div className="relative space-y-4">
                    <div className="absolute top-8 bottom-5 left-3.5 w-px border-l-2 border-dashed border-slate-200" />

                    <div className="relative space-y-2 pl-8">
                      <span className="absolute top-2.5 left-1.5 h-4 w-4 rounded-full border-[3px] border-white bg-slate-800" />
                      <label className="text-xs font-bold text-slate-600">
                        Local de Coleta (Origem)
                      </label>
                      <Input
                        value={origin}
                        onChange={(event) => setOrigin(event.target.value)}
                        placeholder="Ex: Curitiba, PR"
                        className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                      {mappedFieldErrors.origin ? (
                        <p className="text-[11px] font-medium text-rose-600">
                          {mappedFieldErrors.origin}
                        </p>
                      ) : null}
                    </div>

                    <div className="relative space-y-2 pl-8">
                      <span className="absolute top-2.5 left-1.5 h-4 w-4 rounded-full border-[3px] border-white bg-emerald-500" />
                      <label className="text-xs font-bold text-slate-600">
                        Local de Entrega (Destino)
                      </label>
                      <Input
                        value={destination}
                        onChange={(event) => setDestination(event.target.value)}
                        placeholder="Ex: São Paulo, SP"
                        className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                      {mappedFieldErrors.destination ? (
                        <p className="text-[11px] font-medium text-rose-600">
                          {mappedFieldErrors.destination}
                        </p>
                      ) : null}
                    </div>
                  </div>

                  <p className="mt-4 text-[11px] font-medium text-slate-400">
                    Distância, tempo estimado e o veículo exigido são calculados
                    automaticamente a partir da origem, destino e produtos.
                  </p>
                </div>
              </div>

              {/* ========================================================
                  LINHA 2: PRAZOS (Esq) + META COMERCIAL (Dir)
                  ======================================================== */}

              {/* Bloco 2: Prazos (Esquerda, Baixo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] shrink-0 items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
                  <CalendarClock size={16} className="text-amber-600" />
                  <h2 className="text-xs font-bold tracking-wider text-slate-700 uppercase">
                    Prazos (SLA)
                  </h2>
                </div>

                <div className="flex flex-1 flex-col p-5">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <label className="text-xs font-bold text-slate-600">
                        Coleta Limite (Deadline)
                      </label>
                      <Input
                        type="datetime-local"
                        value={pickupDeadline}
                        onChange={(event) =>
                          setPickupDeadline(event.target.value)
                        }
                        className="h-10 border-slate-200 text-xs font-medium text-slate-700 focus:border-blue-500 focus:ring-blue-500"
                      />
                      {mappedFieldErrors.pickupDeadline ? (
                        <p className="text-[11px] font-medium text-rose-600">
                          {mappedFieldErrors.pickupDeadline}
                        </p>
                      ) : null}
                    </div>

                    <div className="space-y-2">
                      <label className="text-xs font-bold text-slate-600">
                        Entrega Limite (Deadline)
                      </label>
                      <Input
                        type="datetime-local"
                        value={deliveryDeadline}
                        onChange={(event) =>
                          setDeliveryDeadline(event.target.value)
                        }
                        className="h-10 border-slate-200 text-xs font-medium text-slate-700 focus:border-blue-500 focus:ring-blue-500"
                      />
                      {mappedFieldErrors.deliveryDeadline ? (
                        <p className="text-[11px] font-medium text-rose-600">
                          {mappedFieldErrors.deliveryDeadline}
                        </p>
                      ) : null}
                    </div>
                  </div>
                </div>
              </div>

              {/* Bloco 4: Balizamento Financeiro (Direita, Baixo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] shrink-0 items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
                  <DollarSign size={16} className="text-emerald-600" />
                  <h2 className="text-xs font-bold tracking-wider text-slate-700 uppercase">
                    Balizamento Financeiro
                  </h2>
                </div>

                <div className="flex flex-1 flex-col p-5">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <label className="flex items-center gap-1.5 text-xs font-bold text-slate-600">
                        <DollarSign size={13} className="text-emerald-500" />{" "}
                        Orçamento Teto (Valor Guia)
                      </label>
                      <div className="relative">
                        <span className="absolute top-2.5 left-3 text-sm font-bold text-slate-400">
                          R$
                        </span>
                        <Input
                          type="number"
                          min={0}
                          step="0.01"
                          value={budgetCeiling}
                          onChange={(event) =>
                            setBudgetCeiling(event.target.value)
                          }
                          placeholder="0,00"
                          className="h-10 border-slate-200 pl-9 text-sm font-semibold text-slate-800 focus:border-blue-500 focus:ring-blue-500"
                        />
                      </div>
                      {mappedFieldErrors.budgetCeiling ? (
                        <p className="text-[11px] font-medium text-rose-600">
                          {mappedFieldErrors.budgetCeiling}
                        </p>
                      ) : null}
                    </div>

                    <div className="space-y-2">
                      <label className="flex items-center gap-1.5 text-xs font-bold text-slate-600">
                        <DollarSign size={13} className="text-slate-400" />{" "}
                        Pedágio Estimado
                      </label>
                      <div className="relative">
                        <span className="absolute top-2.5 left-3 text-sm font-bold text-slate-400">
                          R$
                        </span>
                        <Input
                          type="number"
                          min={0}
                          step="0.01"
                          value={estimatedTollCost}
                          onChange={(event) =>
                            setEstimatedTollCost(event.target.value)
                          }
                          placeholder="0,00"
                          className="h-10 border-slate-200 pl-9 text-sm font-semibold text-slate-800 focus:border-blue-500 focus:ring-blue-500"
                        />
                      </div>
                      {mappedFieldErrors.estimatedTollCost ? (
                        <p className="text-[11px] font-medium text-rose-600">
                          {mappedFieldErrors.estimatedTollCost}
                        </p>
                      ) : null}
                    </div>
                  </div>

                  <p className="mt-auto border-t border-slate-100 pt-4 text-[10px] font-medium text-slate-400">
                    O piso legal (ANTT) será calculado depois.
                  </p>
                </div>
              </div>

              {/* Ações (Cancelar / Salvar) — abaixo do Balizamento, coluna direita */}
              <div className="flex items-center justify-end gap-3 md:col-start-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={pending}
                  className="h-10 border-slate-200 bg-white text-xs font-semibold text-slate-700"
                  onClick={() => navigate("/route-segment-management")}
                >
                  <X size={14} className="mr-1.5" /> Cancelar
                </Button>
                <Button
                  type="submit"
                  disabled={pending}
                  className="h-10 bg-blue-600 text-xs font-bold tracking-wide text-white hover:bg-blue-700"
                >
                  {pending ? (
                    <Loader2 size={14} className="mr-1.5 animate-spin" />
                  ) : (
                    <Save size={14} className="mr-1.5" />
                  )}
                  Salvar Trecho Operacional
                </Button>
              </div>
            </div>
          </div>
        </div>
      </form>
    </AppShell>
  )
}
