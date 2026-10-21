import { useEffect, useMemo, useState } from "react"
import { AlertCircle, CheckCircle2, FileText } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"

const routesMock = [
  {
    id: "ROT-9921",
    carrier: "Transvale Logística",
    status: "Em Transporte",
    closingValue: 4200,
    itinerary: "Curitiba-PR → São Paulo-SP → Salvador-BA",
    completedStops: 2,
    totalStops: 4,
    timeline: [
      {
        id: "evt-1",
        label: "Veículo em deslocamento para coleta em Curitiba-PR.",
        state: "done",
      },
      {
        id: "evt-2",
        label: "Carga coletada e Nota Fiscal emitida (Trecho 1 iniciado).",
        state: "done",
      },
      {
        id: "evt-3",
        label: "Chegada na Parada 1 (São Paulo-SP) para descarregamento parcial e transbordo.",
        state: "active",
      },
      {
        id: "evt-4",
        label: "Saída de São Paulo em direção ao Destino Final (Salvador-BA).",
        state: "future",
      },
    ],
  },
  {
    id: "ROT-9940",
    carrier: "Rota Sul Transportes",
    status: "Adjudicada",
    closingValue: 3680,
    itinerary: "Joinville-SC → Campinas-SP → Sorocaba-SP",
    completedStops: 1,
    totalStops: 3,
    timeline: [
      {
        id: "evt-5",
        label: "Transportadora confirmada e veículo alocado para saída da base.",
        state: "done",
      },
      {
        id: "evt-6",
        label: "Chegada prevista ao ponto de coleta em Joinville-SC.",
        state: "active",
      },
      {
        id: "evt-7",
        label: "Partida para Campinas-SP com janelas de entrega validadas.",
        state: "future",
      },
    ],
  },
  {
    id: "ROT-9962",
    carrier: "Atlântico Cargo",
    status: "Em Transporte",
    closingValue: 4680,
    itinerary: "Maringá-PR → Guarulhos-SP → Serra-ES",
    completedStops: 2,
    totalStops: 5,
    timeline: [
      {
        id: "evt-8",
        label: "Coleta concluída no hub de Maringá-PR.",
        state: "done",
      },
      {
        id: "evt-9",
        label: "Checkpoint de rastreamento confirmado no corredor PR-SP.",
        state: "done",
      },
      {
        id: "evt-10",
        label: "Chegada em Guarulhos-SP para consolidação do segundo trecho.",
        state: "active",
      },
      {
        id: "evt-11",
        label: "Saída interestadual rumo a Serra-ES.",
        state: "future",
      },
      {
        id: "evt-12",
        label: "Entrega final e baixa de comprovantes no destino.",
        state: "future",
      },
    ],
  },
]

function formatCurrency(value) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value)
}

function getStatusBadgeClass(status) {
  if (status === "Em Transporte") {
    return "bg-sky-100 text-sky-800 hover:bg-sky-100"
  }

  if (status === "Adjudicada") {
    return "bg-amber-100 text-amber-800 hover:bg-amber-100"
  }

  return "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"
}

function getTimelineStyles(state) {
  if (state === "done") {
    return {
      line: "bg-emerald-200",
      dot: "border-emerald-200 bg-emerald-50 text-emerald-700",
      text: "text-slate-800",
      icon: CheckCircle2,
    }
  }

  if (state === "active") {
    return {
      line: "bg-sky-200",
      dot: "border-sky-200 bg-sky-50 text-sky-700",
      text: "text-slate-900",
      icon: AlertCircle,
    }
  }

  return {
    line: "bg-slate-200",
    dot: "border-slate-200 bg-slate-50 text-slate-500",
    text: "text-slate-500",
    icon: FileText,
  }
}

function finalizeTimeline(events) {
  return events.map((event) => ({
    ...event,
    state: "done",
  }))
}

function finalizeRoute(route) {
  return {
    ...route,
    status: "Finalizada",
    completedStops: route.totalStops,
    timeline: finalizeTimeline(route.timeline),
  }
}

export default function ControlTower() {
  const [routes, setRoutes] = useState(routesMock)
  const [selectedRouteId, setSelectedRouteId] = useState(routesMock[0]?.id ?? null)

  const activeRoutes = useMemo(
    () => routes.filter((route) => route.status === "Em Transporte" || route.status === "Adjudicada"),
    [routes],
  )

  useEffect(() => {
    if (!selectedRouteId && activeRoutes.length > 0) {
      setSelectedRouteId(activeRoutes[0].id)
      return
    }

    if (selectedRouteId && !activeRoutes.some((route) => route.id === selectedRouteId)) {
      setSelectedRouteId(activeRoutes[0]?.id ?? null)
    }
  }, [activeRoutes, selectedRouteId])

  const selectedRoute = activeRoutes.find((route) => route.id === selectedRouteId) ?? null

  const handlePodUpload = () => {
    if (!selectedRoute) {
      return
    }

    setRoutes((currentRoutes) =>
      currentRoutes.map((route) => (route.id === selectedRoute.id ? finalizeRoute(route) : route)),
    )
  }

  return (
    <AppShell title="Torre de Controle - Monitoramento de Execução">
      <div className="mx-auto grid max-w-7xl gap-4 xl:grid-cols-[380px_minmax(0,1fr)]">
        <Card className="border-slate-200 bg-white shadow-sm xl:sticky xl:top-6 xl:self-start">
          <CardHeader className="border-b border-slate-100 pb-5">
            <CardTitle className="text-lg font-semibold text-slate-900">Rotas ativas</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 pt-5">
            {activeRoutes.map((route) => {
              const progressValue = Math.round((route.completedStops / route.totalStops) * 100)
              const isSelected = route.id === selectedRouteId

              return (
                <button
                  key={route.id}
                  type="button"
                  onClick={() => setSelectedRouteId(route.id)}
                  className={`w-full rounded-2xl border p-4 text-left transition-all ${
                    isSelected
                      ? "border-sky-300 bg-sky-50/70 shadow-sm"
                      : "border-slate-200 bg-slate-50/50 hover:border-slate-300 hover:bg-white"
                  }`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-mono text-sm font-semibold uppercase tracking-[0.2em] text-slate-500">
                        {route.id}
                      </p>
                      <p className="mt-1 text-sm font-medium text-slate-900">{route.carrier}</p>
                    </div>
                    <Badge className={`border-none ${getStatusBadgeClass(route.status)}`}>{route.status}</Badge>
                  </div>

                  <p className="mt-3 text-sm text-slate-500">{route.itinerary}</p>

                  <div className="mt-4 grid grid-cols-2 gap-3 rounded-xl border border-slate-100 bg-white p-3 text-sm">
                    <div>
                      <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-slate-400">Frete</p>
                      <p className="mt-1 font-semibold text-slate-900">{formatCurrency(route.closingValue)}</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-slate-400">Paradas</p>
                      <p className="mt-1 font-semibold text-slate-900">{route.completedStops}/{route.totalStops}</p>
                    </div>
                  </div>

                  <div className="mt-4 space-y-2">
                    <div className="flex items-center justify-between text-xs font-medium uppercase tracking-[0.16em] text-slate-400">
                      <span>Conclusão da viagem</span>
                      <span>{progressValue}%</span>
                    </div>
                    <Progress value={progressValue} className="h-2 bg-slate-200" />
                  </div>
                </button>
              )
            })}

            {activeRoutes.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-8 text-center text-sm text-slate-500">
                Não há rotas ativas em execução neste momento.
              </div>
            ) : null}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card className="border-slate-200 bg-white shadow-sm">
            <CardHeader className="border-b border-slate-100 pb-5">
              <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <CardTitle className="text-lg font-semibold text-slate-900">Linha do tempo da execução</CardTitle>
                </div>
                {selectedRoute ? (
                  <Badge className={`w-fit border-none ${getStatusBadgeClass(selectedRoute.status)}`}>
                    {selectedRoute.status}
                  </Badge>
                ) : null}
              </div>
            </CardHeader>

            <CardContent className="space-y-6 pt-6">
              {selectedRoute ? (
                <>
                  <div className="grid gap-4 rounded-2xl border border-slate-200 bg-slate-50 p-4 md:grid-cols-3">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-400">Rota</p>
                      <p className="mt-2 font-mono text-sm font-semibold text-slate-900">{selectedRoute.id}</p>
                    </div>
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-400">Transportadora</p>
                      <p className="mt-2 text-sm font-medium text-slate-900">{selectedRoute.carrier}</p>
                    </div>
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-400">Frete fechado</p>
                      <p className="mt-2 text-sm font-semibold text-slate-900">{formatCurrency(selectedRoute.closingValue)}</p>
                    </div>
                  </div>

                  <div className="space-y-5">
                    {selectedRoute.timeline.map((event, index) => {
                      const styles = getTimelineStyles(event.state)
                      const Icon = styles.icon
                      const showLine = index < selectedRoute.timeline.length - 1

                      return (
                        <div key={event.id} className="flex gap-4">
                          <div className="flex flex-col items-center">
                            <div className={`flex h-11 w-11 items-center justify-center rounded-full border ${styles.dot}`}>
                              <Icon size={18} />
                            </div>
                            {showLine ? <div className={`mt-2 h-full w-px ${styles.line}`} /> : null}
                          </div>
                          <div className="pb-6 pt-2">
                            <p className={`text-sm font-medium leading-6 ${styles.text}`}>{event.label}</p>
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </>
              ) : (
                <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-10 text-center text-sm text-slate-500">
                  Selecione uma rota ativa para visualizar a linha do tempo do transporte.
                </div>
              )}
            </CardContent>
          </Card>

          <Card className="border-slate-200 bg-white shadow-sm">
            <CardHeader className="border-b border-slate-100 pb-5">
              <CardTitle className="text-lg font-semibold text-slate-900">Comprovação de entrega</CardTitle>
            </CardHeader>
            <CardContent className="pt-6">
              <label className="flex cursor-pointer flex-col items-center justify-center rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-6 py-10 text-center transition-colors hover:border-slate-400 hover:bg-slate-100/60">
                <input type="file" className="hidden" onChange={handlePodUpload} />
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-white text-slate-600 shadow-sm">
                  <FileText size={20} />
                </div>
                <p className="mt-4 text-sm font-medium text-slate-900">
                  Selecionar comprovante assinado
                </p>
                <Button type="button" variant="outline" className="mt-5 border-slate-200 bg-white">
                  Enviar comprovante
                </Button>
              </label>
            </CardContent>
          </Card>
        </div>
      </div>
    </AppShell>
  )
}