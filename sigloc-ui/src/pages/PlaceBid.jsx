import { ArrowLeft, Gavel, AlertTriangle, Check, Lock, Loader2, Scale, Box, Snowflake, Truck } from "lucide-react"
import { useNavigate, useParams } from "react-router-dom"
import { useCallback, useEffect, useState } from "react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useToast } from "@/components/ui/toast"
import { auctionService } from "@/services/auction-service"
import { ApiError } from "@/lib/api-client"

const numberFormat = new Intl.NumberFormat("pt-BR")
const formatWeight = (value) => `${numberFormat.format(Math.round(value ?? 0))} kg`
const formatVolume = (value) => `${numberFormat.format(Math.round(value ?? 0))} m³`
const formatCurrency = (value) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value ?? 0)
// Differences can be a few cents (R$ 2.850,01 against a R$ 3.000 total), which whole reais round to 0.
const formatDifference = (value) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    minimumFractionDigits: Number.isInteger(value) ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(value ?? 0)
const roundCents = (value) => Math.round(value * 100) / 100

const formatDateTime = (value) => {
  if (!value) return "—"
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return "—"
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" }).format(date)
}

/** Share of a vehicle's capacity the load would take, or null when the capacity is unknown. */
const capacityRatio = (required, capacity) => (capacity > 0 ? required / capacity : null)

/** Splits the API's "-15°C a -1°C (Congelado)" label into the range and the refrigeration level. */
const splitTemperature = (label) => {
  const match = label?.match(/^(.*?)\s*\((.+)\)$/)
  return match ? { value: match[1], hint: match[2] } : { value: label || "—", hint: null }
}

// Dividers between the requirement cells: 2×2 on small screens, one row from md up.
const REQUIREMENT_CELL_BORDERS = ["", "border-l", "border-t md:border-t-0 md:border-l", "border-t border-l md:border-t-0"]

function RequirementCell({ icon: Icon, label, value, hint, className = "" }) {
  return (
    <div className={`flex items-start gap-3 border-slate-100 p-4 ${className}`}>
      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
        <Icon size={16} />
      </div>
      <div className="min-w-0">
        <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{label}</p>
        <p className="text-sm font-bold text-slate-900">{value}</p>
        {hint && <p className="text-[10px] font-medium text-slate-500">{hint}</p>}
      </div>
    </div>
  )
}

function StepCard({ step, done, title, description, children }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4">
      <div className="mb-4 flex items-start gap-2.5">
        <div className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10px] font-black text-white ${done ? "bg-emerald-500" : "bg-blue-600"}`}>
          {done ? <Check size={12} strokeWidth={3} /> : step}
        </div>
        <div>
          <h3 className="text-xs font-bold text-slate-900">{title}</h3>
          <p className="text-[10px] text-slate-500">{description}</p>
        </div>
      </div>
      {children}
    </div>
  )
}

/** Places the bid total between the ANTT floor, the leader and the ceiling (all totals with toll). */
function PriceRuler({ bidTotal, floor, leader, ceiling }) {
  const values = [floor, ceiling, leader, bidTotal].filter((v) => v != null)
  const min = Math.min(...values)
  const max = Math.max(...values)
  const padding = (max - min || 1) * 0.08
  const start = min - padding
  const span = max + padding - start
  const at = (value) => `${Math.min(Math.max(((value - start) / span) * 100, 0), 100)}%`
  const overCeiling = bidTotal != null && bidTotal > ceiling

  return (
    <div className="pt-8">
      <div className="relative h-1.5 rounded-full bg-slate-100">
        <div className="absolute inset-y-0 rounded-full bg-blue-100" style={{ left: at(floor), right: `calc(100% - ${at(ceiling)})` }} />
        <span className="absolute top-1/2 h-3 w-0.5 -translate-x-1/2 -translate-y-1/2 rounded-full bg-slate-300" style={{ left: at(floor) }} />
        {leader != null && <span className="absolute top-1/2 h-3 w-0.5 -translate-x-1/2 -translate-y-1/2 rounded-full bg-emerald-500" style={{ left: at(leader) }} />}
        <span className="absolute top-1/2 h-3 w-0.5 -translate-x-1/2 -translate-y-1/2 rounded-full bg-slate-700" style={{ left: at(ceiling) }} />
        {bidTotal != null && (
          <div className="absolute top-1/2 -translate-x-1/2 -translate-y-1/2 transition-[left] duration-200" style={{ left: at(bidTotal) }}>
            <div className={`h-3.5 w-3.5 rounded-full border-2 border-white shadow ${overCeiling ? "bg-rose-500" : "bg-blue-600"}`} />
            <span className={`absolute bottom-full left-1/2 mb-1.5 -translate-x-1/2 whitespace-nowrap font-mono text-[10px] font-bold ${overCeiling ? "text-rose-600" : "text-blue-700"}`}>
              {formatCurrency(bidTotal)}
            </span>
          </div>
        )}
      </div>

      <div className="mt-3 flex justify-between text-[10px] text-slate-400">
        <span>Piso ANTT <span className="font-mono font-semibold text-slate-600">{formatCurrency(floor)}</span></span>
        <span>Líder <span className="font-mono font-semibold text-emerald-600">{leader != null ? formatCurrency(leader) : "—"}</span></span>
        <span>Teto <span className="font-mono font-semibold text-slate-600">{formatCurrency(ceiling)}</span></span>
      </div>
    </div>
  )
}

function UsageBar({ label, required, capacity, format }) {
  const ratio = capacityRatio(required, capacity)
  const exceeds = ratio != null && ratio > 1
  return (
    <div title={capacity > 0 ? `${format(required)} de ${format(capacity)}` : "Capacidade não informada"}>
      <div className="mb-1 flex justify-between text-[10px]">
        <span className="text-slate-400">{label}</span>
        <span className={`font-mono font-semibold ${exceeds ? "text-rose-600" : "text-slate-600"}`}>{ratio != null ? `${Math.round(ratio * 100)}%` : "—"}</span>
      </div>
      <div className="h-1 overflow-hidden rounded-full bg-slate-100">
        {ratio != null && <div className={`h-full rounded-full ${exceeds ? "bg-rose-500" : "bg-slate-400"}`} style={{ width: `${Math.min(ratio, 1) * 100}%` }} />}
      </div>
    </div>
  )
}

function SummaryRow({ label, value }) {
  return (
    <div className="flex items-center justify-between text-xs">
      <span className="text-slate-500">{label}</span>
      <span className="font-mono font-semibold text-slate-800">{value}</span>
    </div>
  )
}

/** Where the bid total stands against the ceiling and the current leader. */
function getBidPosition({ hasValue, bidTotal, ceiling, leader }) {
  if (!hasValue) return { tone: "slate", text: "Informe o valor do frete para ver sua posição." }
  if (bidTotal > ceiling) return { tone: "rose", text: `${formatDifference(roundCents(bidTotal - ceiling))} acima do teto do leilão.` }
  if (leader == null) return { tone: "emerald", text: "Nenhum lance ainda: você abre a disputa." }
  const diff = roundCents(bidTotal - leader)
  if (diff < 0) return { tone: "emerald", text: `${formatDifference(-diff)} abaixo do líder: você assume a liderança.` }
  if (diff === 0) return { tone: "amber", text: "Mesmo valor do líder." }
  return { tone: "amber", text: `${formatDifference(diff)} acima do líder.` }
}

const POSITION_STYLES = {
  slate: "text-slate-400",
  rose: "text-rose-600",
  emerald: "text-emerald-600",
  amber: "text-amber-600",
}

export default function PlaceBid() {
  const navigate = useNavigate()
  const { auctionId } = useParams()
  const toast = useToast()

  const [analysis, setAnalysis] = useState(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(null)

  const [bidValue, setBidValue] = useState("")
  const [selectedVehicleId, setSelectedVehicleId] = useState(null)
  const [submitting, setSubmitting] = useState(false)

  const routeDetailsPath = `/bid-analysis/${auctionId}`

  const loadAnalysis = useCallback(
    async (signal) => {
      setLoading(true)
      setLoadError(null)
      try {
        const data = await auctionService.getCarrierAnalysis(auctionId, { signal })
        if (signal?.aborted) return
        setAnalysis(data)
        // Pre-fill with the carrier's own active bid when present, otherwise the highest freight that
        // fits the ceiling (the ceiling is a total, so the toll comes off it).
        const ceilingFreight = Math.max((data?.competition?.auctionCeiling ?? 0) - (data?.competition?.estimatedToll ?? 0), 0)
        setBidValue(data?.myBid?.netFreightValue ?? (data?.competition ? roundCents(ceilingFreight) : ""))
        setSelectedVehicleId(data?.myBid?.vehicleId ?? null)
      } catch (error) {
        if (error?.name === "AbortError") return
        setLoadError(error instanceof ApiError ? error.message : "Não foi possível carregar a análise da rota.")
      } finally {
        if (!signal?.aborted) setLoading(false)
      }
    },
    [auctionId]
  )

  useEffect(() => {
    const controller = new AbortController()
    loadAnalysis(controller.signal)
    return () => controller.abort()
  }, [loadAnalysis])

  const ceiling = analysis?.competition?.auctionCeiling ?? 0
  const bestLeaderOffer = analysis?.competition?.bestLeaderOffer ?? null
  // The leader offer and the ceiling are totals (freight + toll); the input is freight only.
  const estimatedToll = analysis?.competition?.estimatedToll ?? 0
  // The route's estimated floor, the same figure the contractor sees, read as a total like the
  // ceiling and never above it (it is still a placeholder equal to the ceiling on most routes).
  const serverFloor = analysis?.competition?.estimatedAnttFloor
  const anttFloorTotal = Math.min(serverFloor > 0 ? serverFloor : ceiling, ceiling)

  const applyLeaderBid = () => {
    if (bestLeaderOffer != null) setBidValue(Math.max(bestLeaderOffer - estimatedToll - 50, 0))
  }
  const applyFloorBid = () => setBidValue(Math.max(roundCents(anttFloorTotal - estimatedToll), 0))

  const selectedVehicle = (analysis?.carrierAvailableFleet ?? []).find((v) => v.vehicleId === selectedVehicleId) ?? null

  const handleSubmit = async () => {
    const value = Number(bidValue)
    if (!selectedVehicleId) {
      toast.error("Selecione um veículo", "Escolha um veículo da sua frota para prosseguir com o lance.")
      return
    }
    if (!Number.isFinite(value) || value <= 0) {
      toast.error("Valor inválido", "Informe um valor de proposta maior que zero.")
      return
    }

    const isUpdate = Boolean(analysis?.myBid)
    setSubmitting(true)
    try {
      const result = await auctionService.placeBid(auctionId, { valorOferecido: value, veiculoId: selectedVehicleId })
      toast.success(
        isUpdate ? "Lance atualizado com sucesso!" : "Lance enviado com sucesso!",
        `Valor total ${formatCurrency(result?.totalValue)} (frete ${formatCurrency(result?.netFreightValue)} + pedágio ${formatCurrency(result?.tollValue)}).`
      )
      navigate(routeDetailsPath)
    } catch (error) {
      // Constraint Engine rejections (400) carry the exact motive in error.message.
      const message = error instanceof ApiError ? error.message : "Não foi possível registrar o lance. Tente novamente."
      toast.error("Lance recusado", message)
      setSubmitting(false)
    }
  }

  if (loading) {
    return (
      <AppShell title="Lance">
        <div className="flex h-[calc(100vh-8.5rem)] items-center justify-center text-slate-500">
          <Loader2 className="mr-2 h-5 w-5 animate-spin" /> Carregando dados do leilão...
        </div>
      </AppShell>
    )
  }

  if (loadError || !analysis) {
    return (
      <AppShell title="Lance">
        <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-md flex-col items-center justify-center gap-3 text-center">
          <AlertTriangle className="h-8 w-8 text-rose-500" />
          <p className="text-sm font-semibold text-slate-700">{loadError ?? "Rota não encontrada."}</p>
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate(routeDetailsPath)}>Voltar</Button>
            <Button onClick={() => loadAnalysis()}>Tentar novamente</Button>
          </div>
        </div>
      </AppShell>
    )
  }

  const { routeSummary, physicalRequirements, carrierAvailableFleet, myBid } = analysis
  const hasValue = Number(bidValue) > 0
  const bidTotal = Number(bidValue) + estimatedToll
  const position = getBidPosition({ hasValue, bidTotal, ceiling, leader: bestLeaderOffer })
  const temperature = splitTemperature(physicalRequirements.requiredTemperature)
  const requirements = [
    { icon: Scale, label: "Peso", value: formatWeight(physicalRequirements.consolidatedWeightKg) },
    { icon: Box, label: "Volume", value: formatVolume(physicalRequirements.volumeM3) },
    { icon: Snowflake, label: "Temperatura", value: temperature.value, hint: temperature.hint },
    { icon: Truck, label: "Equipamento", value: physicalRequirements.recommendedFleet || "—" },
  ]
  const restrictions = physicalRequirements.handlingRestrictions

  return (
    <AppShell title={myBid ? "Atualizar Lance" : "Dar Lance"}>
      <div className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden">

        {/* HEADER LIMPO */}
        <div className="flex shrink-0 items-center justify-between border-b border-slate-200 pb-2 pt-1 mb-4">
          <Button variant="ghost" className="h-auto p-0 text-xs font-medium text-slate-500 hover:bg-transparent hover:text-slate-900" onClick={() => navigate(routeDetailsPath)}>
            <ArrowLeft size={14} className="mr-2" /> Voltar para os detalhes da rota
          </Button>
        </div>

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6 space-y-4">

            {/* CABEÇALHO */}
            <div>
              <h1 className="text-xl font-black text-slate-900 flex items-center gap-2">
                <Gavel size={18} className="text-blue-600" /> {myBid ? "Atualizar Lance" : "Dar Lance"}
              </h1>
              <p className="mt-0.5 text-xs text-slate-500">
                <span className="font-mono font-bold">{routeSummary.referenceCode}</span> · {routeSummary.shortItinerary}
              </p>
            </div>

            {/* EXIGÊNCIAS DA CARGA */}
            <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
              <div className="border-b border-slate-100 px-4 py-2.5">
                <h2 className="text-[11px] font-bold uppercase tracking-wider text-slate-700">Exigências da Carga</h2>
              </div>
              <div className="grid grid-cols-2 md:grid-cols-4">
                {requirements.map((requirement, index) => (
                  <RequirementCell key={requirement.label} {...requirement} className={REQUIREMENT_CELL_BORDERS[index]} />
                ))}
              </div>
              <div className={`flex flex-wrap items-center gap-2 border-t px-4 py-2.5 ${restrictions.length > 0 ? "border-rose-100 bg-rose-50/50" : "border-slate-100"}`}>
                {restrictions.length > 0 ? (
                  <>
                    <span className="flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider text-rose-600">
                      <AlertTriangle size={12} /> Manuseio
                    </span>
                    {restrictions.map((restriction) => (
                      <Badge key={restriction} variant="outline" className="border-rose-200 bg-white text-rose-600 text-[10px] font-semibold">
                        {restriction}
                      </Badge>
                    ))}
                  </>
                ) : (
                  <span className="text-[10px] text-slate-400">Sem restrições especiais de manuseio</span>
                )}
              </div>
            </div>

            <div className="grid grid-cols-1 gap-4 lg:grid-cols-12 lg:items-start">
              <div className="space-y-4 lg:col-span-8">

                {/* 1. VALOR DO FRETE */}
                <StepCard
                  step={1}
                  done={hasValue}
                  title="Valor do Frete"
                  description={`O pedágio estimado de ${formatCurrency(estimatedToll)} é somado ao total.`}
                >
                  <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                    <div className="flex-1 bg-white border border-slate-300 rounded-lg p-1 focus-within:border-blue-500 focus-within:ring-1 focus-within:ring-blue-500 transition-all">
                      <div className="flex items-center px-2">
                        <span className="text-xs font-bold text-slate-400">R$</span>
                        <Input
                          type="number"
                          value={bidValue}
                          onChange={(e) => setBidValue(e.target.value)}
                          className="border-0 shadow-none focus-visible:ring-0 text-xl font-black font-mono text-slate-800 h-10"
                        />
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-2 sm:flex sm:flex-col">
                      <Button variant="outline" size="sm" onClick={applyLeaderBid} disabled={bestLeaderOffer == null} className="text-[10px] font-bold border-slate-200 bg-white text-slate-600 h-7">
                        Cobrir Líder
                      </Button>
                      <Button variant="outline" size="sm" onClick={applyFloorBid} className="text-[10px] font-bold border-slate-200 bg-white text-slate-600 h-7">
                        Piso ANTT
                      </Button>
                    </div>
                  </div>

                  <PriceRuler
                    bidTotal={hasValue ? bidTotal : null}
                    floor={anttFloorTotal}
                    leader={bestLeaderOffer}
                    ceiling={ceiling}
                  />
                </StepCard>

                {/* 2. SELETOR DE VEÍCULO */}
                <StepCard
                  step={2}
                  done={Boolean(selectedVehicle)}
                  title="Veículo"
                  description="Compare a capacidade com as exigências da carga. A compatibilidade é auditada ao confirmar o lance."
                >
                  {carrierAvailableFleet.length === 0 ? (
                    <div className="rounded-lg border border-dashed border-slate-200 p-4 text-center">
                      <Lock size={16} className="text-slate-400 mx-auto mb-1" />
                      <p className="text-[10px] font-semibold text-slate-500">Nenhum veículo cadastrado na sua frota</p>
                    </div>
                  ) : (
                    <div className="max-h-[260px] space-y-2 overflow-y-auto pr-1">
                      {carrierAvailableFleet.map((vehicle) => {
                        const isSelected = selectedVehicleId === vehicle.vehicleId
                        const details = [vehicle.model, ...vehicle.specifications].filter(Boolean).join(" · ")
                        return (
                          <button
                            key={vehicle.vehicleId}
                            type="button"
                            onClick={() => setSelectedVehicleId(vehicle.vehicleId)}
                            className={`flex w-full flex-col gap-3 rounded-lg border p-3 text-left transition-colors sm:flex-row sm:items-center sm:gap-6 ${
                              isSelected ? "border-blue-500 bg-blue-50/50" : "border-slate-200 bg-white hover:border-slate-300"
                            }`}
                          >
                            <div className="flex min-w-0 items-center gap-3 sm:w-56 sm:shrink-0">
                              <span className={`flex h-4 w-4 shrink-0 items-center justify-center rounded-full border-2 ${isSelected ? "border-blue-600" : "border-slate-300"}`}>
                                {isSelected && <span className="h-2 w-2 rounded-full bg-blue-600" />}
                              </span>
                              <div className="min-w-0">
                                <p className="font-mono text-sm font-black text-slate-900">{vehicle.plate}</p>
                                <p className="truncate text-[10px] text-slate-500">{details}</p>
                              </div>
                            </div>

                            <div className="grid flex-1 grid-cols-2 gap-4 pl-7 sm:pl-0">
                              <UsageBar label="Peso" required={physicalRequirements.consolidatedWeightKg} capacity={vehicle.capacityWeightKg} format={formatWeight} />
                              <UsageBar label="Volume" required={physicalRequirements.volumeM3} capacity={vehicle.capacityVolumeM3} format={formatVolume} />
                            </div>
                          </button>
                        )
                      })}
                    </div>
                  )}
                </StepCard>
              </div>

              {/* RESUMO DO LANCE */}
              <aside className="rounded-xl border border-slate-200 bg-white lg:col-span-4 lg:sticky lg:top-0">
                <div className="border-b border-slate-100 px-4 py-3">
                  <h2 className="text-[11px] font-bold uppercase tracking-wider text-slate-700">Resumo do Lance</h2>
                </div>

                <div className="space-y-4 p-4">
                  <div className="space-y-1.5">
                    <SummaryRow label="Coleta" value={formatDateTime(routeSummary.sla.firstPickup)} />
                    <SummaryRow label="Entrega" value={formatDateTime(routeSummary.sla.lastDelivery)} />
                  </div>

                  <div className="space-y-1.5 border-t border-slate-100 pt-4">
                    {myBid && <SummaryRow label="Lance atual" value={formatCurrency(myBid.totalValue)} />}
                    <SummaryRow label="Frete" value={hasValue ? formatCurrency(bidValue) : "—"} />
                    <SummaryRow label="Pedágio estimado" value={`+ ${formatCurrency(estimatedToll)}`} />
                    <div className="flex items-center justify-between pt-1">
                      <span className="text-xs font-bold text-slate-900">Total</span>
                      <span className={`font-mono text-lg font-black ${bidTotal > ceiling ? "text-rose-600" : "text-slate-900"}`}>
                        {hasValue ? formatCurrency(bidTotal) : "—"}
                      </span>
                    </div>
                    <p className={`text-right text-[10px] font-semibold ${POSITION_STYLES[position.tone]}`}>{position.text}</p>
                  </div>

                  <div className="border-t border-slate-100 pt-4">
                    <SummaryRow
                      label="Veículo"
                      value={selectedVehicle ? selectedVehicle.plate : <span className="font-sans text-[10px] text-amber-600">Selecione um veículo</span>}
                    />
                  </div>
                </div>

                <div className="space-y-1 border-t border-slate-100 p-4">
                  <Button
                    disabled={!selectedVehicleId || submitting}
                    onClick={handleSubmit}
                    className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold h-9 text-xs disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {submitting ? (
                      <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> {myBid ? "Atualizando lance..." : "Enviando lance..."}</>
                    ) : myBid ? (
                      "Atualizar Lance"
                    ) : (
                      "Confirmar Lance"
                    )}
                  </Button>
                  <Button variant="ghost" disabled={submitting} onClick={() => navigate(routeDetailsPath)} className="w-full h-8 text-xs font-medium text-slate-500">
                    Cancelar
                  </Button>
                </div>
              </aside>
            </div>

          </div>
        </div>
      </div>
    </AppShell>
  )
}
