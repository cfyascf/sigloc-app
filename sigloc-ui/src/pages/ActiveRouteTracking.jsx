import { useState } from "react"
import { ArrowLeft, RefreshCw, Truck, CheckCircle2, MapPinned, AlertTriangle, Clock, Phone, User, Fuel, Scale, ArrowRightLeft, Target, MapPin } from "lucide-react"
import { Link, useParams } from "react-router-dom"
import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

export default function ActiveRouteTracking() {
  const { routeId } = useParams()
  const [isPinging, setIsPinging] = useState(false)
  const [lastPing, setLastPing] = useState("14:20")
  
  // Novo estado para o SLA
  const [slaCompliance, setSlaCompliance] = useState(98.5)

  // Status vindo do motorista
  const [currentStatus] = useState({ 
    label: "Em Trânsito", 
    type: "normal", 
    time: "14:00" 
  })

  const handlePing = () => {
    setIsPinging(true)
    setTimeout(() => {
      setIsPinging(false)
      setLastPing(new Date().toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }))
      // Simula uma pequena variação no SLA ao atualizar a posição
      const variance = (Math.random() - 0.5) * 2
      setSlaCompliance(prev => Math.max(90, Math.min(99.9, prev + variance)).toFixed(1))
    }, 1500)
  }

  return (
    <AppShell title={`Monitoramento: Rota ${routeId || "ROT-9921"}`}>
      <div className="space-y-6">
        
        {/* HEADER */}
        <div className="flex items-center justify-between border-b border-slate-100 pb-4">
          <Link to="/active-routes">
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Rotas Ativas
            </Button>
          </Link>
          <Button className="h-9 bg-emerald-600 text-xs font-bold text-white hover:bg-emerald-700" onClick={handlePing} disabled={isPinging}>
            <RefreshCw size={14} className={`mr-1.5 ${isPinging ? 'animate-spin' : ''}`} />
            {isPinging ? "Localizando..." : "Localizar Motorista"}
          </Button>
        </div>

        {/* TOP KPI BAR (Incluindo SLA dinâmico) */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                <div className="h-10 w-10 bg-blue-50 text-blue-600 rounded-lg flex items-center justify-center"><ArrowRightLeft size={20}/></div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">Progresso da Rota</p>
                    <p className="text-sm font-bold text-slate-900">240 km / 408 km</p>
                </div>
            </div>
            
            {/* KPI DE SLA DINÂMICO */}
            <div className={`bg-white border rounded-xl p-4 flex items-center gap-3 transition-colors ${slaCompliance < 95 ? 'border-amber-200 bg-amber-50/50' : 'border-slate-200'}`}>
                <div className={`h-10 w-10 rounded-lg flex items-center justify-center ${slaCompliance < 95 ? 'bg-amber-100 text-amber-600' : 'bg-indigo-50 text-indigo-600'}`}>
                    <Target size={20}/>
                </div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">Conf. SLA</p>
                    <p className="text-sm font-black text-slate-900">{slaCompliance}%</p>
                </div>
            </div>

            <div className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                <div className="h-10 w-10 bg-purple-50 text-purple-600 rounded-lg flex items-center justify-center"><Scale size={20}/></div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">Utilização Carga</p>
                    <p className="text-sm font-bold text-slate-900">85%</p>
                </div>
            </div>
            <div className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                <div className="h-10 w-10 bg-emerald-50 text-emerald-600 rounded-lg flex items-center justify-center"><Clock size={20}/></div>
                <div>
                    <p className="text-[10px] uppercase font-bold text-slate-400">ETA Previsto</p>
                    <p className="text-sm font-bold text-slate-900">18:30h</p>
                </div>
            </div>
        </div>

        {/* DASHBOARD GRID */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
            
            <div className="lg:col-span-8 space-y-6">
                <div className="bg-white border border-slate-200 rounded-xl p-6">
                    <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700 mb-6 flex items-center gap-2">
                        <MapPinned size={14} /> Histórico de Paradas
                    </h3>
                    <div className="relative border-l-2 border-slate-100 ml-3 space-y-8">
                        <div className="relative pl-6"><div className="absolute -left-[9px] top-0 h-4 w-4 rounded-full bg-emerald-500 border-4 border-white" />
                            <p className="text-sm font-bold text-slate-800">Curitiba, PR</p>
                            <p className="text-xs text-slate-500">Saída: 14:00h</p>
                        </div>
                        <div className="relative pl-6"><div className="absolute -left-[9px] top-0 h-4 w-4 rounded-full bg-amber-500 ring-4 ring-amber-50" />
                            <p className="text-sm font-bold text-slate-800">Joinville, PR</p>
                            <p className="text-xs text-amber-600 font-bold">Em trânsito • Previsto 17:30h</p>
                        </div>
                    </div>
                </div>

                <div className="bg-white border border-slate-200 rounded-xl p-6">
                    <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700 mb-4">Log de Eventos</h3>
                    <div className="space-y-4">
                        <div className="flex gap-4 items-center text-sm p-3 bg-slate-50 rounded-lg border border-slate-100">
                             <Clock className="text-slate-400" size={16}/>
                             <span className="text-slate-600 font-medium">15:07</span>
                             <Badge variant="secondary" className="bg-emerald-100 text-emerald-700 border-none">Trânsito Normalizado</Badge>
                        </div>
                        <div className="flex gap-4 items-center text-sm p-3 bg-slate-50 rounded-lg border border-slate-100">
                             <Clock className="text-slate-400" size={16}/>
                             <span className="text-slate-600 font-medium">14:35</span>
                             <Badge variant="secondary" className="bg-amber-100 text-amber-700 border-none">Trânsito Intenso</Badge>
                        </div>
                    </div>
                </div>
            </div>

            <div className="lg:col-span-4 space-y-6">
                {/* SNAPSHOT MAP CARD (Ideia 1 implementada) */}
                <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
                    <div className="p-4 border-b border-slate-100 flex items-center justify-between">
                        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700 flex items-center gap-2">
                            <MapPin size={14} className="text-rose-500" /> Contexto Geográfico
                        </h3>
                        <span className="text-[10px] font-bold text-slate-400">Último Ping: {lastPing}</span>
                    </div>
                    {/* Visualização estilizada do mapa */}
                    <div className="h-[200px] bg-slate-50 relative flex items-center justify-center">
                        <div className="absolute inset-0 opacity-20" style={{ backgroundImage: 'radial-gradient(#cbd5e1 1px, transparent 1px)', backgroundSize: '20px 20px' }} />
                        <div className="relative w-full h-full flex items-center justify-center">
                             {/* Linha da Rota */}
                             <svg className="absolute w-full h-full" viewBox="0 0 400 200">
                                <path d="M50 150 Q 200 50 350 150" fill="none" stroke="#94a3b8" strokeWidth="4" strokeDasharray="6 6" />
                             </svg>
                             {/* Ponto do Motorista */}
                             <div className="absolute animate-pulse flex flex-col items-center">
                                <div className="h-4 w-4 bg-rose-600 rounded-full border-2 border-white" />
                                <span className="bg-white text-[9px] font-bold px-2 py-0.5 rounded mt-1 text-rose-700">KM 640</span>
                             </div>
                        </div>
                    </div>
                </div>

                <div className="bg-white border border-slate-200 rounded-xl p-5 space-y-4">
                    <div className="flex items-center gap-3">
                        <div className="h-10 w-10 bg-slate-100 rounded-lg flex items-center justify-center text-slate-500"><Truck size={20} /></div>
                        <div>
                            <p className="text-xs font-bold text-slate-800">Expresso Frio Ltda</p>
                            <p className="text-[10px] text-slate-400 font-mono">Placa: ABC-1234</p>
                        </div>
                    </div>
                    <div className="space-y-3 pt-4 border-t border-slate-100">
                        <div className="flex items-center gap-2 text-xs text-slate-600"><User size={14} className="text-slate-400" /> João Silva</div>
                        <div className="flex items-center gap-2 text-xs text-slate-600"><Phone size={14} className="text-slate-400" /> (41) 99999-8888</div>
                    </div>
                </div>
            </div>
        </div>
      </div>
    </AppShell>
  )
}