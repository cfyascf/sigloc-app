import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { Search, Filter, Truck, ChevronRight, RefreshCw } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

export default function ActiveRoutes() {
  const navigate = useNavigate()
  
  // Mock das rotas ativas
  const [rotas] = useState([
    { id: "ROT-9921", transportadora: "Expresso Frio Ltda", placa: "ABC-1234", status: "Em curso", progresso: 65, eta: "18:30", lastPing: "14:20" },
    { id: "ROT-8840", transportadora: "LogBrasil S.A.", placa: "XYZ-9876", status: "Atrasado", progresso: 40, eta: "21:00", lastPing: "13:00" },
    { id: "ROT-7732", transportadora: "Transportes Rapidos", placa: "LMN-5544", status: "Em curso", progresso: 90, eta: "16:45", lastPing: "14:40" },
  ])

  return (
    <AppShell title="Rotas Ativas">
      <div className="space-y-6">
        
        {/* HEADER */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">Rotas em Execução</h1>
          </div>
          
          <div className="flex items-center gap-3">
             <div className="relative w-64">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input placeholder="Buscar rota ou transportadora..." className="h-9 border-slate-200 bg-white pl-9 text-xs" />
            </div>
            <Button variant="outline" className="h-9 border-slate-200 text-xs font-semibold bg-white">
               <Filter size={14} className="mr-2" /> Filtros
            </Button>
          </div>
        </div>

        {/* LISTAGEM EM CARDS */}
        <div className="space-y-3">
          {rotas.map((rota) => (
            <button
              key={rota.id}
              type="button"
              onClick={() => navigate(`/active-route-tracking/${rota.id}`)}
              className="group flex w-full items-center gap-6 rounded-xl border border-slate-200 bg-white p-4 transition-all hover:border-blue-300 hover:ring-1 hover:ring-blue-100 cursor-pointer"
            >
              {/* ID + STATUS */}
              <div className="w-[120px]">
                <Badge variant="outline" className={`text-[10px] font-bold uppercase border-none px-1.5 py-0 ${rota.status === 'Atrasado' ? 'bg-rose-50 text-rose-600' : 'bg-emerald-50 text-emerald-600'}`}>
                    {rota.status}
                </Badge>
              </div>

              {/* TRANSPORTADORA + VEÍCULO */}
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-slate-800">{rota.transportadora}</p>
                <div className="flex items-center gap-2 text-[11px] text-slate-400 mt-0.5">
                    <Truck size={12} /> {rota.placa}
                </div>
              </div>

              {/* PROGRESSO */}
              <div className="w-[180px]">
                <div className="flex justify-between text-[10px] font-bold text-slate-400 mb-1">
                    <span>Progresso</span>
                    <span>{rota.progresso}%</span>
                </div>
                <div className="h-1.5 w-full bg-slate-100 rounded-full overflow-hidden">
                    <div className="h-full bg-blue-600" style={{ width: `${rota.progresso}%` }} />
                </div>
              </div>

              {/* DADOS DE MONITORAMENTO */}
              <div className="w-[180px] grid grid-cols-2 gap-4 text-right">
                <div>
                    <span className="block text-[10px] text-slate-400 uppercase font-bold">ETA</span>
                    <span className="text-sm font-bold text-slate-700 font-mono">{rota.eta}</span>
                </div>
                <div>
                    <span className="block text-[10px] text-slate-400 uppercase font-bold">Ping</span>
                    <span className="text-sm font-bold text-slate-700 font-mono flex justify-end items-center gap-1">
                        <RefreshCw size={10} /> {rota.lastPing}
                    </span>
                </div>
              </div>

              {/* AÇÃO */}
              <div className="w-[40px] flex justify-end">
                <div className="h-8 w-8 rounded-full bg-slate-50 flex items-center justify-center group-hover:bg-blue-50 transition-colors">
                    <ChevronRight size={16} className="text-slate-400 group-hover:text-blue-600" />
                </div>
              </div>
            </button>
          ))}
        </div>
      </div>
    </AppShell>
  )
}