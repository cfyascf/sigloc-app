import { Activity, AlertTriangle, ArrowUpRight, CheckCircle2, DollarSign, Truck } from "lucide-react"
import { Link } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"

export default function CarrierDashboard() {
  const fleetMetrics = {
    totalVehicles: 12,
    inTransit: 8,
    available: 3,
    maintenance: 1,
    utilizationRate: 75 // 75% da frota está rodando
  }

  const financialMetrics = {
    dailyRevenue: 14500.00,
    monthlyRevenue: 245000.00,
    pendingPayments: 32000.00
  }

  const activeTransportsMock = [
    {
      id: "TRP-8821",
      plate: "ABC-1234",
      driver: "Carlos Silva",
      route: "Curitiba → São Paulo",
      status: "No Prazo",
      progress: 60,
      eta: "Hoje, 18:30"
    },
    {
      id: "TRP-8822",
      plate: "XYZ-9876",
      driver: "Roberto Dias",
      route: "Joinville → Campinas",
      status: "Atraso Previsto",
      progress: 35,
      eta: "Amanhã, 10:00"
    },
    {
      id: "TRP-8823",
      plate: "DEF-5566",
      driver: "Ana Souza",
      route: "Ponta Grossa → Santos",
      status: "No Prazo",
      progress: 90,
      eta: "Hoje, 14:15"
    }
  ]

  const formatCurrency = (value) => {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
  }

  return (
    <AppShell title="Dashboard Operacional (Transportadora)">
      
      {/* Metrics Row */}
      <div className="mb-8 grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-4">
        
        <Card className="border-slate-200">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-slate-500">Faturamento Diário</CardTitle>
            <div className="rounded-lg bg-emerald-50 p-2 text-emerald-600">
              <DollarSign size={20} />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-slate-800">{formatCurrency(financialMetrics.dailyRevenue)}</div>
            <p className="mt-1 flex items-center text-xs font-medium text-emerald-600">
              <ArrowUpRight size={14} className="mr-1" /> +12% vs. ontem
            </p>
          </CardContent>
        </Card>

        <Card className="border-slate-200">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-slate-500">Utilização da Frota</CardTitle>
            <div className="rounded-lg bg-blue-50 p-2 text-blue-600">
              <Activity size={20} />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-slate-800">{fleetMetrics.utilizationRate}%</div>
            <Progress value={fleetMetrics.utilizationRate} className="mt-3 h-1.5 bg-slate-100" indicatorColor="bg-blue-600" />
            <p className="mt-2 text-xs text-slate-500">
              {fleetMetrics.inTransit} de {fleetMetrics.totalVehicles} veículos rodando
            </p>
          </CardContent>
        </Card>

        <Card className="border-slate-200">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-slate-500">Veículos Disponíveis</CardTitle>
            <div className="rounded-lg bg-indigo-50 p-2 text-indigo-600">
              <Truck size={20} />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-slate-800">{fleetMetrics.available}</div>
            <p className="mt-1 text-xs text-slate-500">Prontos para alocação</p>
          </CardContent>
        </Card>

        <Card className="border-red-100 relative overflow-hidden">
          <div className="absolute right-0 top-0 -z-10 h-16 w-16 rounded-bl-full bg-red-50"></div>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-slate-500">Alertas Operacionais</CardTitle>
            <div className="rounded-lg bg-red-50 p-2 text-red-600">
              <AlertTriangle size={20} />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-slate-800">2</div>
            <p className="mt-1 text-xs font-medium text-red-600">
              1 atraso • 1 manutenção
            </p>
          </CardContent>
        </Card>

      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        
        {/* Active Transports List */}
        <Card className="col-span-2 border-slate-200">
          <CardHeader className="border-b border-slate-100 pb-4 flex flex-row items-center justify-between">
            <div>
              <CardTitle className="text-base font-bold text-slate-800">Transportes em Andamento</CardTitle>
              <p className="text-xs text-slate-500 mt-1">Acompanhamento de SLA e ETAs</p>
            </div>
            <Button asChild variant="link" className="text-blue-600">
              <Link to="/transport-overview">Ver mapa</Link>
            </Button>
          </CardHeader>
          <CardContent className="p-0">
            <div className="divide-y divide-slate-100">
              {activeTransportsMock.map((transport) => (
                <div key={transport.id} className="flex items-center justify-between p-4 hover:bg-slate-50 transition-colors">
                  <div className="flex items-center space-x-4">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-600">
                      <Truck size={18} />
                    </div>
                    <div>
                      <p className="font-semibold text-slate-900">{transport.plate} <span className="text-slate-400 font-normal text-sm">| {transport.driver}</span></p>
                      <p className="text-xs text-slate-500 mt-0.5">{transport.route}</p>
                    </div>
                  </div>
                  
                  <div className="hidden md:flex flex-col w-32">
                    <div className="flex justify-between text-[10px] uppercase font-semibold text-slate-500 mb-1.5">
                      <span>Progresso</span>
                      <span>{transport.progress}%</span>
                    </div>
                    <Progress value={transport.progress} className="h-1.5 bg-slate-100" indicatorColor={transport.status === "No Prazo" ? "bg-emerald-500" : "bg-amber-500"} />
                  </div>

                  <div className="text-right">
                    <Badge variant={transport.status === "No Prazo" ? "secondary" : "outline"} className={transport.status === "No Prazo" ? "bg-emerald-50 text-emerald-700 hover:bg-emerald-100 border-none" : "border-amber-200 text-amber-700 bg-amber-50"}>
                      {transport.status}
                    </Badge>
                    <p className="text-xs text-slate-500 mt-1.5 font-medium">ETA: {transport.eta}</p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Quick Actions / Status */}
        <Card className="border-slate-200">
          <CardHeader className="border-b border-slate-100 pb-4">
            <CardTitle className="text-base font-bold text-slate-800">Ações Rápidas</CardTitle>
          </CardHeader>
          <CardContent className="pt-4 space-y-4">
            <Button asChild className="w-full justify-start bg-blue-50 text-blue-700 hover:bg-blue-100 border-none shadow-none">
              <Link to="/transport-overview">
                <Truck size={16} className="mr-3" /> Reportar Chegada (Check-in)
              </Link>
            </Button>
            <Button asChild className="w-full justify-start bg-slate-50 text-slate-700 hover:bg-slate-100 border border-slate-200 shadow-none">
              <Link to="/transport-overview">
                <AlertTriangle size={16} className="mr-3 text-amber-500" /> Registrar Ocorrência
              </Link>
            </Button>
            
            <div className="mt-6 pt-6 border-t border-slate-100">
              <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-500 mb-4">Auditoria do Motor</h4>
              <div className="space-y-3">
                <div className="flex items-start">
                  <CheckCircle2 size={16} className="text-emerald-500 mr-2 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-slate-800">Alocação Aprovada</p>
                    <p className="text-xs text-slate-500">Veículo ABC-1234 passou no validador de volume.</p>
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

      </div>
    </AppShell>
  )
}