import { useCallback, useEffect, useMemo, useState } from "react"
import { ArrowLeft, CheckCircle2, Truck, MapPin, Clock, ShieldCheck, Trophy, ArrowDownRight, ExternalLink, Calendar, ChevronDown, ChevronUp, Activity, FileCheck, Loader2 } from "lucide-react"
import { Link, useParams } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { FormAlert } from "@/components/auth/FormAlert"
import { useAsyncAction } from "@/hooks/use-async-action"
import { getBidRanking, awardAuction } from "@/services/auction-service"

const formatCurrency = (value) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value ?? 0)

const formatDeadline = (value) => {
  if (!value) return "—"
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return "—"
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" }).format(date)
}

export default function AuctionBids() {
  // The route param carries the auction id (see /auction-bids/:segmentId route).
  const { segmentId: auctionId } = useParams()
  const [expandedBids, setExpandedBids] = useState([])
  const [ranking, setRanking] = useState(null)
  const [awardedTripId, setAwardedTripId] = useState(null)

  const loadAction = useAsyncAction((id) => getBidRanking(id))
  const awardAction = useAsyncAction(({ id, bidId }) => awardAuction(id, bidId))

  const reload = useCallback(async () => {
    if (!auctionId) return
    const result = await loadAction.run(auctionId)
    if (result.ok) {
      setRanking(result.data)
    }
  }, [auctionId, loadAction])

  useEffect(() => {
    reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auctionId])

  const routeSummary = ranking?.route ?? null
  const rankedBids = ranking?.bids ?? []
  const targetFare = routeSummary?.financialScenario?.consolidatedCeiling ?? 0
  const anttFloorFare = routeSummary?.financialScenario?.estimatedAnttFloor ?? 0
  const isClosed = ranking?.status === "CLOSED"

  const anttPercent = useMemo(() => {
    if (!targetFare || targetFare <= 0) return 72
    return Math.min(100, Math.round((anttFloorFare / targetFare) * 100))
  }, [targetFare, anttFloorFare])

  const toggleExpand = (bidId) => {
    setExpandedBids(prev => prev.includes(bidId) ? prev.filter(id => id !== bidId) : [...prev, bidId])
  }

  const handleAward = async (bidId) => {
    if (!auctionId || !bidId) return
    const result = await awardAction.run({ id: auctionId, bidId })
    if (result.ok) {
      setAwardedTripId(result.data?.tripId ?? null)
      await reload()
    }
  }

  return (
    <AppShell title="Análise de Lances">
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-5xl flex-col overflow-hidden">
        
        {/* HEADER */}
        <div className="flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1 mb-5">
          <Link to="/freights-offered-overview">
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900">
              <ArrowLeft size={16} className="mr-2" /> Voltar à Mesa de Leilões
            </Button>
          </Link>

          <div className="flex items-center gap-3">
            {routeSummary?.firstSegmentId && (
              <Button asChild variant="outline" className="h-9 border-slate-200 text-xs font-semibold text-slate-700 bg-white">
                <Link to={`/route-segment-details/${routeSummary.firstSegmentId}`}>
                  <ExternalLink size={14} className="mr-1.5" /> Detalhes da Rota
                </Link>
              </Button>
            )}
          </div>
        </div>

        {loadAction.pending && !ranking && (
          <div className="flex flex-1 items-center justify-center text-slate-400">
            <Loader2 className="mr-2 h-5 w-5 animate-spin" /> Carregando ranking de propostas…
          </div>
        )}

        {loadAction.error && (
          <div className="mb-5">
            <FormAlert message={loadAction.error.message} />
          </div>
        )}

        {/* CONTEXTO DA ROTA - A TRÍPLICE RESTRIÇÃO (Escopo, Tempo, Custo) */}
        {routeSummary && (
          <div className="flex shrink-0 flex-col md:flex-row rounded-xl border border-slate-200 bg-white mb-5 divide-y md:divide-y-0 md:divide-x divide-slate-100 overflow-hidden">
            
            {/* Bloco 1: O Escopo (Físico) */}
            <div className="flex-1 p-5 flex flex-col justify-center">
              <div className="flex items-center justify-between mb-2">
                <Badge className="bg-slate-100 text-slate-600 border-none text-[10px] font-bold uppercase tracking-wider">
                  {isClosed ? "Leilão Encerrado" : "Leilão Aberto"}
                </Badge>
                <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider">{routeSummary.segmentCount} Trecho(s)</span>
              </div>
              <h2 className="text-sm font-bold text-slate-900 mb-1.5 truncate">{routeSummary.itinerarySummary || "Rota Consolidada"}</h2>
              <p className="text-xs font-semibold text-slate-600 flex items-center gap-1.5">
                <MapPin size={14} className="text-blue-500 shrink-0" /> <span className="truncate">{routeSummary.itineraryWithStates || routeSummary.itinerarySummary}</span>
              </p>
            </div>

            {/* Bloco 2: O Tempo (Janelas de SLA com Destaque Máximo) */}
            <div className="w-full md:w-[340px] p-5 bg-blue-50/30 flex flex-col justify-center border-b-0">
              <p className="text-[10px] font-bold uppercase tracking-wider text-slate-500 mb-3 flex items-center gap-1.5">
                <Clock size={12} className="text-blue-500" /> Janelas de SLA Acordadas
              </p>
              <div className="space-y-2.5">
                {/* Card Coleta */}
                <div className="flex items-center justify-between bg-white px-3 py-2 rounded-lg border border-slate-200">
                    <div className="flex items-center gap-2">
                        <Calendar size={14} className="text-amber-500" />
                        <span className="text-xs font-bold text-slate-700">Primeira Coleta</span>
                    </div>
                        <span className="text-xs font-black text-slate-800">{formatDeadline(routeSummary.firstPickupDeadline)}</span>
                </div>
                {/* Card Entrega */}
                <div className="flex items-center justify-between bg-white px-3 py-2 rounded-lg border border-slate-200">
                    <div className="flex items-center gap-2">
                        <CheckCircle2 size={14} className="text-emerald-500" />
                        <span className="text-xs font-bold text-slate-700">Última Entrega</span>
                    </div>
                        <span className="text-xs font-black text-slate-800">{formatDeadline(routeSummary.lastDeliveryDeadline)}</span>
                </div>
              </div>
            </div>

            {/* Bloco 3: O Custo (Piso e Teto unificados) */}
            <div className="w-full md:w-[280px] p-5 bg-slate-50/50 flex flex-col justify-center gap-4">
              <div className="flex justify-between items-end">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400 mb-0.5">Orçamento Teto</p>
                  <p className="text-lg font-black text-slate-700 font-mono leading-none">{formatCurrency(targetFare)}</p>
                </div>
                <div className="text-right">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400 mb-0.5 flex items-center justify-end gap-1"><ShieldCheck size={10}/> Piso ANTT</p>
                  <p className="text-sm font-bold text-slate-500 font-mono leading-none">{formatCurrency(anttFloorFare)}</p>
                </div>
              </div>
              
              {/* Barra de Progresso da Janela de Negociação */}
              <div className="space-y-1.5">
                  <div className="h-1.5 w-full bg-slate-200 rounded-full overflow-hidden flex">
                    <div className="bg-slate-300" style={{ width: `${anttPercent}%` }} title="Valor Piso" />
                    <div className="bg-emerald-400 flex-1" title="Margem de Negociação" />
                  </div>
                  <div className="flex justify-between text-[9px] font-bold text-slate-400 uppercase tracking-wider">
                      <span>Bloqueado</span>
                      <span className="text-emerald-600">Margem Válida</span>
                  </div>
              </div>
            </div>

          </div>
        )}

        {/* CONTAINER DO RANKING */}
        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">
            
            <div className="space-y-3">
              <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3 ml-1">Ranking de Propostas ({rankedBids.length})</h3>

              {awardAction.error && (
                <FormAlert message={awardAction.error.message} />
              )}

              {awardedTripId && (
                <div className="flex items-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">
                  <CheckCircle2 size={16} /> Leilão adjudicado com sucesso. Viagem criada ({awardedTripId}).
                </div>
              )}
              
              {rankedBids.length === 0 ? (
                <div className="rounded-xl border border-slate-200 border-dashed py-12 text-center text-slate-500 bg-slate-50/50">
                  {loadAction.pending ? "Carregando propostas…" : "Nenhum lance recebido para este leilão ainda."}
                </div>
              ) : (
                rankedBids.map((bid) => {
                  const isTopRank = bid.rank === 1
                  const isWinner = bid.status === "WINNER"
                  const isExpanded = expandedBids.includes(bid.bidId)
                  const savings = bid.financials.savingsValue
                  const savingsPercent = bid.financials.savingsPercentage
                  const highlight = isWinner || (!isClosed && isTopRank)
                  const awarding = awardAction.pending
                  const bodyType = bid.vehicle.bodyType
                  const vehicleLabel = `${bodyType}${bid.vehicle.plate ? ` · ${bid.vehicle.plate}` : ""}`

                  return (
                    <div 
                      key={bid.bidId} 
                      className={`relative flex flex-col rounded-xl border p-4 transition-all ${
                        highlight 
                          ? "border-emerald-300 bg-emerald-50/20 ring-1 ring-emerald-100" 
                          : "border-slate-200 bg-white"
                      }`}
                    >
                      
                      {(isWinner || (!isClosed && isTopRank)) && (
                        <div className="absolute -top-2.5 left-4 flex items-center gap-1 rounded-full bg-emerald-100 border border-emerald-200 px-2 py-0.5 text-[9px] font-black uppercase tracking-wider text-emerald-700 shadow-sm">
                          <Trophy size={10} /> {isWinner ? "Vencedor" : "Melhor Oferta"}
                        </div>
                      )}

                      {/* LINHA PRINCIPAL VISÍVEL */}
                      <div className="flex items-center justify-between w-full">
                          <div className="flex items-center gap-5 w-[45%]">
                            <div className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-black ${
                              highlight ? "bg-emerald-600 text-white" : "bg-slate-100 text-slate-400"
                            }`}>
                              #{bid.rank}
                            </div>

                            <div className="min-w-0">
                              <h4 className="text-sm font-bold text-slate-800 truncate">{bid.carrier.tradeName}</h4>
                              <div className="flex items-center gap-3 mt-1">
                                <span className="flex items-center text-[11px] font-semibold text-slate-500">
                                  <Truck size={12} className="mr-1.5 text-slate-400" /> {vehicleLabel}
                                </span>
                                {bid.carrier.averageRating != null && (
                                  <span className="flex items-center text-[11px] font-semibold text-amber-500">
                                    ★ {bid.carrier.averageRating.toFixed(1)}
                                  </span>
                                )}
                              </div>
                            </div>
                          </div>

                          <div className="flex items-center justify-end gap-6 w-[55%]">
                            <div className="text-right">
                              <p className={`text-lg font-black font-mono ${highlight ? "text-emerald-700" : "text-slate-700"}`}>
                                {formatCurrency(bid.financials.totalValue)}
                              </p>
                              {savings >= 0 ? (
                                <p className="flex items-center justify-end text-[10px] font-bold text-emerald-600 mt-0.5">
                                  <ArrowDownRight size={12} className="mr-0.5" /> 
                                  Economia de {formatCurrency(savings)} ({savingsPercent}%)
                                </p>
                              ) : (
                                <p className="text-[10px] font-bold text-rose-500 mt-0.5">
                                  Sobrecusto de {formatCurrency(Math.abs(savings))} ({savingsPercent}%)
                                </p>
                              )}
                            </div>

                            {!isClosed && (
                              <Button 
                                disabled={awarding}
                                onClick={() => handleAward(bid.bidId)}
                                className={`h-9 px-5 text-xs font-bold tracking-wide transition-colors ${
                                  isTopRank 
                                    ? "bg-emerald-600 hover:bg-emerald-700 text-white" 
                                    : "bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                                }`}
                              >
                                {awarding ? <Loader2 size={14} className="animate-spin" /> : <><CheckCircle2 size={14} className="mr-1.5" /> Adjudicar Vencedor</>}
                              </Button>
                            )}

                            {isClosed && isWinner && (
                              <span className="flex items-center h-9 px-4 text-xs font-bold text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-md">
                                <CheckCircle2 size={14} className="mr-1.5" /> Adjudicado
                              </span>
                            )}

                            <Button 
                                variant="ghost" 
                                size="icon" 
                                className="h-8 w-8 text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0"
                                onClick={() => toggleExpand(bid.bidId)}
                            >
                                {isExpanded ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                            </Button>
                          </div>
                      </div>

                      {/* PAINEL RETRAÍVEL (ANÁLISE DE RISCO) */}
                      {isExpanded && (
                          <div className="mt-4 pt-4 border-t border-slate-100 grid grid-cols-1 md:grid-cols-3 gap-4 animate-in slide-in-from-top-2 fade-in duration-200">
                              <div className="bg-slate-50 rounded-lg p-3 border border-slate-100">
                                  <div className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500 mb-2">
                                      <Truck size={12} /> Compliance de Frota
                                  </div>
                                  <p className="text-xs font-semibold text-slate-800">{vehicleLabel}</p>
                                  <p className="text-[11px] text-slate-500 mt-0.5">Veículo alocado para o lance.</p>
                              </div>

                              <div className="bg-slate-50 rounded-lg p-3 border border-slate-100">
                                  <div className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500 mb-2">
                                      <FileCheck size={12} /> Validação de Seguro
                                  </div>
                                  <div className="flex items-center gap-1.5">
                                    <div className={`h-2 w-2 rounded-full ${bid.carrier.hasActiveInsurancePolicy ? 'bg-emerald-500' : 'bg-rose-500'}`} />
                                    <p className="text-xs font-semibold text-slate-800">{bid.carrier.hasActiveInsurancePolicy ? "Apólice Ativa" : "Sem Apólice Ativa"}</p>
                                  </div>
                                  <p className="text-[11px] text-slate-500 mt-0.5">{bid.carrier.hasActiveInsurancePolicy ? "Seguro de carga validado." : "Atenção: seguro não confirmado."}</p>
                              </div>

                              <div className="bg-slate-50 rounded-lg p-3 border border-slate-100">
                                  <div className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500 mb-2">
                                      <Activity size={12} /> Rating de Performance (OTD)
                                  </div>
                                  <p className="text-xs font-semibold text-slate-800">
                                    <span className={bid.carrier.onTimeDeliveryRate >= 95 ? "text-emerald-600" : "text-amber-600"}>{bid.carrier.onTimeDeliveryRate}%</span> de entregas no prazo
                                  </p>
                                  <p className="text-[11px] text-slate-500 mt-0.5">Nota média {bid.carrier.averageRating != null ? bid.carrier.averageRating.toFixed(1) : "—"} / 5.</p>
                              </div>
                          </div>
                      )}

                    </div>
                  )
                })
              )}
            </div>
            
          </div>
        </div>

      </div>
    </AppShell>
  )
}