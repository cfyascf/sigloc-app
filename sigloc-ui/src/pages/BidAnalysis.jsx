import { ArrowLeft, MapPinned, Truck, Scale, Box, Gavel, Layers, Activity, CalendarClock, PackageOpen, Snowflake, AlertTriangle, CheckCircle2, Lock, Loader2 } from "lucide-react"
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

const formatDateTime = (value) => {
  if (!value) return "—"
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return "—"
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" }).format(date)
}

function getStopBadgeClass(index, totalStops) {
  if (index === 0) return "bg-slate-800"
  if (index === totalStops - 1) return "bg-emerald-500"
  return "bg-blue-500"
}

export default function BidAnalysis() {
  const navigate = useNavigate()
  const { auctionId } = useParams()
  const toast = useToast()

  const [analysis, setAnalysis] = useState(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(null)

  const [bidValue, setBidValue] = useState("")
  const [selectedVehicleId, setSelectedVehicleId] = useState(null)
  const [submitting, setSubmitting] = useState(false)

  const loadAnalysis = useCallback(
    async (signal) => {
      setLoading(true)
      setLoadError(null)
      try {
        const data = await auctionService.getCarrierAnalysis(auctionId, { signal })
        if (signal?.aborted) return
        setAnalysis(data)
        // Pre-fill with the carrier's own active bid when present, otherwise the ceiling.
        setBidValue(data?.myBid?.netFreightValue ?? data?.competition?.auctionCeiling ?? "")
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
  const anttFloor = ceiling * 0.75

  const applyLeaderBid = () => {
    if (bestLeaderOffer != null) setBidValue(Math.max(bestLeaderOffer - 50, 0))
  }
  const applyFloorBid = () => setBidValue(Math.round(anttFloor))

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
      await loadAnalysis()
    } catch (error) {
      // Constraint Engine rejections (400) carry the exact motive in error.message.
      const message = error instanceof ApiError ? error.message : "Não foi possível registrar o lance. Tente novamente."
      toast.error("Lance recusado", message)
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return (
      <AppShell title="Detalhes da Rota">
        <div className="flex h-[calc(100vh-8.5rem)] items-center justify-center text-slate-500">
          <Loader2 className="mr-2 h-5 w-5 animate-spin" /> Carregando análise da rota...
        </div>
      </AppShell>
    )
  }

  if (loadError || !analysis) {
    return (
      <AppShell title="Detalhes da Rota">
        <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-md flex-col items-center justify-center gap-3 text-center">
          <AlertTriangle className="h-8 w-8 text-rose-500" />
          <p className="text-sm font-semibold text-slate-700">{loadError ?? "Rota não encontrada."}</p>
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate(-1)}>Voltar</Button>
            <Button onClick={() => loadAnalysis()}>Tentar novamente</Button>
          </div>
        </div>
      </AppShell>
    )
  }

  const { routeSummary, competition, physicalRequirements, carrierAvailableFleet, travelPlan, myBid } = analysis
  const distanceToLeader = bestLeaderOffer != null ? Number(bidValue) - bestLeaderOffer : null

  return (
    <AppShell title="Detalhes da Rota">
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-5xl flex-col overflow-hidden">

        {/* HEADER LIMPO */}
        <div className="flex shrink-0 items-center justify-between border-b border-slate-200 pb-2 pt-1 mb-4">
          <Button variant="ghost" className="h-auto p-0 text-xs font-medium text-slate-500 hover:bg-transparent hover:text-slate-900" onClick={() => navigate(-1)}>
            <ArrowLeft size={14} className="mr-2" /> Voltar para a mesa de leilões
          </Button>
        </div>

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6 space-y-4">

            {/* CABEÇALHO E SLA CRÍTICO */}
            <div className="flex flex-col gap-4">
              <div>
                <div className="flex items-center gap-2 mb-1.5">
                  <Layers size={14} className="text-blue-500" />
                  <span className="font-mono text-xs font-bold text-slate-500">{routeSummary.referenceCode}</span>
                  <Badge className="bg-slate-800 text-white text-[9px] uppercase font-bold border-none hover:bg-slate-800">Em Leilão</Badge>
                </div>
                <h1 className="text-xl font-black text-slate-900">{routeSummary.shortItinerary}</h1>
              </div>

              {/* BANNER DE SLA */}
              <div className="flex flex-col md:flex-row border border-slate-200 rounded-xl overflow-hidden">
                <div className="flex-1 bg-amber-50 p-3 border-b md:border-b-0 md:border-r border-slate-200 flex items-center gap-3">
                  <div className="bg-amber-100 p-1.5 rounded-lg text-amber-700">
                    <CalendarClock size={16} />
                  </div>
                  <div>
                    <p className="text-[9px] font-bold uppercase text-amber-700/70 tracking-wider">Primeira Coleta SLA</p>
                    <p className="font-black text-xs text-amber-900">{formatDateTime(routeSummary.sla.firstPickup)}</p>
                  </div>
                </div>
                <div className="flex-1 bg-emerald-50 p-3 flex items-center gap-3">
                  <div className="bg-emerald-100 p-1.5 rounded-lg text-emerald-700">
                    <MapPinned size={16} />
                  </div>
                  <div>
                    <p className="text-[9px] font-bold uppercase text-emerald-700/70 tracking-wider">Última Entrega SLA</p>
                    <p className="font-black text-xs text-emerald-900">{formatDateTime(routeSummary.sla.lastDelivery)}</p>
                  </div>
                </div>
              </div>
            </div>

            {/* PAINEL DE AÇÃO (LANCE) E ESTRATÉGIA */}
            <div className="grid grid-cols-1 gap-4">

              {/* 1. LANCE E SELEÇÃO DE VEÍCULO */}
              <div className="rounded-xl border border-blue-200 bg-blue-50/30 p-4 flex flex-col">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="font-bold text-slate-900 flex items-center gap-2 text-xs">
                    <Gavel size={14} className="text-blue-600" /> Formulário de Lance
                  </h3>
                  <div className="flex items-center gap-1.5">
                    {myBid && (
                      <Badge className="bg-blue-600 text-white text-[9px] font-bold border-none uppercase hover:bg-blue-600">Lance Ativo</Badge>
                    )}
                    <Badge variant="outline" className="border-blue-200 bg-white text-blue-700 text-[9px]">Teto: {formatCurrency(ceiling)}</Badge>
                  </div>
                </div>

                {myBid && (
                  <div className="mb-3 flex items-center justify-between rounded-lg border border-blue-200 bg-blue-50 px-3 py-2">
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-blue-600" />
                      <div>
                        <p className="text-[9px] font-bold uppercase tracking-wider text-blue-700/70">Seu lance atual</p>
                        <p className="font-mono text-sm font-black text-blue-800">{formatCurrency(myBid.netFreightValue)}</p>
                      </div>
                    </div>
                    <p className="text-[9px] font-medium text-blue-700/70">Enviado em {formatDateTime(myBid.submittedAt)}</p>
                  </div>
                )}

                <div className="space-y-4">
                  {/* Input de Valor */}
                  <div>
                    <p className="text-[10px] font-bold uppercase tracking-wider text-slate-600 mb-1.5">{myBid ? "Atualizar Valor da Proposta" : "Valor da Proposta"}</p>
                    <div className="bg-white border border-slate-300 rounded-lg p-1 focus-within:border-blue-500 focus-within:ring-1 focus-within:ring-blue-500 transition-all">
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
                  </div>

                  {/* Botões de Ação Rápida */}
                  <div className="grid grid-cols-2 gap-2">
                    <Button variant="outline" size="sm" onClick={applyLeaderBid} disabled={bestLeaderOffer == null} className="text-[10px] font-bold border-slate-200 bg-white text-slate-600 h-7">
                      Cobrir Líder
                    </Button>
                    <Button variant="outline" size="sm" onClick={applyFloorBid} className="text-[10px] font-bold border-slate-200 bg-white text-slate-600 h-7">
                      Piso ANTT
                    </Button>
                  </div>

                  {/* SELETOR DE VEÍCULO */}
                  <div className="border-t border-slate-200/50 pt-4">
                    <p className="text-[10px] font-bold uppercase tracking-wider text-slate-600 mb-2 flex items-center gap-1.5">
                      <Truck size={12} className="text-blue-600" /> Veículo Alocado para Esta Rota
                    </p>
                    <p className="text-[9px] text-slate-500 mb-3">
                      Toda a frota é exibida. A compatibilidade (peso, volume, equipamento e agenda) é auditada ao confirmar o lance.
                    </p>

                    <div className="space-y-2 max-h-[240px] overflow-y-auto pr-1">
                      {carrierAvailableFleet.length === 0 ? (
                        <div className="bg-rose-50 border border-rose-200 rounded-lg p-3 text-center">
                          <Lock size={16} className="text-rose-500 mx-auto mb-1" />
                          <p className="text-[10px] font-bold text-rose-700">Nenhum veículo cadastrado na sua frota</p>
                        </div>
                      ) : (
                        carrierAvailableFleet.map((vehicle) => {
                          const isSelected = selectedVehicleId === vehicle.vehicleId
                          return (
                            <button
                              key={vehicle.vehicleId}
                              type="button"
                              onClick={() => setSelectedVehicleId(vehicle.vehicleId)}
                              className={`w-full text-left border rounded-lg p-3 transition-all ${
                                isSelected
                                  ? "border-blue-500 bg-blue-50 ring-1 ring-blue-500"
                                  : "border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50"
                              }`}
                            >
                              <div className="flex items-start justify-between mb-2">
                                <div className="flex items-center gap-2">
                                  <div className={`h-7 w-7 rounded-lg flex items-center justify-center ${isSelected ? "bg-blue-600" : "bg-slate-100"}`}>
                                    {isSelected ? <CheckCircle2 size={16} className="text-white" /> : <Truck size={14} className="text-slate-400" />}
                                  </div>
                                  <div>
                                    <p className="font-mono text-sm font-black text-slate-900">{vehicle.plate}</p>
                                    <p className="text-[9px] font-semibold text-slate-500">{vehicle.model}</p>
                                  </div>
                                </div>
                                <span className="font-mono text-[10px] font-bold text-slate-600">{formatWeight(vehicle.capacityWeightKg)}</span>
                              </div>

                              {vehicle.specifications.length > 0 && (
                                <div className="flex flex-wrap gap-1">
                                  {vehicle.specifications.map((spec) => (
                                    <Badge key={spec} variant="outline" className="border-slate-200 bg-slate-50 text-slate-600 text-[8px] font-semibold">
                                      {spec}
                                    </Badge>
                                  ))}
                                </div>
                              )}
                            </button>
                          )
                        })
                      )}
                    </div>

                    {!selectedVehicleId && carrierAvailableFleet.length > 0 && (
                      <p className="text-[9px] font-semibold text-amber-600 mt-2 flex items-center gap-1">
                        <AlertTriangle size={10} /> Selecione um veículo para prosseguir
                      </p>
                    )}
                  </div>

                  <Button
                    disabled={!selectedVehicleId || submitting}
                    onClick={handleSubmit}
                    className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold h-9 text-xs disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {submitting ? (
                      <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> {myBid ? "Atualizando lance..." : "Enviando lance..."}</>
                    ) : selectedVehicle ? (
                      `${myBid ? "Atualizar" : "Confirmar"} Lance com ${selectedVehicle.plate}`
                    ) : (
                      "Selecione um Veículo para Confirmar"
                    )}
                  </Button>
                </div>
              </div>

              {/* 2. ESTRATÉGIA E RANKING */}
              <div className="rounded-xl border border-slate-200 bg-white p-4 flex flex-col justify-between">
                <h3 className="font-bold text-slate-900 mb-3 flex items-center gap-2 text-xs">
                  <Activity size={14} className="text-slate-500" /> Mercado & Concorrência
                </h3>

                <div className="grid grid-cols-2 gap-3 mb-3">
                  <div className="bg-slate-50 p-3 rounded-lg border border-slate-100 flex flex-col items-center justify-center">
                    <p className="text-[9px] font-bold text-slate-400 uppercase mb-0.5">Teto do Leilão</p>
                    <p className="text-xl font-black text-slate-800">{formatCurrency(ceiling)}</p>
                  </div>
                  <div className="bg-slate-50 p-3 rounded-lg border border-slate-100 flex flex-col items-center justify-center">
                    <p className="text-[9px] font-bold text-slate-400 uppercase mb-0.5">Lances Ativos</p>
                    <p className="text-xl font-black text-blue-600">{competition.activeBids}</p>
                  </div>
                </div>

                <div className="flex items-center justify-between border-t border-slate-100 pt-3">
                  <div>
                    <p className="text-[9px] font-bold text-slate-400 uppercase">Melhor Oferta (Líder)</p>
                    <p className="text-xs font-black font-mono text-emerald-600">
                      {bestLeaderOffer != null ? formatCurrency(bestLeaderOffer) : "Sem lances"}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-[9px] font-bold text-slate-400 uppercase">Distância P/ Líder</p>
                    <p className={`text-xs font-bold font-mono ${distanceToLeader != null && distanceToLeader > 0 ? "text-rose-500" : "text-emerald-600"}`}>
                      {distanceToLeader != null ? formatCurrency(distanceToLeader) : "—"}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* PAINEL TÉCNICO: PRODUTO E EQUIPAMENTO */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">

              {/* Informações Críticas do Produto */}
              <div className="rounded-xl border border-slate-200 bg-white p-4 space-y-3">
                <div className="flex items-center gap-2 border-b border-slate-100 pb-2.5">
                  <PackageOpen size={14} className="text-blue-600" />
                  <h2 className="text-[11px] font-bold uppercase tracking-wider text-slate-700">Manuseio & Restrições</h2>
                </div>

                <div className="space-y-3">
                  <div>
                    <p className="text-[9px] font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1"><Snowflake size={10} /> Temperatura Exigida</p>
                    <p className="mt-0.5 text-xs font-bold text-blue-600">{physicalRequirements.requiredTemperature}</p>
                  </div>
                  <div>
                    <p className="text-[9px] font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1"><AlertTriangle size={10} /> Restrições de Manuseio</p>
                    {physicalRequirements.handlingRestrictions.length > 0 ? (
                      <div className="mt-1 flex flex-wrap gap-1.5">
                        {physicalRequirements.handlingRestrictions.map((r) => (
                          <Badge key={r} variant="outline" className="border-rose-200 bg-rose-50 text-rose-600 text-[9px] font-semibold">
                            {r}
                          </Badge>
                        ))}
                      </div>
                    ) : (
                      <p className="mt-0.5 text-xs font-bold text-slate-500">Sem restrições especiais</p>
                    )}
                  </div>
                </div>
              </div>

              {/* Capacidade e Veículo */}
              <div className="rounded-xl border border-slate-200 bg-white p-4 space-y-3 flex flex-col">
                <div className="flex items-center gap-2 border-b border-slate-100 pb-2.5">
                  <Truck size={14} className="text-blue-600" />
                  <h2 className="text-[11px] font-bold uppercase tracking-wider text-slate-700">Exigência de Equipamento</h2>
                </div>
                <div>
                  <p className="text-[9px] font-bold uppercase tracking-wider text-slate-400">Frota Recomendada</p>
                  <p className="mt-0.5 text-xs font-bold text-slate-800">{physicalRequirements.recommendedFleet}</p>
                </div>
                <div className="grid grid-cols-2 gap-3 border-t border-slate-100 pt-3 mt-auto">
                  <div>
                    <p className="text-[9px] font-bold uppercase tracking-wider text-slate-400">Peso Consolidado</p>
                    <p className="mt-0.5 font-mono text-xs font-black text-slate-800 flex items-center gap-1"><Scale size={12} className="text-slate-400" /> {formatWeight(physicalRequirements.consolidatedWeightKg)}</p>
                  </div>
                  <div>
                    <p className="text-[9px] font-bold uppercase tracking-wider text-slate-400">Cubagem (Volume)</p>
                    <p className="mt-0.5 font-mono text-xs font-black text-slate-800 flex items-center gap-1"><Box size={12} className="text-slate-400" /> {formatVolume(physicalRequirements.volumeM3)}</p>
                  </div>
                </div>
              </div>
            </div>

            {/* ITINERÁRIO */}
            <div className="rounded-xl border border-slate-200 bg-white">
              <div className="flex items-center gap-2 border-b border-slate-100 px-4 py-3">
                <MapPinned size={14} className="text-slate-600" />
                <h2 className="text-[11px] font-bold uppercase tracking-wider text-slate-700">Plano de Viagem (Milking Run)</h2>
              </div>
              <div className="p-4 grid grid-cols-1 md:grid-cols-4 gap-3">
                {travelPlan.map((stop, index) => (
                  <div key={`${stop.order}-${stop.city}`} className="flex md:flex-col gap-3 md:gap-0 items-center text-center">
                    <div className={`h-5 w-5 rounded-full flex items-center justify-center text-[9px] font-black text-white ${getStopBadgeClass(index, travelPlan.length)}`}>
                      {stop.order}
                    </div>
                    <div className="md:mt-2 text-left md:text-center">
                      <p className="text-xs font-bold text-slate-900">{stop.city}</p>
                      <p className="text-[9px] font-medium text-slate-500 mt-0.5">{stop.action}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

          </div>
        </div>
      </div>
    </AppShell>
  )
}
