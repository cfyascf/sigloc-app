import { useCallback, useEffect, useState } from "react"
import { ArrowUpRight, ArrowDownRight, ExternalLink, ShieldCheck, Scale, Box, Loader2, AlertTriangle } from "lucide-react"
import { useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { useAsyncAction } from "@/hooks/use-async-action"
import { getExecutiveDashboard } from "@/services/dashboard-service"

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

export default function ContractorDashboard() {
  const navigate = useNavigate()
  const { run, pending, error } = useAsyncAction(getExecutiveDashboard)
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
  const efficiency = data?.networkEfficiency
  const costDeviations = data?.costDeviations ?? []
  const slaMilestones = data?.slaMilestones ?? []

  const kpiCards = [
    { title: "Trechos Avulsos", value: kpis?.unassignedSegments ?? 0, style: "text-slate-900" },
    { title: "Leilões Ativos", value: kpis?.activeAuctions ?? 0, style: "text-blue-600" },
    { title: "Em Trânsito", value: kpis?.inTransitTrips ?? 0, style: "text-emerald-600" },
  ]

  return (
    <AppShell title="Visão Geral" contentClassName="overflow-hidden" innerClassName="h-full min-h-0">
      {/* Container mestre rígido na viewport para eliminar scroll da página */}
      <div className="flex h-full min-h-0 flex-col overflow-hidden">
        
        {/* O GRANDE CARD BRANCO UNIFICADOR DE TODA A TORRE DE CONTROLE */}
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
            
            <div className="flex items-center gap-2 text-xs font-bold text-slate-400 tracking-wide">
              <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
              <span>SISTEMA ATIVO</span>
            </div>
          </section>

          {/* SEÇÃO 2: EFICIÊNCIA FÍSICA DA MALHA (Subiu para o Topo como Diagnóstico) */}
          <section className="border-b border-slate-100 pb-5 mb-5">
            <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">Eficiência Física da Malha (Média Atual)</h2>
            <div className="grid grid-cols-3 gap-8">
              
              {/* Peso Médio */}
              <div className="flex items-center gap-3 bg-slate-50/60 p-3 rounded-lg border border-slate-100">
                <div className="p-2 bg-white rounded-md text-slate-600 border border-slate-100">
                  <Scale size={16} />
                </div>
                <div className="flex-1">
                  <div className="flex justify-between items-baseline">
                    <span className="text-xs font-semibold text-slate-500">Ocupação de Peso</span>
                    <span className="text-sm font-black text-slate-800">{formatPercentage(efficiency?.averageWeightOccupationPercentage ?? 0)}</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${efficiency?.averageWeightOccupationPercentage ?? 0}%` }} />
                  </div>
                </div>
              </div>

              {/* Volume Médio */}
              <div className="flex items-center gap-3 bg-slate-50/60 p-3 rounded-lg border border-slate-100">
                <div className="p-2 bg-white rounded-md text-slate-600 border border-slate-100">
                  <Box size={16} />
                </div>
                <div className="flex-1">
                  <div className="flex justify-between items-baseline">
                    <span className="text-xs font-semibold text-slate-500">Ocupação de Volume</span>
                    <span className="text-sm font-black text-slate-800">{formatPercentage(efficiency?.averageVolumeOccupationPercentage ?? 0)}</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${efficiency?.averageVolumeOccupationPercentage ?? 0}%` }} />
                  </div>
                </div>
              </div>

              {/* Acoplamento Continuous Move */}
              <div className="flex items-center gap-3 bg-slate-50/60 p-3 rounded-lg border border-slate-100">
                <div className="p-2 bg-white rounded-md text-emerald-600 border border-slate-100">
                  <ShieldCheck size={16} />
                </div>
                <div className="flex-1">
                  <div className="flex justify-between items-baseline">
                    <span className="text-xs font-semibold text-slate-500">Aproveitamento de Rotas</span>
                    <span className="text-sm font-black text-emerald-600">{formatPercentage(efficiency?.routeUtilizationPercentage ?? 0)}</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-emerald-500 h-full rounded-full" style={{ width: `${efficiency?.routeUtilizationPercentage ?? 0}%` }} />
                  </div>
                </div>
              </div>

            </div>
          </section>

          {/* SEÇÃO 3: DETALHES DE OPERAÇÃO (CUSTOS E SLAs NO BLOCO INFERIOR) */}
          <div className="flex-1 grid grid-cols-[1.2fr_1fr] gap-12 min-h-0 overflow-hidden">
            
            {/* Coluna Esquerda: Desvio de Custo */}
            <div className="flex flex-col min-h-0">
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400">Desvio de Custo (Atual vs Alvo)</h2>
                <Button 
                  variant="ghost" size="sm" 
                  className="h-7 text-xs text-blue-600 hover:text-blue-700 font-semibold gap-1 px-2"
                  onClick={() => navigate("/freights-offered-overview")}
                >
                  Ver Detalhes <ExternalLink size={12} />
                </Button>
              </div>
              
              <div className="flex-1 divide-y divide-slate-100 overflow-hidden">
                {costDeviations.length === 0 ? (
                  <p className="py-3 text-sm text-slate-400">Nenhum desvio de custo no momento.</p>
                ) : (
                  costDeviations.map((item) => {
                    const isAbove = item.isOverBudget
                    const diff = Math.abs(item.deviationAmount)

                    return (
                      <div
                        key={item.routeId}
                        className="flex w-full items-center justify-between rounded-lg py-3 text-left first:pt-0 last:pb-0"
                      >
                        <div>
                          <p className="text-sm font-semibold text-slate-800">{item.itinerary}</p>
                          <p className="text-xs text-slate-400 mt-0.5">Alvo: {formatCurrency(item.targetBudget)}</p>
                        </div>
                        <div className="text-right">
                          <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Melhor lance</p>
                          <p className="text-sm font-bold text-slate-900">{formatCurrency(item.currentBestBid)}</p>
                          <div className={`mt-0.5 flex items-center justify-end gap-1 text-xs font-bold ${isAbove ? "text-rose-600" : "text-emerald-600"}`}>
                            {isAbove ? <ArrowUpRight size={14} /> : <ArrowDownRight size={14} />}
                            <span>{formatCurrency(diff)} ({isAbove ? "+" : "-"})</span>
                          </div>
                        </div>
                      </div>
                    )
                  })
                )}
              </div>
            </div>

            {/* Coluna Direito: Próximos Marcos de SLA */}
            <div className="flex flex-col min-h-0 border-l border-slate-100 pl-10">
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400">Próximos Marcos de SLA</h2>
                <Button 
                  variant="ghost" size="sm" 
                  className="h-7 text-xs text-blue-600 hover:text-blue-700 font-semibold gap-1 px-2"
                  onClick={() => navigate("/route-segment-management")}
                >
                  Gerenciar Trechos <ExternalLink size={12} />
                </Button>
              </div>

              <div className="flex-1 divide-y divide-slate-100 overflow-hidden">
                {slaMilestones.length === 0 ? (
                  <p className="py-3 text-sm text-slate-400">Nenhum marco de SLA pendente.</p>
                ) : (
                  slaMilestones.map((alert) => (
                    <div key={alert.referenceCode} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
                      <div className="flex items-center gap-3">
                        <div className={`h-2 w-2 rounded-full ${alert.isCritical ? "bg-rose-600 animate-pulse" : "bg-amber-500"}`} />
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-mono text-xs font-bold text-slate-400">{alert.referenceCode}</span>
                            <span className="text-sm font-bold text-slate-800">{alert.itinerary}</span>
                          </div>
                          <span className="inline-block mt-0.5 bg-slate-100 px-1.5 py-0.5 rounded text-[10px] font-bold text-slate-500 tracking-wide">
                            {alert.milestoneType}
                          </span>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className={`text-sm font-black font-mono ${alert.isCritical ? "text-rose-600" : "text-slate-700"}`}>
                          {formatTimeRemaining(alert.timeRemainingMinutes)}
                        </p>
                        <p className="text-[10px] font-bold text-slate-400 uppercase tracking-wider mt-0.5">Restante</p>
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