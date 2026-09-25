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
  Search,
} from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { FormAlert } from "@/components/auth/FormAlert"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useProductOptions } from "@/hooks/use-product-options"
import { useAsyncAction } from "@/hooks/use-async-action"
import { createRouteSegment } from "@/services/route-segment-service"
import { mapRouteSegmentFieldErrors } from "@/constants/route-segments"

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

  const { run, pending, error, fieldErrors } = useAsyncAction((form) =>
    createRouteSegment(form)
  )

  const mappedFieldErrors = useMemo(
    () => mapRouteSegmentFieldErrors(fieldErrors),
    [fieldErrors]
  )

  const filteredProductOptions = productOptions.filter((product) => {
    const isAlreadySelected = selectedProducts.some(
      (p) => p.value === product.value
    )
    if (isAlreadySelected) return false

    const search = productSearch.trim().toLowerCase()
    if (!search) return true

    return [product.sku, product.name, product.details]
      .filter(Boolean)
      .some((field) => field.toLowerCase().includes(search))
  })

  const handleAddProduct = (productId) => {
    const productToAdd = productOptions.find((p) => p.value === productId)
    if (productToAdd) {
      setSelectedProducts([...selectedProducts, { ...productToAdd, quantity: 1 }])
    }
    setProductSearch("")
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
        onSubmit={handleSubmit}
        className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-5xl flex-col overflow-hidden"
      >
        {/* HEADER LIMPO E TEXTUAL */}
        <div className="mb-5 flex shrink-0 items-center justify-between border-b border-slate-200 pt-1 pb-3">
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

          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="outline"
              disabled={pending}
              className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700"
              onClick={() => navigate("/route-segment-management")}
            >
              <X size={14} className="mr-1.5" /> Cancelar
            </Button>
            <Button
              type="submit"
              disabled={pending}
              className="h-9 bg-blue-600 text-xs font-bold tracking-wide text-white hover:bg-blue-700"
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

        {error ? (
          <div className="mb-4 shrink-0">
            <FormAlert message={error.message} />
          </div>
        ) : null}

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">
            <div className="grid min-h-full gap-6 md:grid-cols-2 md:grid-rows-[auto_1fr]">
              {/* ========================================================
                  LINHA 1: ESPECIFICAÇÕES (Esq) + ITINERÁRIO (Dir)
                  ======================================================== */}

              {/* Bloco 1: Identificação da Mercadoria (Esquerda, Topo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] shrink-0 items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
                  <PackageOpen size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold tracking-wider text-slate-700 uppercase">
                    Especificações da Carga
                  </h2>
                </div>

                <div className="flex flex-col space-y-4 p-5">
                  <div className="space-y-3">
                    <label className="text-xs font-bold text-slate-600">
                      Composição da Carga (Produtos)
                    </label>

                    {/* Select atua como botão/pesquisa para Adicionar */}
                    <Select value="" onValueChange={handleAddProduct}>
                      <SelectTrigger className="h-10 w-full border-slate-200 bg-slate-50 text-sm transition-colors hover:bg-slate-100 focus:border-blue-500 focus:ring-blue-500">
                        <SelectValue placeholder="Buscar e adicionar produto à carga..." />
                      </SelectTrigger>
                      <SelectContent>
                        <div className="sticky top-0 z-10 border-b border-slate-100 bg-white/95 px-2 py-2 backdrop-blur-sm">
                          <div className="relative">
                            <Search className="absolute top-2.5 left-3 h-4 w-4 text-slate-400" />
                            <Input
                              value={productSearch}
                              onChange={(event) =>
                                setProductSearch(event.target.value)
                              }
                              onKeyDown={(event) => event.stopPropagation()}
                              placeholder="Buscar por código ou nome..."
                              className="h-9 border-slate-200 bg-slate-50 pl-9 text-xs focus:border-blue-500 focus:ring-blue-500"
                            />
                          </div>
                        </div>

                        {filteredProductOptions.length === 0 ? (
                          <div className="px-3 py-3 text-xs text-slate-500">
                            Nenhum produto encontrado.
                          </div>
                        ) : (
                          filteredProductOptions.map((product) => (
                            <SelectItem key={product.id} value={product.value}>
                              <div className="flex min-w-0 flex-col gap-0.5 py-0.5">
                                <div className="flex items-center gap-2">
                                  <span className="rounded-md bg-slate-100 px-2 py-0.5 font-mono text-[10px] font-bold tracking-wider text-slate-500 uppercase">
                                    {product.sku}
                                  </span>
                                  <span className="truncate font-semibold text-slate-800">
                                    {product.name}
                                  </span>
                                </div>
                                <span className="pl-[3.4rem] text-[11px] text-slate-500">
                                  {product.details}
                                </span>
                              </div>
                            </SelectItem>
                          ))
                        )}
                      </SelectContent>
                    </Select>

                    {/* LISTA MÚLTIPLA: produtos adicionados, com quantidade */}
                    {selectedProducts.length > 0 && (
                      <div className="flex max-h-[160px] animate-in flex-col gap-2 overflow-y-auto pr-1 fade-in">
                        {selectedProducts.map((product) => (
                          <div
                            key={product.value}
                            className="flex items-center justify-between gap-2 rounded-lg border border-slate-200 bg-white p-2 shadow-sm transition-all hover:border-slate-300"
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
                    )}

                    {mappedFieldErrors.items ? (
                      <p className="text-[11px] font-medium text-rose-600">
                        {mappedFieldErrors.items}
                      </p>
                    ) : null}
                  </div>

                  <div className="mt-auto flex items-center justify-between rounded-lg border border-dashed border-slate-200 bg-slate-50/70 px-3 py-2.5">
                    <p className="text-[11px] font-medium text-slate-500">
                      Não encontrou o produto na lista?
                    </p>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => navigate("/register-product")}
                      className="h-8 border-slate-200 bg-white text-[11px] font-semibold text-slate-700 hover:bg-slate-100"
                    >
                      <Plus size={13} className="mr-1.5" /> Cadastrar produto
                    </Button>
                  </div>
                </div>
              </div>

              {/* Bloco 3: Malha Logística (Direita, Topo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
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
            </div>
          </div>
        </div>
      </form>
    </AppShell>
  )
}
