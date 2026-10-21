import { Calendar, CheckCircle2, Search, Truck } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

export default function MyTransports() {
  const activeTripsMock = [
    {
      id: "TRP-8821",
      loadRef: "CRG-1042",
      vehicle: "ABC-1234",
      route: "Curitiba, PR → São Paulo, SP",
      status: "Em Trânsito",
      eta: "Hoje, 18:30",
      value: 2150,
    },
    {
      id: "TRP-8822",
      loadRef: "CRG-1043",
      vehicle: "XYZ-9876",
      route: "Joinville, SC → Campinas, SP",
      status: "Carregando",
      eta: "Amanhã, 10:00",
      value: 1800,
    }
  ]

  const historyTripsMock = [
    {
      id: "TRP-8810",
      loadRef: "CRG-1020",
      vehicle: "DEF-5566",
      route: "Araucária, PR → Santos, SP",
      status: "Entregue",
      deliveryDate: "10/05/2026",
      value: 3200,
    },
    {
      id: "TRP-8809",
      loadRef: "CRG-1019",
      vehicle: "ABC-1234",
      route: "Curitiba, PR → Ponta Grossa, PR",
      status: "Entregue",
      deliveryDate: "08/05/2026",
      value: 950,
    }
  ]

  const formatCurrency = (value) => {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
  }

  return (
    <AppShell title="Meus Transportes">
      <div className="mb-6 flex flex-col items-start justify-between space-y-4 md:flex-row md:items-center md:space-y-0">
        <div className="relative w-full md:w-96">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
          <Input 
            type="text" 
            placeholder="Buscar por ID, placa ou rota..." 
            className="border-slate-200 bg-white pl-9 shadow-sm"
          />
        </div>
      </div>

      <Tabs defaultValue="active" className="w-full">
        <TabsList className="mb-4 bg-white border border-slate-200">
          <TabsTrigger value="active" className="data-[state=active]:bg-blue-50 data-[state=active]:text-blue-700">
            Em Andamento (2)
          </TabsTrigger>
          <TabsTrigger value="history" className="data-[state=active]:bg-slate-100">
            Histórico Concluído
          </TabsTrigger>
        </TabsList>

        <TabsContent value="active">
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
            <Table>
              <TableHeader className="bg-slate-50">
                <TableRow>
                  <TableHead className="w-[120px]">ID Transporte</TableHead>
                  <TableHead>Veículo</TableHead>
                  <TableHead>Status Operacional</TableHead>
                  <TableHead>Previsão (ETA)</TableHead>
                  <TableHead className="text-right">Valor do Frete</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {activeTripsMock.map((trip) => (
                  <TableRow key={trip.id} className="hover:bg-slate-50/50">
                    <TableCell>
                      <p className="font-bold text-slate-900">{trip.id}</p>
                      <p className="text-xs text-slate-500">Ref: {trip.loadRef}</p>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className="font-mono bg-slate-50 text-slate-700 border-slate-200">
                        <Truck size={12} className="mr-1 text-slate-400" /> {trip.vehicle}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge className="bg-blue-100 text-blue-800 border-none hover:bg-blue-200">
                        {trip.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-slate-600 text-sm">{trip.eta}</TableCell>
                    <TableCell className="text-right font-bold text-emerald-700">
                      {formatCurrency(trip.value)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </TabsContent>

        <TabsContent value="history">
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
            <Table>
              <TableHeader className="bg-slate-50">
                <TableRow>
                  <TableHead className="w-[120px]">ID Transporte</TableHead>
                  <TableHead>Veículo</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Data de Entrega</TableHead>
                  <TableHead className="text-right">Valor Faturado</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {historyTripsMock.map((trip) => (
                  <TableRow key={trip.id} className="hover:bg-slate-50/50">
                    <TableCell>
                      <p className="font-bold text-slate-900">{trip.id}</p>
                      <p className="text-xs text-slate-500">Ref: {trip.loadRef}</p>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className="font-mono bg-slate-50 text-slate-700 border-slate-200">
                        {trip.vehicle}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary" className="bg-slate-100 text-slate-600 border-none flex w-max items-center">
                        <CheckCircle2 size={12} className="mr-1 text-emerald-500" /> {trip.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-slate-600 text-sm flex items-center">
                      <Calendar size={14} className="mr-2 text-slate-400" /> {trip.deliveryDate}
                    </TableCell>
                    <TableCell className="text-right font-bold text-slate-700">
                      {formatCurrency(trip.value)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </TabsContent>

      </Tabs>
    </AppShell>
  )
}