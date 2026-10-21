import { useState } from "react"
import { Search, Box, SlidersHorizontal, TrendingDown, Clock } from "lucide-react"
import { Link, useSearchParams } from "react-router-dom"
import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { freightOffersMock } from "@/constants/logistics-mock"
import { RISK } from "@/constants/risk"

const formatCurrency = (value) => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(value)

const getRiskBadge = (risk) => {
  const styles = {
    [RISK.NORMAL]: "bg-emerald-50 text-emerald-700 border-emerald-200",
    [RISK.WARNING]: "bg-amber-50 text-amber-700 border-amber-200",
    [RISK.CRITIC]: "bg-rose-50 text-rose-700 border-rose-200",
  }
  return <Badge variant="outline" className={`rounded-full text-[10px] uppercase font-bold tracking-wider px-2 py-0 ${styles[risk] || "bg-slate-50"}`}>{risk}</Badge>
}

const getRequirementBadge = (req) => {
  const normalized = req.toLowerCase();
  if (normalized.includes("frágil")) return "bg-rose-50 text-rose-600 border-rose-200";
  if (normalized.includes("hazmat") || normalized.includes("perigoso")) return "bg-amber-50 text-amber-600 border-amber-200";
  return "bg-slate-100 text-slate-600 border-slate-200";
}

export default function FreightsOffersOverview() {
  const [searchParams] = useSearchParams()
  const [searchTerm, setSearchTerm] = useState(() => searchParams.get("partner") || "")
  const ofertas = freightOffersMock

  const filteredOfertas = ofertas.filter((o) => {
    const term = searchTerm.toLowerCase()
    return o.segmentName.toLowerCase().includes(term) || o.contractor.toLowerCase().includes(term)
  })

  return (
    <AppShell title="Mural de Fretes">
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-7xl flex-col gap-4 overflow-hidden">
        
        {/* BARRA DE TOPO */}
        <div className="flex shrink-0 flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-3 pt-1">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900">Mural de Oportunidades</h1>
          </div>
          
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative w-72">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por cliente ou rota..."
                className="h-9 border-slate-200 bg-white pl-9 text-xs"
              />
            </div>
            <Button variant="outline" className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700">
              <SlidersHorizontal size={14} className="mr-1.5 text-slate-500" /> Filtros
            </Button>
          </div>
        </div>

        {/* GRID DE OFERTAS */}
        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              
              {filteredOfertas.map((o) => {
                const hasBid = !!o.bidStatus;
                
                return (
                  <div key={o.id} className={`group flex flex-col rounded-xl border border-slate-200 bg-white transition-all overflow-hidden ${hasBid ? "border-t-4 border-t-blue-600" : ""}`}>
                    
                    {/* CABEÇALHO */}
                    <div className="bg-slate-50/50 px-4 py-2.5 border-b border-slate-100">
                      <div className="flex justify-between items-center mb-1">
                          <p className="text-[9px] font-bold uppercase text-slate-400 tracking-wider truncate">{o.contractor}</p>
                          <div className="flex gap-1.5 items-center">
                             {getRiskBadge(o.risk)}
                             {/* Badge de Lance Ativo */}
                             {hasBid && (
                                <Badge className="bg-blue-600 text-white text-[9px] font-bold border-none uppercase shadow-none">
                                    Lance Ativo
                                </Badge>
                             )}
                          </div>
                      </div>
                      <p className="text-sm font-bold text-slate-800 leading-tight">{o.routeLabel}</p>
                    </div>

                    {/* CORPO */}
                    <div className="flex flex-col p-4 gap-3">
                      
                      <div className="flex items-start justify-between">
                          <div>
                              <p className="text-[10px] font-bold uppercase text-slate-400">Valor Teto</p>
                              <p className="text-lg font-black text-blue-700">{formatCurrency(o.targetValue)}</p>
                              <p className="text-[10px] text-slate-500 font-medium flex items-center gap-1">
                                  <TrendingDown size={10}/> {o.totalBids || 0} lances ativos
                              </p>
                          </div>
                          {o.isExpiringSoon && (
                            <Badge className="bg-rose-50 text-rose-600 border-rose-200 text-[9px] font-bold flex items-center gap-1">
                                <Clock size={10} /> {o.hoursLeft}H
                            </Badge>
                          )}
                      </div>

                      <div className="grid grid-cols-2 gap-2 text-[11px]">
                          <div className="bg-slate-50 p-2 rounded border border-slate-100">
                              <span className="text-slate-400 block font-bold uppercase text-[9px]">Coleta</span>
                              <span className="font-semibold text-slate-700">{o.pickupLabel}</span>
                          </div>
                          <div className="bg-slate-50 p-2 rounded border border-slate-100">
                              <span className="text-slate-400 block font-bold uppercase text-[9px]">Entrega</span>
                              <span className="font-semibold text-slate-700">{o.etaLabel}</span>
                          </div>
                      </div>

                      <div className="flex flex-col gap-2">
                          <div className="flex items-center gap-2 text-[11px] text-slate-600">
                              <Box size={14} className="text-slate-400" />
                              <span className="font-medium">{o.totalWeight} • {o.totalVolume}</span>
                          </div>
                          
                          <div className="flex flex-wrap gap-1.5">
                              {o.requirements.map((req) => (
                                  <Badge key={req} variant="outline" className={`text-[9px] font-bold uppercase tracking-wide border ${getRequirementBadge(req)}`}>
                                      {req}
                                  </Badge>
                              ))}
                          </div>
                      </div>
                    </div>

                    {/* FOOTER */}
                    <div className="px-4 pb-4">
                      <Button asChild variant={hasBid ? "outline" : "default"} className={`w-full font-bold h-8 text-xs ${
                          hasBid ? "border-slate-300 text-slate-700 hover:bg-slate-50" : "bg-slate-900 hover:bg-slate-800 text-white"
                      }`}>
                        <Link to={`/bid-analysis/${o.segmentId}`}>
                            {hasBid ? "Ver Detalhes do Lance" : "Analisar e Dar Lance"}
                        </Link>
                      </Button>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  )
}