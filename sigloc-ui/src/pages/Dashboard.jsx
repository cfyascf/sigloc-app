import { ArrowUpRight, ArrowDownRight, ExternalLink, ShieldCheck, Scale, Box } from "lucide-react"
import { useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"

const kpis = [
  { title: "Trechos Avulsos", value: "14", style: "text-slate-900" },
  { title: "Leilões Ativos", value: "3", style: "text-blue-600" },
  { title: "Em Trânsito", value: "8", style: "text-emerald-600" },
]

const auctionFinance = [
  { id: "ROT-9921", auctionId: "TRC-201", label: "Curitiba → São Paulo", current: 4280, target: 4100, status: "ACIMA" },
  { id: "ROT-9922", auctionId: "TRC-202", label: "Joinville → Campinas", current: 3680, target: 3600, status: "ACIMA" },
  { id: "ROT-9923", auctionId: "TRC-203", label: "Londrina → Contagem", current: 3100, target: 3500, status: "ABAIXO" },
  { id: "ROT-9924", auctionId: "TRC-201", label: "Maringá → Serra", current: 4050, target: 4400, status: "ABAIXO" },
]

const slaAlerts = [
  { id: "TRC-1042", route: "CWB ➔ SP", action: "COLETA", time: "45 min", critical: true },
  { id: "TRC-1045", route: "RAO ➔ UDI", action: "COLETA", time: "1h 10m", critical: true },
  { id: "TRC-1043", route: "SP ➔ CPQ", action: "TRANSBORDO", time: "2h 20m", critical: false },
  { id: "TRC-1051", route: "CPQ ➔ RJ", action: "DESCARGA", time: "3h 05m", critical: false },
]

const fleetUtilization = {
  weightEfficiency: 88,
  volumeEfficiency: 74,
  continuousMoveSuccess: 92,
}

function formatCurrency(value) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value)
}

export default function ContractorDashboard() {
  const navigate = useNavigate()

  return (
    <AppShell title="Visão Geral" contentClassName="overflow-hidden" innerClassName="h-full min-h-0">
      {/* Container mestre rígido na viewport para eliminar scroll da página */}
      <div className="flex h-full min-h-0 flex-col overflow-hidden">
        
        {/* O GRANDE CARD BRANCO UNIFICADOR DE TODA A TORRE DE CONTROLE */}
        <div className="flex-1 bg-white border border-slate-200 rounded-xl p-6 shadow-sm flex flex-col min-h-0 overflow-hidden">
          
          {/* SEÇÃO 1: LINHA DE CONTADORES (KPIs MACROS) */}
          <section className="flex items-center justify-between border-b border-slate-100 pb-5 mb-5">
            <div className="flex gap-16">
              {kpis.map((kpi) => (
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
                    <span className="text-sm font-black text-slate-800">{fleetUtilization.weightEfficiency}%</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${fleetUtilization.weightEfficiency}%` }} />
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
                    <span className="text-sm font-black text-slate-800">{fleetUtilization.volumeEfficiency}%</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-slate-700 h-full rounded-full" style={{ width: `${fleetUtilization.volumeEfficiency}%` }} />
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
                    <span className="text-sm font-black text-emerald-600">{fleetUtilization.continuousMoveSuccess}%</span>
                  </div>
                  <div className="w-full bg-slate-200 h-1.5 rounded-full mt-1.5 overflow-hidden">
                    <div className="bg-emerald-500 h-full rounded-full" style={{ width: `${fleetUtilization.continuousMoveSuccess}%` }} />
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
                {auctionFinance.slice(0, 4).map((item) => {
                  const isAbove = item.status === "ACIMA"
                  const diff = Math.abs(item.current - item.target)

                  return (
                    <button
                      key={item.id}
                      type="button"
                      onClick={() => navigate(`/auction-bids/${item.auctionId}`)}
                      className="flex w-full items-center justify-between rounded-lg py-3 text-left transition-colors first:pt-0 last:pb-0 hover:bg-slate-50/80"
                    >
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-mono text-xs font-bold text-slate-400">{item.id}</span>
                          <span className="text-sm font-semibold text-slate-800">{item.label}</span>
                        </div>
                        <p className="text-xs text-slate-400 mt-0.5">Alvo: {formatCurrency(item.target)}</p>
                      </div>
                      <div className="text-right">
                        <p className="text-sm font-bold text-slate-900">{formatCurrency(item.current)}</p>
                        <div className={`mt-0.5 flex items-center justify-end gap-1 text-xs font-bold ${isAbove ? "text-rose-600" : "text-emerald-600"}`}>
                          {isAbove ? <ArrowUpRight size={14} /> : <ArrowDownRight size={14} />}
                          <span>{formatCurrency(diff)} ({isAbove ? "+" : "-"})</span>
                        </div>
                      </div>
                    </button>
                  )
                })}
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
                {slaAlerts.slice(0, 4).map((alert) => (
                  <div key={alert.id} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
                    <div className="flex items-center gap-3">
                      <div className={`h-2 w-2 rounded-full ${alert.critical ? "bg-rose-600 animate-pulse" : "bg-amber-500"}`} />
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-mono text-xs font-bold text-slate-400">{alert.id}</span>
                          <span className="text-sm font-bold text-slate-800">{alert.route}</span>
                        </div>
                        <span className="inline-block mt-0.5 bg-slate-100 px-1.5 py-0.5 rounded text-[10px] font-bold text-slate-500 tracking-wide">
                          {alert.action}
                        </span>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className={`text-sm font-black font-mono ${alert.critical ? "text-rose-600" : "text-slate-700"}`}>
                        {alert.time}
                      </p>
                      <p className="text-[10px] font-bold text-slate-400 uppercase tracking-wider mt-0.5">Restante</p>
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