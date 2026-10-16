import { ArrowDownRight, ExternalLink, Scale, Trophy, Truck, Clock } from "lucide-react"
import { useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

// ==========================================
// MOCKS ADAPTADOS PARA TRANSPORTADORA
// ==========================================
const kpis = [
  { title: "Veículos Livres", value: "4", style: "text-amber-600"},
  { title: "Lances Ativos", value: "12", style: "text-blue-600" },
  { title: "Em Trânsito", value: "38", style: "text-emerald-600"},
]

const carrierPerformance = {
  fleetActive: 88, // % de frota rodando
  capacityUtilization: 92, // % de ocupação de peso/cubagem nos caminhoes ativos
  winRate: 34, // % de vitorias nos ultimos leiloes
}

const activeBidsTracker = [
  { id: "ROT-9921", auctionId: "TRC-201", label: "Curitiba → São Paulo", myBid: 4200, bestBid: 4200, status: "VENCENDO" },
  { id: "ROT-9922", auctionId: "TRC-202", label: "Joinville → Campinas", myBid: 3680, bestBid: 3600, status: "PERDENDO" },
  { id: "ROT-9923", auctionId: "TRC-203", label: "Londrina → Contagem", myBid: 3500, bestBid: 3200, status: "PERDENDO" },
  { id: "ROT-9924", auctionId: "TRC-204", label: "Maringá → Serra", myBid: 4050, bestBid: 4050, status: "VENCENDO" },
]

const operationAlerts = [
  { id: "TRC-1042", truck: "ABC-1234", action: "ATRASO NA COLETA", time: "- 15 min", critical: true },
  { id: "TRC-1045", truck: "XYZ-9876", action: "PROX. COLETA", time: "1h 10m", critical: false },
  { id: "TRC-1043", truck: "QWE-5544", action: "DESCARGA SLA", time: "2h 20m", critical: false },
  { id: "TRC-1051", truck: "ASD-9988", action: "FIM DE JORNADA", time: "3h 00m", critical: false },
]

function formatCurrency(value) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value)
}

export default function CarrierDashboard() {
  const navigate = useNavigate()

  return (
    <AppShell title="Visão Geral" contentClassName="overflow-hidden" innerClassName="h-full min-h-0">
      <div className="flex h-full min-h-0 flex-col overflow-hidden">
        
        {/* O GRANDE CARD BRANCO UNIFICADOR */}
        <div className="flex-1 bg-white border border-slate-200 rounded-xl p-6 flex flex-col min-h-0 overflow-hidden">
          
          {/* SEÇÃO 1: LINHA DE CONTADORES (KPIs MACROS) */}
          <section className="flex items-center justify-between border-b border-slate-100 pb-5 mb-5">
            <div className="flex gap-16">
              {kpis.map((kpi) => (
                <div key={kpi.title}>
                  <p className="text-[11px] font-bold uppercase tracking-wider text-slate-400">{kpi.title}</p>
                  <div className="flex items-baseline gap-2 mt-0.5">
                      <p className={`text-2xl font-black ${kpi.style}`}>{kpi.value}</p>
                      <p className="text-[10px] font-semibold text-slate-400 uppercase">{kpi.desc}</p>
                  </div>
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
                    <span className="text-sm font-black text-slate-800">{carrierPerformance.fleetActive}%</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${carrierPerformance.fleetActive}%` }} />
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
                    <span className="text-sm font-black text-slate-800">{carrierPerformance.capacityUtilization}%</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${carrierPerformance.capacityUtilization}%` }} />
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
                    <span className="text-sm font-black text-emerald-600">{carrierPerformance.winRate}%</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-emerald-500 h-full rounded-full" style={{ width: `${carrierPerformance.winRate}%` }} />
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
                {activeBidsTracker.map((item) => {
                  const isWinning = item.status === "VENCENDO"
                  const diff = item.myBid - item.bestBid

                  return (
                    <button
                      key={item.id}
                      type="button"
                      onClick={() => navigate(`/bid-analysis/${item.auctionId}`)}
                      className="flex w-full items-center justify-between rounded-lg py-3 px-2 text-left transition-colors hover:bg-slate-50"
                    >
                      <div>
                        <div className="flex items-center gap-2 mb-1">
                          <Badge variant="outline" className={`text-[8px] font-black uppercase tracking-wider px-1.5 py-0 border ${
                              isWinning ? "bg-emerald-50 text-emerald-700 border-emerald-200" : "bg-rose-50 text-rose-700 border-rose-200"
                          }`}>
                              {item.status}
                          </Badge>
                        </div>
                        <p className="text-sm font-bold text-slate-800">{item.label}</p>
                      </div>

                      <div className="text-right">
                        <div className="flex justify-end gap-3 mb-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">
                            <span>Meu Lance</span>
                            <span>Líder</span>
                        </div>
                        <div className="flex items-center justify-end gap-3">
                            <span className="text-sm font-bold text-slate-900 font-mono">{formatCurrency(item.myBid)}</span>
                            <span className="text-sm font-bold text-slate-500 font-mono">{formatCurrency(item.bestBid)}</span>
                        </div>
                        
                        {!isWinning && diff > 0 && (
                            <p className="text-[10px] font-bold text-rose-500 flex items-center justify-end gap-1 mt-1">
                                <ArrowDownRight size={12} /> R$ {diff} para cobrir
                            </p>
                        )}
                      </div>
                    </button>
                  )
                })}
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
                {operationAlerts.map((alert) => (
                  <div key={alert.id} className="flex items-center justify-between py-3 px-2">
                    <div className="flex items-center gap-3">
                      <div className={`h-2 w-2 rounded-full shrink-0 ${alert.critical ? "bg-rose-600 animate-pulse" : "bg-amber-500"}`} />
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-mono text-[10px] font-bold text-slate-400"><Truck size={10} className="inline mr-1"/>{alert.truck}</span>
                        </div>
                        <span className={`inline-block mt-1 px-1.5 py-0.5 rounded text-[9px] font-bold tracking-wide uppercase ${
                            alert.critical ? "bg-rose-50 text-rose-700" : "bg-slate-100 text-slate-600"
                        }`}>
                          {alert.action}
                        </span>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className={`text-sm font-black font-mono flex items-center justify-end gap-1 ${alert.critical ? "text-rose-600" : "text-slate-700"}`}>
                        <Clock size={12} /> {alert.time}
                      </p>
                      <p className="text-[9px] font-bold text-slate-400 uppercase tracking-wider mt-0.5">Prazo Restante</p>
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
