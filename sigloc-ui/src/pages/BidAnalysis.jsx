import { ArrowLeft, MapPinned, Truck, Scale, Box, Gavel, Layers, Activity, CalendarClock, PackageOpen, Snowflake, AlertTriangle, Loader2 } from "lucide-react"
import { useNavigate, useParams } from "react-router-dom"
import { useCallback, useEffect, useState } from "react"

import AppShell from "@/components/app-shell"
import TravelPlan from "@/components/travel-plan"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { auctionService } from "@/services/auction-service"
import { ApiError } from "@/lib/api-client"
import { shortId } from "@/lib/utils"

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

export default function BidAnalysis() {
  const navigate = useNavigate()
  const { auctionId } = useParams()

  const [analysis, setAnalysis] = useState(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(null)

  const loadAnalysis = useCallback(
    async (signal) => {
      setLoading(true)
      setLoadError(null)
      try {
        const data = await auctionService.getCarrierAnalysis(auctionId, { signal })
        if (signal?.aborted) return
        setAnalysis(data)
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

  const { routeSummary, competition, physicalRequirements, travelPlan, segments, myBid } = analysis
  // The leader offer is a total (freight + toll), so it is compared against the bid's total.
  const myBidDistanceToLeader = myBid && bestLeaderOffer != null ? myBid.totalValue - bestLeaderOffer : null

  return (
    <AppShell title="Detalhes da Rota">
      <div className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden">

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
              <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                <div>
                  <div className="flex items-center gap-2 mb-1.5">
                    <Layers size={14} className="text-blue-500" />
                    <span className="font-mono text-xs font-bold text-slate-500">{routeSummary.referenceCode}</span>
                    <Badge className="bg-slate-800 text-white text-[9px] uppercase font-bold border-none hover:bg-slate-800">Em Leilão</Badge>
                  </div>
                  <h1 className="text-xl font-black text-slate-900">{routeSummary.shortItinerary}</h1>
                </div>
                <Button onClick={() => navigate(`/bid-analysis/${auctionId}/place-bid`)} className="shrink-0 bg-blue-600 hover:bg-blue-700 text-white font-bold h-9 text-xs">
                  <Gavel size={14} className="mr-1.5" /> {myBid ? "Atualizar Lance" : "Dar Lance"}
                </Button>
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

            {/* MERCADO & CONCORRÊNCIA */}
            <div className="rounded-xl border border-slate-200 bg-white p-4">
              <h3 className="font-bold text-slate-900 mb-3 flex items-center gap-2 text-xs">
                <Activity size={14} className="text-slate-500" /> Mercado & Concorrência
              </h3>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <div className="bg-slate-50 p-3 rounded-lg border border-slate-100">
                  <p className="text-[9px] font-bold text-slate-400 uppercase mb-0.5">Teto do Leilão</p>
                  <p className="text-lg font-black text-slate-800">{formatCurrency(ceiling)}</p>
                </div>
                <div className="bg-slate-50 p-3 rounded-lg border border-slate-100">
                  <p className="text-[9px] font-bold text-slate-400 uppercase mb-0.5">Lances Ativos</p>
                  <p className="text-lg font-black text-blue-600">{competition.activeBids}</p>
                </div>
                <div className="bg-slate-50 p-3 rounded-lg border border-slate-100">
                  <p className="text-[9px] font-bold text-slate-400 uppercase mb-0.5">Melhor Oferta (Líder)</p>
                  <p className="text-lg font-black font-mono text-emerald-600">
                    {bestLeaderOffer != null ? formatCurrency(bestLeaderOffer) : "Sem lances"}
                  </p>
                </div>
                <div className={`p-3 rounded-lg border ${myBid ? "border-blue-200 bg-blue-50" : "border-slate-100 bg-slate-50"}`}>
                  <p className={`text-[9px] font-bold uppercase mb-0.5 ${myBid ? "text-blue-700/70" : "text-slate-400"}`}>Seu Lance</p>
                  {myBid ? (
                    <>
                      <p className="text-lg font-black font-mono text-blue-800">{formatCurrency(myBid.totalValue)}</p>
                      {myBidDistanceToLeader != null && (
                        <p className={`text-[9px] font-bold ${myBidDistanceToLeader > 0 ? "text-rose-500" : "text-emerald-600"}`}>
                          {myBidDistanceToLeader > 0 ? `${formatCurrency(myBidDistanceToLeader)} acima do líder` : "Você lidera o leilão"}
                        </p>
                      )}
                      <p className="text-[9px] font-medium text-blue-700/70">Enviado em {formatDateTime(myBid.submittedAt)}</p>
                    </>
                  ) : (
                    <p className="text-lg font-black text-slate-400">Nenhum lance</p>
                  )}
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

            {/* PRODUTOS CONTIDOS NA ROTA */}
            <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
              <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/50 px-5 py-3">
                <div className="flex items-center gap-2">
                  <Layers size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Produtos Contidos nesta Rota ({segments.length})</h2>
                </div>
              </div>
              {segments.length === 0 ? (
                <p className="px-5 py-4 text-xs font-medium text-slate-500">Nenhum produto associado a esta rota.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  {segments.map((seg) => (
                    <div key={seg.id} className="flex items-center justify-between px-5 py-3 hover:bg-slate-50/50 transition-colors">
                      <div className="flex items-center gap-4">
                        <div>
                          <p className="font-mono text-[10px] font-semibold tracking-wider text-slate-400">#{shortId(seg.id)}</p>
                          <p className="text-xs font-bold text-slate-800">{seg.mainProduct}</p>
                          <p className="text-[10px] font-medium text-slate-500">{seg.origin} ➔ {seg.destination}</p>
                        </div>
                      </div>
                      <p className="text-xs font-bold font-mono text-slate-600">Teto: {formatCurrency(seg.financialCeiling ?? 0)}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* ITINERÁRIO */}
            <TravelPlan stops={travelPlan} />

          </div>
        </div>
      </div>
    </AppShell>
  )
}
