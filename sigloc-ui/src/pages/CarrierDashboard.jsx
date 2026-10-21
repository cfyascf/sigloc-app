import { useCallback, useEffect, useState } from "react"
import { ArrowDownRight, ExternalLink, Scale, Trophy, Truck, Clock, Loader2, AlertTriangle } from "lucide-react"
import { useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { useAsyncAction } from "@/hooks/use-async-action"
import { getCarrierDashboard } from "@/services/dashboard-service"

function formatCurrency(value) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value)
}

/** Formats a signed minute count into a compact countdown, e.g. "45 min", "1h 10m", "Atrasado". */
function formatTimeRemaining(minutes) {
  if (minutes <= 0) {
    return "Atrasado"
  }

  if (minutes < 60) {
    return `${minutes} min`
  }

  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  return rest === 0 ? `${hours}h` : `${hours}h ${rest}m`
}

function formatPercentage(value) {
  return `${Math.round(value)}%`
}

export default function CarrierDashboard() {
  const navigate = useNavigate()
  const { run, pending, error } = useAsyncAction(getCarrierDashboard)
  const [data, setData] = useState(null)

  const load = useCallback(async () => {
    const result = await run()
    if (result.ok) {
      setData(result.data)
    }
    return result
  }, [run])

  useEffect(() => {
    let active = true
    run().then((result) => {
      if (active && result.ok) {
        setData(result.data)
      }
    })
    return () => {
      active = false
    }
  }, [run])

  const kpis = data?.kpis
  const performance = data?.performance
  const activeDisputes = data?.activeDisputes ?? []
  const controlTower = data?.controlTower ?? []

  const kpiCards = [
    { title: "Veículos Livres", value: kpis?.availableVehicles ?? 0, style: "text-amber-600" },
    { title: "Lances Ativos", value: kpis?.activeBids ?? 0, style: "text-blue-600" },
    { title: "Em Trânsito", value: kpis?.inTransitTrips ?? 0, style: "text-emerald-600" },
  ]

  const fleetOperation = performance?.fleetOperationPercentage ?? 0
  const capacityUtilization = performance?.capacityUtilizationPercentage ?? 0
  const auctionSuccessRate = performance?.auctionSuccessRate ?? 0

  return (
    <AppShell title="Visão Geral" contentClassName="overflow-hidden" innerClassName="h-full min-h-0">
      <div className="flex h-full min-h-0 flex-col overflow-hidden">
        
        {/* O GRANDE CARD BRANCO UNIFICADOR */}
        <div className="flex-1 bg-white border border-slate-200 rounded-xl p-6 shadow-sm flex flex-col min-h-0 overflow-hidden">

          {pending && !data ? (
            <div className="flex flex-1 items-center justify-center gap-2 text-slate-400">
              <Loader2 className="animate-spin" size={18} />
              <span className="text-sm font-semibold">Carregando indicadores…</span>
            </div>
          ) : error && !data ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-3 text-center">
              <AlertTriangle className="text-rose-500" size={28} />
              <p className="text-sm font-semibold text-slate-700">Não foi possível carregar o painel.</p>
              <p className="text-xs text-slate-400">{error.message}</p>
              <Button variant="outline" size="sm" onClick={load}>Tentar novamente</Button>
            </div>
          ) : (
            <>
          {/* SEÇÃO 1: LINHA DE CONTADORES (KPIs MACROS) */}
          <section className="flex items-center justify-between border-b border-slate-100 pb-5 mb-5">
            <div className="flex gap-16">
              {kpiCards.map((kpi) => (
                <div key={kpi.title}>
                  <p className="text-[11px] font-bold uppercase tracking-wider text-slate-400">{kpi.title}</p>
                  <p className={`text-2xl font-black ${kpi.style} mt-0.5`}>{kpi.value}</p>
                </div>
              ))}
            </div>
            
            <div className="flex items-center gap-2 text-xs font-bold text-slate-400 tracking-wide bg-slate-50 px-3 py-1.5 rounded-lg border border-slate-100">
              <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
              <span>DISPATCH ATIVO</span>
            </div>
          </section>

          {/* SEÇÃO 2: DESEMPENHO DA TRANSPORTADORA */}
          <section className="border-b border-slate-100 pb-5 mb-5">
            <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">Performance e Operação (Mês Atual)</h2>
            <div className="grid grid-cols-3 gap-8">
              
              {/* Frota Ativa */}
              <div className="flex items-center gap-3 bg-slate-50/60 p-3 rounded-lg border border-slate-100">
                <div className="p-2 bg-white rounded-md text-slate-600 border border-slate-100">
                  <Truck size={16} />
                </div>
                <div className="flex-1">
                  <div className="flex justify-between items-baseline">
                    <span className="text-xs font-semibold text-slate-500">Frota em Operação</span>
                    <span className="text-sm font-black text-slate-800">{formatPercentage(fleetOperation)}</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${fleetOperation}%` }} />
                  </div>
                </div>
              </div>

              {/* Aproveitamento de Capacidade */}
              <div className="flex items-center gap-3 bg-slate-50/60 p-3 rounded-lg border border-slate-100">
                <div className="p-2 bg-white rounded-md text-slate-600 border border-slate-100">
                  <Scale size={16} />
                </div>
                <div className="flex-1">
                  <div className="flex justify-between items-baseline">
                    <span className="text-xs font-semibold text-slate-500">Ocupação (Peso/Vol)</span>
                    <span className="text-sm font-black text-slate-800">{formatPercentage(capacityUtilization)}</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${capacityUtilization}%` }} />
                  </div>
                </div>
              </div>

              {/* Taxa de Vitória (Win Rate) */}
              <div className="flex items-center gap-3 bg-slate-50/60 p-3 rounded-lg border border-slate-100">
                <div className="p-2 bg-white rounded-md text-emerald-600 border border-slate-100">
                  <Trophy size={16} />
                </div>
                <div className="flex-1">
                  <div className="flex justify-between items-baseline">
                    <span className="text-xs font-semibold text-slate-500">Sucesso em Leilões</span>
                    <span className="text-sm font-black text-emerald-600">{formatPercentage(auctionSuccessRate)}</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-emerald-500 h-full rounded-full" style={{ width: `${auctionSuccessRate}%` }} />
                  </div>
                </div>
              </div>

            </div>
          </section>

          {/* SEÇÃO 3: LANCES E OPERAÇÃO (BLOCO INFERIOR SCROLLÁVEL) */}
          <div className="flex-1 grid grid-cols-[1.2fr_1fr] gap-12 min-h-0 overflow-hidden">
            
            {/* Coluna Esquerda: Radar de Lances */}
            <div className="flex flex-col min-h-0">
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400">Radar de Lances (Disputas Ativas)</h2>
                <Button 
                  variant="ghost" size="sm" 
                  className="h-7 text-xs text-blue-600 hover:text-blue-700 font-semibold gap-1 px-2"
                  onClick={() => navigate("/freights-offers-overview")}
                >
                  Ir para Mural <ExternalLink size={12} />
                </Button>
              </div>
              
              <div className="flex-1 divide-y divide-slate-100 overflow-hidden">
                {activeDisputes.length === 0 ? (
                  <div className="flex h-full items-center justify-center py-8 text-xs font-semibold text-slate-400">
                    Nenhuma disputa ativa no momento.
                  </div>
                ) : (
                  activeDisputes.map((item) => {
                    const isWinning = item.status === "VENCENDO"

                    return (
                      <div
                        key={item.routeId}
                        className="flex w-full items-center justify-between rounded-lg py-3 px-2 text-left"
                      >
                        <div>
                          <div className="flex items-center gap-2 mb-1">
                            <span className="font-mono text-[10px] font-bold text-slate-400">{item.routeId}</span>
                            <Badge variant="outline" className={`text-[8px] font-black uppercase tracking-wider px-1.5 py-0 border ${
                                isWinning ? "bg-emerald-50 text-emerald-700 border-emerald-200" : "bg-rose-50 text-rose-700 border-rose-200"
                            }`}>
                                {item.status}
                            </Badge>
                          </div>
                          <p className="text-sm font-bold text-slate-800">{item.itinerary}</p>
                        </div>

                        <div className="text-right">
                          <div className="flex justify-end gap-3 mb-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">
                              <span>Meu Lance</span>
                              <span>Líder</span>
                          </div>
                          <div className="flex items-center justify-end gap-3">
                              <span className="text-sm font-bold text-slate-900 font-mono">{formatCurrency(item.myBidAmount)}</span>
                              <span className="text-sm font-bold text-slate-500 font-mono">{formatCurrency(item.leaderBidAmount)}</span>
                          </div>

                          {!isWinning && item.amountToCover > 0 && (
                              <p className="text-[10px] font-bold text-rose-500 flex items-center justify-end gap-1 mt-1">
                                  <ArrowDownRight size={12} /> {formatCurrency(item.amountToCover)} para cobrir
                              </p>
                          )}
                        </div>
                      </div>
                    )
                  })
                )}
              </div>
            </div>

            {/* Coluna Direita: Próximos Marcos de SLA (Despacho) */}
            <div className="flex flex-col min-h-0 border-l border-slate-100 pl-10">
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400">Torre de Controle (SLAs)</h2>
                <Button 
                  variant="ghost" size="sm" 
                  className="h-7 text-xs text-blue-600 hover:text-blue-700 font-semibold gap-1 px-2"
                  onClick={() => navigate("/fleet-management")}
                >
                  Ver Frota <ExternalLink size={12} />
                </Button>
              </div>

              <div className="flex-1 divide-y divide-slate-100 overflow-hidden">
                {controlTower.length === 0 ? (
                  <div className="flex h-full items-center justify-center py-8 text-xs font-semibold text-slate-400">
                    Nenhuma viagem em rota no momento.
                  </div>
                ) : (
                  controlTower.map((alert) => (
                    <div key={alert.referenceCode} className="flex items-center justify-between py-3 px-2">
                      <div className="flex items-center gap-3">
                        <div className={`h-2 w-2 rounded-full shrink-0 ${alert.isDelayed ? "bg-rose-600 animate-pulse" : "bg-amber-500"}`} />
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-mono text-[10px] font-bold text-slate-400"><Truck size={10} className="inline mr-1"/>{alert.vehiclePlate}</span>
                            <span className="text-xs font-bold text-slate-800">{alert.referenceCode}</span>
                          </div>
                          <span className={`inline-block mt-1 px-1.5 py-0.5 rounded text-[9px] font-bold tracking-wide uppercase ${
                              alert.isDelayed ? "bg-rose-50 text-rose-700" : "bg-slate-100 text-slate-600"
                          }`}>
                            {alert.milestoneType}
                          </span>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className={`text-sm font-black font-mono flex items-center justify-end gap-1 ${alert.isDelayed ? "text-rose-600" : "text-slate-700"}`}>
                          <Clock size={12} /> {formatTimeRemaining(alert.timeRemainingMinutes)}
                        </p>
                        <p className="text-[9px] font-bold text-slate-400 uppercase tracking-wider mt-0.5">Prazo Restante</p>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>

          </div>
            </>
          )}

        </div>
      </div>
    </AppShell>
  )
}
