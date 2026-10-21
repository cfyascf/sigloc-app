import { useRef, useState } from "react"
import { AlertTriangle, ArrowLeft, Boxes, ChevronRight, MapPin, Plus, Route, Save, Sparkles, Trash2, Workflow } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { cargoItemsMock } from "@/constants/logistics-mock"

const initialStops = [
  { id: "stop-1", value: "Curitiba, PR" },
  { id: "stop-2", value: "São Paulo, SP" },
  { id: "stop-3", value: "Salvador, BA" },
]

function getStepBadgeClass(isActive, isDone) {
  if (isActive) {
    return "bg-sky-600 text-white"
  }

  if (isDone) {
    return "bg-emerald-100 text-emerald-700"
  }

  return "bg-slate-100 text-slate-500"
}

export default function CreateRoute() {
  const navigate = useNavigate()
  const nextStopIdRef = useRef(initialStops.length + 1)
  const [segmentName, setSegmentName] = useState("Curitiba → São Paulo → Salvador")
  const [plannerNote, setPlannerNote] = useState("Consolidar coleta refrigerada na 1ª perna e manter janela urbana na transferência.")
  const [bidDeadline, setBidDeadline] = useState("2026-05-18T18:00")
  const [targetFare, setTargetFare] = useState("9800")
  const [stops, setStops] = useState(initialStops)
  const [selectedItemIds, setSelectedItemIds] = useState(["CRG-1042", "CRG-1043"])
  const [currentStep, setCurrentStep] = useState(0)

  const steps = [
    { id: "identidade", label: "Identidade", helper: "nome, paradas e contexto" },
    { id: "composicao", label: "Composição", helper: "itens e compatibilidade" },
    { id: "publicacao", label: "Publicação", helper: "tarifa e prazo" },
  ]

  const selectedItems = cargoItemsMock.filter((item) => selectedItemIds.includes(item.id))
  const suggestedFloor = selectedItems.reduce((total, item) => total + item.freightValue, 0)
  const filledStopsCount = stops.filter((stop) => stop.value.trim()).length
  const hasWarning = filledStopsCount < 2 || selectedItems.length === 0

  const updateStop = (index, value) => {
    setStops((currentStops) => currentStops.map((stop, stopIndex) => (stopIndex === index ? { ...stop, value } : stop)))
  }

  const addStop = () => {
    const nextId = `stop-${nextStopIdRef.current}`
    nextStopIdRef.current += 1
    setStops((currentStops) => [...currentStops, { id: nextId, value: "" }])
  }

  const removeStop = (index) => {
    setStops((currentStops) => currentStops.filter((_, stopIndex) => stopIndex !== index))
  }

  const toggleItem = (cargoId) => {
    setSelectedItemIds((currentIds) => (
      currentIds.includes(cargoId)
        ? currentIds.filter((id) => id !== cargoId)
        : [...currentIds, cargoId]
    ))
  }

  return (
    <AppShell title="Planejamento de Novo Trecho">
      <div className="mx-auto max-w-6xl space-y-6">
        <div className="flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
          <Link to="/load-management">
            <Button variant="ghost" className="text-slate-500 hover:text-slate-900">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de Trechos
            </Button>
          </Link>
          <div className="flex flex-wrap gap-3">
            <Button asChild variant="outline" className="border-slate-200">
              <Link to="/load-management">Cancelar</Link>
            </Button>
            <Button className="bg-sky-700 text-white hover:bg-sky-800" onClick={() => navigate("/load-management")}>
              <Save size={16} className="mr-2" /> Salvar Trecho
            </Button>
          </div>
        </div>

        <section className="planner-hero overflow-hidden rounded-[28px] p-6 text-white shadow-lg shadow-sky-950/10 md:p-8">
          <div className="grid gap-6 lg:grid-cols-[1.5fr_1fr] lg:items-end">
            <div className="space-y-4">
              <Badge className="border border-white/20 bg-white/12 text-white hover:bg-white/15">Fluxo guiado do planejador</Badge>
              <div className="space-y-3">
                <h1 className="max-w-3xl text-3xl font-semibold tracking-tight md:text-4xl">
                  Monte um trecho pronto para negociação, com clareza operacional desde a primeira parada.
                </h1>
                <p className="max-w-2xl text-sm text-sky-50/85 md:text-base">
                  A tela segue o raciocínio natural do planejador: definir identidade, validar composição e só então fechar a estratégia de publicação.
                </p>
              </div>
            </div>

            <div className="glass-panel rounded-3xl border border-white/20 p-5 text-slate-900">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Prontidão</p>
              <div className="mt-3 grid gap-3 sm:grid-cols-3 lg:grid-cols-1 xl:grid-cols-3">
                <div>
                  <p className="text-2xl font-semibold text-slate-900">{filledStopsCount}</p>
                  <p className="text-xs text-slate-500">paradas</p>
                </div>
                <div>
                  <p className="text-2xl font-semibold text-slate-900">{selectedItems.length}</p>
                  <p className="text-xs text-slate-500">itens</p>
                </div>
                <div>
                  <p className="text-2xl font-semibold text-slate-900">
                    {Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(suggestedFloor)}
                  </p>
                  <p className="text-xs text-slate-500">referência</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className="glass-panel rounded-[24px] border border-white/60 p-4 shadow-sm">
          <div className="grid gap-3 md:grid-cols-3">
            {steps.map((step, index) => {
              const isActive = currentStep === index
              const isDone = currentStep > index

              return (
                <button
                  key={step.id}
                  type="button"
                  aria-current={isActive ? "step" : undefined}
                  onClick={() => setCurrentStep(index)}
                  className={`focus-ring-strong rounded-2xl border px-4 py-4 text-left transition-colors ${isActive ? "border-sky-300 bg-white shadow-sm" : "border-slate-200/80 bg-white/70 hover:bg-white"}`}
                >
                  <div className="flex items-center gap-3">
                    <span className={`flex h-9 w-9 items-center justify-center rounded-full text-sm font-semibold ${getStepBadgeClass(isActive, isDone)}`}>
                      {index + 1}
                    </span>
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{step.label}</p>
                      <p className="text-xs text-slate-500">{step.helper}</p>
                    </div>
                  </div>
                </button>
              )
            })}
          </div>
        </section>

        {hasWarning && (
          <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
            <div className="flex items-start gap-3">
              <AlertTriangle size={18} className="mt-0.5 text-amber-600" />
              <div>
                <p className="font-semibold">Atenção operacional</p>
                <p className="mt-1 text-amber-800/90">
                  Para um trecho ficar pronto para leilão, ele precisa de pelo menos duas paradas válidas e uma composição mínima de itens.
                </p>
              </div>
            </div>
          </div>
        )}

        <div className="grid gap-6 lg:grid-cols-3">
          <div className="space-y-6 lg:col-span-2">
            {currentStep === 0 && (
              <Card className="glass-panel gap-0 border-white/60 py-0 shadow-sm">
                <CardHeader className="border-b border-slate-200/70 pb-5">
                  <CardTitle className="flex items-center text-lg font-bold text-slate-800">
                    <Route size={18} className="mr-2 text-blue-600" /> Identidade do Trecho
                  </CardTitle>
                  <CardDescription>
                    Defina a narrativa operacional do trecho antes de entrar na lógica comercial.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6 pt-6">
                  <div className="space-y-2">
                    <label htmlFor="segment-name" className="text-sm font-medium text-slate-700">Nome do trecho</label>
                    <Input id="segment-name" value={segmentName} onChange={(e) => setSegmentName(e.target.value)} className="focus-ring-strong border-slate-200 bg-white/90" />
                  </div>

                  <div className="space-y-3">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-sm font-medium text-slate-700">Paradas do trecho</p>
                      <Button type="button" variant="outline" size="sm" className="border-slate-200 bg-white" onClick={addStop}>
                        <Plus size={14} className="mr-1" /> Adicionar parada
                      </Button>
                    </div>
                    {stops.map((stop, index) => (
                      <div key={stop.id} className="flex items-center gap-3">
                        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-sky-50 text-xs font-semibold text-sky-700">
                          {index + 1}
                        </div>
                        <div className="relative flex-1">
                          <MapPin size={16} className="absolute left-3 top-2.5 text-slate-400" />
                          <Input
                            value={stop.value}
                            onChange={(e) => updateStop(index, e.target.value)}
                            className="focus-ring-strong border-slate-200 bg-white/90 pl-9"
                            placeholder={`Parada ${index + 1}`}
                          />
                        </div>
                        {stops.length > 2 && (
                          <Button type="button" variant="ghost" size="icon" className="text-slate-500 hover:bg-rose-50 hover:text-rose-600" onClick={() => removeStop(index)}>
                            <Trash2 size={16} />
                          </Button>
                        )}
                      </div>
                    ))}
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="segment-note" className="text-sm font-medium text-slate-700">Nota do planejador</label>
                    <textarea
                      id="segment-note"
                      value={plannerNote}
                      onChange={(e) => setPlannerNote(e.target.value)}
                      className="focus-ring-strong min-h-[140px] w-full rounded-2xl border border-slate-200 bg-white/90 px-4 py-3 text-sm text-slate-700 outline-none"
                    />
                  </div>

                  <div className="flex justify-end">
                    <Button type="button" className="bg-sky-700 text-white hover:bg-sky-800" onClick={() => setCurrentStep(1)}>
                      Próximo passo <ChevronRight size={16} className="ml-2" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            )}

            {currentStep === 1 && (
              <Card className="glass-panel gap-0 border-white/60 py-0 shadow-sm">
                <CardHeader className="border-b border-slate-200/70 pb-5">
                  <CardTitle className="flex items-center text-lg font-bold text-slate-800">
                    <Boxes size={18} className="mr-2 text-blue-600" /> Itens que entram no trecho
                  </CardTitle>
                  <CardDescription>
                    Selecione apenas as cargas que realmente compartilham sequência logística e janela operacional.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-5 pt-6">
                  <div className="grid gap-3 md:grid-cols-2">
                    {cargoItemsMock.map((cargo) => {
                      const isSelected = selectedItemIds.includes(cargo.id)

                      return (
                        <button
                          key={cargo.id}
                          type="button"
                          aria-pressed={isSelected}
                          onClick={() => toggleItem(cargo.id)}
                          className={`focus-ring-strong rounded-2xl border p-4 text-left transition-colors ${isSelected ? "border-sky-300 bg-sky-50 shadow-sm" : "border-slate-200 bg-white/90 hover:bg-white"}`}
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <p className="text-sm font-semibold text-slate-900">{cargo.id} • {cargo.productName}</p>
                              <p className="mt-1 text-xs text-slate-500">{cargo.routeLabel}</p>
                            </div>
                            <Badge className={isSelected ? "bg-sky-700 text-white" : "bg-slate-100 text-slate-700"}>
                              {isSelected ? "Selecionado" : cargo.status}
                            </Badge>
                          </div>
                          <div className="mt-3 flex flex-wrap gap-2 text-xs text-slate-500">
                            <span>{cargo.quantityLabel}</span>
                            <span>{cargo.weight}</span>
                            <span>{cargo.volume}</span>
                          </div>
                        </button>
                      )
                    })}
                  </div>

                  <div className="flex flex-col gap-3 border-t border-slate-200 pt-5 md:flex-row md:items-center md:justify-between">
                    <p className="text-sm text-slate-600">Valide primeiro a coerência da composição. O preço vem depois.</p>
                    <div className="flex gap-3">
                      <Button type="button" variant="outline" className="border-slate-200 bg-white" onClick={() => setCurrentStep(0)}>
                        Voltar
                      </Button>
                      <Button type="button" className="bg-sky-700 text-white hover:bg-sky-800" onClick={() => setCurrentStep(2)}>
                        Próximo passo <ChevronRight size={16} className="ml-2" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}

            {currentStep === 2 && (
              <Card className="glass-panel gap-0 border-white/60 py-0 shadow-sm">
                <CardHeader className="border-b border-slate-200/70 pb-5">
                  <CardTitle className="flex items-center text-lg font-bold text-slate-800">
                    <Sparkles size={18} className="mr-2 text-blue-600" /> Estratégia de publicação
                  </CardTitle>
                  <CardDescription>
                    Feche o posicionamento comercial do trecho com base na composição já definida.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6 pt-6">
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <label htmlFor="segment-target-fare" className="text-sm font-medium text-slate-700">Tarifa-alvo do trecho</label>
                      <Input id="segment-target-fare" type="number" value={targetFare} onChange={(e) => setTargetFare(e.target.value)} className="focus-ring-strong border-slate-200 bg-white/90" />
                      <p className="text-xs text-slate-500">
                        Referência atual da composição: {Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(suggestedFloor)}.
                      </p>
                    </div>
                    <div className="space-y-2">
                      <label htmlFor="segment-deadline" className="text-sm font-medium text-slate-700">Prazo sugerido para publicação</label>
                      <Input id="segment-deadline" type="datetime-local" value={bidDeadline} onChange={(e) => setBidDeadline(e.target.value)} className="focus-ring-strong border-slate-200 bg-white/90" />
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-200 bg-white/90 p-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Checklist de prontidão</p>
                    <div className="mt-4 grid gap-3 md:grid-cols-3">
                      <div className="rounded-xl bg-slate-50 p-4">
                        <p className="text-sm font-semibold text-slate-800">Paradas definidas</p>
                        <p className="mt-1 text-sm text-slate-500">{filledStopsCount >= 2 ? "OK para sequência" : "Faltam pontos"}</p>
                      </div>
                      <div className="rounded-xl bg-slate-50 p-4">
                        <p className="text-sm font-semibold text-slate-800">Composição</p>
                        <p className="mt-1 text-sm text-slate-500">{selectedItems.length > 0 ? `${selectedItems.length} itens vinculados` : "Nenhum item selecionado"}</p>
                      </div>
                      <div className="rounded-xl bg-slate-50 p-4">
                        <p className="text-sm font-semibold text-slate-800">Publicação</p>
                        <p className="mt-1 text-sm text-slate-500">{bidDeadline ? "Prazo definido" : "Sem prazo"}</p>
                      </div>
                    </div>
                  </div>

                  <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                    <Button type="button" variant="outline" className="border-slate-200 bg-white" onClick={() => setCurrentStep(1)}>
                      Voltar
                    </Button>
                    <Button className="bg-sky-700 text-white hover:bg-sky-800" onClick={() => navigate("/load-management")}>
                      <Save size={16} className="mr-2" /> Concluir planejamento
                    </Button>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>

          <div className="space-y-6">
            <Card className="glass-panel gap-0 border-white/60 py-0 shadow-sm">
              <CardHeader className="border-b border-slate-200/70">
                <CardTitle className="flex items-center gap-2 text-base font-bold text-slate-800">
                  <Sparkles size={18} className="text-blue-600" /> Resumo operacional
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4 p-6">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Trecho</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{segmentName}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Paradas ativas</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{filledStopsCount} pontos</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Itens selecionados</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{selectedItems.length} itens</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Soma de referência</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">
                    {Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(suggestedFloor)}
                  </p>
                </div>
              </CardContent>
            </Card>

            <Card className="glass-panel gap-0 border-white/60 py-0 shadow-sm">
              <CardHeader className="border-b border-slate-200/70">
                <CardTitle className="flex items-center gap-2 text-base font-bold text-slate-800">
                  <Workflow size={18} className="text-blue-600" /> Lógica do planejador
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 p-6 text-sm text-slate-600">
                <p>1. Agrupe itens que compartilham fluxo e janela de operação.</p>
                <p>2. Estruture o trecho com clareza de sequência e transferência.</p>
                <p>3. Só depois publique o leilão com visibilidade total da composição.</p>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </AppShell>
  )
}
