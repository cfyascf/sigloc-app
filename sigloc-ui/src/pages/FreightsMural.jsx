import { useState } from "react"
import { Search, MapPin, Clock, Calendar, Box, Truck, ArrowRight, SlidersHorizontal, Filter, AlertCircle } from "lucide-react"
import { Link } from "react-router-dom"
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

export default function FreightsMural() {
  const [searchTerm, setSearchTerm] = useState("")
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
            <p className="text-xs text-slate-500">Encontre fretes compatíveis com sua frota</p>
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
              <Filter size={14} className="mr-1.5 text-slate-500" /> Filtros
            </Button>
          </div>
        </div>

        {/* GRID DE OFERTAS */}
        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              
              {filteredOfertas.map((o) => (
                <div key={o.id} className="group relative flex flex-col rounded-xl border border-slate-200 bg-white shadow-sm hover:shadow-md transition-all">
                  
                  {/* HEADER DO CARD: Contratante + Budget */}
                  <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/50 px-4 py-3">
                    <div>
                      <p className="text-[10px] font-bold uppercase text-slate-400 tracking-wider">{o.contractor}</p>
                      <p className="text-lg font-black text-slate-800">{formatCurrency(o.targetValue)}</p>
                    </div>
                    <div className="flex flex-col items-end gap-1">
                      {getRiskBadge(o.risk)}
                      {o.urgencia === "Alta" && (
                        <Badge variant="destructive" className="bg-rose-50 text-rose-600 border-rose-200 text-[9px]">Urgente</Badge>
                      )}
                    </div>
                  </div>

                  {/* CORPO DO CARD */}
                  <div className="flex flex-col flex-1 p-4">
                    <div className="mb-4">
                      <h3 className="text-sm font-bold text-slate-800 line-clamp-1">{o.segmentName}</h3>
                      <p className="text-xs font-medium text-slate-500 mt-1 flex items-center gap-1.5">
                        <MapPin size={12} className="text-blue-500" /> {o.routeLabel}
                      </p>
                    </div>

                    {/* DETALHES TÉCNICOS */}
                    <div className="grid grid-cols-2 gap-2 mb-4">
                      <div className="rounded bg-slate-50 p-2 border border-slate-100">
                        <p className="text-[10px] font-bold text-slate-400 uppercase">Coleta</p>
                        <p className="text-xs font-semibold text-slate-700">{o.pickupLabel}</p>
                      </div>
                      <div className="rounded bg-slate-50 p-2 border border-slate-100">
                        <p className="text-[10px] font-bold text-slate-400 uppercase">Entrega</p>
                        <p className="text-xs font-semibold text-slate-700">{o.etaLabel}</p>
                      </div>
                    </div>

                    <div className="space-y-2 mb-4">
                        <div className="flex items-center gap-2 text-xs text-slate-600">
                            <Box size={14} className="text-slate-400" />
                            <span>{o.totalWeight} • {o.totalVolume}</span>
                        </div>
                        <div className="flex flex-wrap gap-1">
                            {o.requirements.map(req => (
                                <Badge key={req} variant="secondary" className="bg-slate-100 text-[9px] font-medium text-slate-600">{req}</Badge>
                            ))}
                        </div>
                    </div>

                    {/* AÇÃO PRINCIPAL */}
                    <div className="mt-auto pt-4 border-t border-slate-100">
                      <Button asChild className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold h-9 text-xs">
                        <Link to={`/freight-bid/${o.id}`}>Analisar e Dar Lance <ArrowRight size={14} className="ml-2" /></Link>
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  )
}