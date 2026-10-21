import { ArrowLeft, MapPinned, DollarSign, Truck, Scale, Box, Gavel, Layers } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"

// ==========================================
// MOCKS DA ROTA CONSOLIDADA (Substitui os dados de Trecho Único)
// ==========================================
const mockRoute = {
  id: "ROT-9921",
  name: "Rota Sul-Sudeste Consolidada",
  status: "Em Leilão",
  risk: "WARNING",
  // A soma de todos os trechos
  totalWeightKg: 25800,
  totalVolumeM3: 95,
  totalDistanceKm: 854,
  targetFare: 12500, // Teto somado
  bodyType: "Frigorífico (Trailers/Carretas)",
  // As paradas em ordem
  itinerary: [
    { city: "Curitiba, PR", action: "Coleta (2 Trechos)", time: "Hoje, 14:00h" },
    { city: "Joinville, PR", action: "Coleta (1 Trecho)", time: "Hoje, 17:00h" },
    { city: "São Paulo, SP", action: "Entrega Parcial", time: "Amanhã, 08:00h" },
    { city: "Campinas, SP", action: "Entrega Final", time: "Amanhã, 12:00h" },
  ],
  // Os trechos que formam essa rota (O que o planejador juntou no Knapsack)
  segments: [
    { id: "TRC-1042", load: "Frango Congelado", from: "Curitiba", to: "São Paulo", value: 4200 },
    { id: "TRC-1088", load: "Polpa de Fruta", from: "Curitiba", to: "Campinas", value: 5100 },
    { id: "TRC-1090", load: "Sorvetes", from: "Joinville", to: "São Paulo", value: 3200 },
  ],
  auctionInfo: {
    bids: 18,
    bestBid: 11200,
    leader: "Expresso Frio Ltda",
  }
}

const formatWeight = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} kg`
const formatVolume = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} m³`
const formatCurrency = (value) => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value)

function getStopBadgeClass(index, totalStops) {
  if (index === 0) {
    return "bg-slate-800"
  }

  if (index === totalStops - 1) {
    return "bg-emerald-500"
  }

  return "bg-blue-500"
}

export default function SegmentDetails() {
  const navigate = useNavigate()
  const route = mockRoute // Na vida real, busca a rota pelo ID
  
  const anttFloor = route.targetFare * 0.75 // Piso da Rota

  return (
    <AppShell title={`Detalhes da Rota`}>
      {/* Container com scroll blindado e altura calculada em 8.5rem (Sem overflow!) */}
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-5xl flex-col overflow-hidden">
        
        {/* HEADER LIMPO E TEXTUAL */}
        <div className="flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1 mb-5">
          <Button
            type="button"
            variant="ghost"
            className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900"
            onClick={() => navigate(-1)}
          >
            <ArrowLeft size={16} className="mr-2" /> Voltar para a tela anterior
          </Button>
        </div>

        {/* ÁREA DE SCROLL CONTEÚDO (Double Div) */}
        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6 space-y-6">

            {/* HEADER DA ROTA */}
            <div className="flex flex-col md:flex-row md:items-end justify-between gap-4">
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <Layers size={16} className="text-blue-500" />
                  <span className="font-mono text-sm font-bold text-slate-500">{route.id}</span>
                  <Badge variant="secondary" className="border-none bg-slate-100 text-slate-600 text-[10px] font-bold uppercase tracking-wider px-2 py-0">
                    Rota Consolidada
                  </Badge>
                  <Badge variant="outline" className="border-amber-200 bg-amber-50 text-amber-700 text-[10px] font-bold uppercase tracking-wider px-2 py-0">
                    {route.status}
                  </Badge>
                </div>
                <h1 className="text-2xl font-bold tracking-tight text-slate-900">{route.name}</h1>
              </div>
            </div>

            {/* PAINEL MACRO: LOGÍSTICA E FINANCEIRO */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              
              {/* BLOCO 1: Capacidade e Equipamento */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5">
                  <Truck size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Exigência do Caminhão</h2>
                </div>
                <div className="p-5 space-y-4">
                  <div>
                    <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Frota Recomendada</p>
                    <p className="mt-1 text-sm font-bold text-slate-800">{route.bodyType}</p>
                  </div>
                  <div className="grid grid-cols-2 gap-4 border-t border-slate-100 pt-4">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Peso Total Consolidado</p>
                      <p className="mt-1 font-mono text-sm font-semibold text-slate-800 flex items-center gap-1.5"><Scale size={14} className="text-slate-400"/> {formatWeight(route.totalWeightKg)}</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Cubagem Total</p>
                      <p className="mt-1 font-mono text-sm font-semibold text-slate-800 flex items-center gap-1.5"><Box size={14} className="text-slate-400"/> {formatVolume(route.totalVolumeM3)}</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* BLOCO 2: Termômetro Financeiro e Leilão */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5">
                  <DollarSign size={16} className="text-emerald-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Cenário Financeiro da Rota</h2>
                </div>
                <div className="p-5 flex flex-col justify-center h-full space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Teto (Soma dos Trechos)</p>
                      <p className="text-lg font-black font-mono text-slate-800">{formatCurrency(route.targetFare)}</p>
                    </div>
                    <div className="text-right border-l border-slate-100 pl-5">
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Piso Base ANTT</p>
                      <p className="text-base font-bold font-mono text-slate-400">{formatCurrency(anttFloor)}</p>
                    </div>
                  </div>
                  
                  <div className="border-t border-slate-100 pt-4 flex items-center justify-between">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-emerald-600">Melhor Lance ({route.auctionInfo.bids} recebidos)</p>
                      <p className="text-xs font-bold text-slate-700 mt-0.5">{route.auctionInfo.leader}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-lg font-black font-mono text-emerald-600">{formatCurrency(route.auctionInfo.bestBid)}</p>
                    </div>
                  </div>
                </div>
              </div>

            </div>

            {/* BLOCO 3: ITINERÁRIO (O Mapa da Viagem) */}
            <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
              <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/50 px-5 py-3">
                <div className="flex items-center gap-2">
                  <MapPinned size={16} className="text-emerald-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Plano de Viagem (Milking Run)</h2>
                </div>
                <span className="text-xs font-mono font-bold text-slate-400 bg-white px-2 py-1 rounded border border-slate-200">{route.totalDistanceKm} km totais</span>
              </div>
              
              <div className="p-5 flex flex-col md:flex-row gap-6 md:gap-2 justify-between">
                {route.itinerary.map((stop, index) => (
                  <div key={`${stop.city}-${stop.action}`} className="flex-1 relative">
                    {/* Linha conectora (Some no mobile) */}
                    {index !== route.itinerary.length - 1 && (
                      <div className="hidden md:block absolute top-3 left-[50%] w-full border-t-2 border-dashed border-slate-200" />
                    )}
                    
                    <div className="relative z-10 flex flex-col items-center text-center">
                      <div className={`h-6 w-6 rounded-full border-4 border-white flex items-center justify-center text-[10px] font-black text-white ${getStopBadgeClass(index, route.itinerary.length)}`}>
                        {index + 1}
                      </div>
                      <p className="text-xs font-bold text-slate-800 mt-2">{stop.city}</p>
                      <p className="text-[10px] font-semibold text-slate-500">{stop.action}</p>
                      <p className="text-[10px] font-bold text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded mt-1">{stop.time}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* BLOCO 4: OS TRECHOS QUE COMPÕEM A ROTA */}
            <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
              <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/50 px-5 py-3">
                <div className="flex items-center gap-2">
                  <Layers size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Trechos Contidos nesta Rota ({route.segments.length})</h2>
                </div>
              </div>
              <div className="divide-y divide-slate-100">
                {route.segments.map((seg) => (
                  <div key={seg.id} className="flex items-center justify-between px-5 py-3 hover:bg-slate-50/50 transition-colors">
                    <div className="flex items-center gap-4">
                      <span className="font-mono text-xs font-bold text-slate-400">{seg.id}</span>
                      <div>
                        <p className="text-xs font-bold text-slate-800">{seg.load}</p>
                        <p className="text-[10px] font-medium text-slate-500">{seg.from} ➔ {seg.to}</p>
                      </div>
                    </div>
                    <p className="text-xs font-bold font-mono text-slate-600">Teto: {formatCurrency(seg.value)}</p>
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