import { useState } from "react"
import { useLocation, Link } from "react-router-dom"
import { ArrowLeft, MapPinned, Clock } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"

// Importa os trechos para cruzamento
import { availableRouteSegments } from "./RouteSegmentManagement"

const formatWeight = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} kg`
const formatVolume = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} m³`
const formatDistance = (value) => `${new Intl.NumberFormat("pt-BR").format(value)} km`
const formatCurrency = (value) => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 }).format(value)

function getNodeType(index, totalNodes) {
  if (index === 0) return "Origem"
  if (index === totalNodes - 1) return "Destino final"
  return "Parada intermediária"
}

const buildUnifiedTimeline = (segments) => {
  const nodes = []
  const addAction = (city, action) => {
    const existing = nodes.find((n) => n.city === city)
    if (existing) {
      existing.actions.push(action)
      return
    }
    nodes.push({ city, actions: [action] })
  }

  segments.forEach((s) => {
    addAction(s.origin, `📥 Coleta · ${s.productName} · ${s.id}`)
    addAction(s.destination, `📤 Descarga · ${s.productName} · ${s.id}`)
  })

  return nodes.map((node, idx) => ({
    ...node,
    type: getNodeType(idx, nodes.length),
  }))
}

function getRestrictiveRequirement(requirements) {
  if (requirements.has("Refrigerado")) return "Baú Frigorífico"
  if (requirements.has("Frágil")) return "Carga Sensível"
  return "Carga Seca Padrão"
}

export default function OfferFreight() {
  const location = useLocation()
  const [auctionDeadline, setAuctionDeadline] = useState("")
  const [isAutoAwardEnabled, setIsAutoAwardEnabled] = useState(true)

  const selectedIds = location.state?.selectedIds || []
  const selectedSegments = availableRouteSegments.filter((s) => selectedIds.includes(s.id))
  const unifiedTimeline = buildUnifiedTimeline(selectedSegments)
  
  // Métricas Físicas e Geográficas
  const totalDistance = selectedSegments.reduce((sum, s) => sum + s.distanceKm, 0)
  const maxWeight = selectedSegments.reduce((max, s) => Math.max(max, s.weightKg), 0)
  const maxVolume = selectedSegments.reduce((max, s) => Math.max(max, s.volumeM3), 0)

  // Inteligência Comercial e Logística Real
  const minimumFreightValue = selectedSegments.reduce((sum, s) => sum + (s.targetPrice || s.weightKg * 0.35), 0)
  const estimatedTolls = Math.round(totalDistance * 0.48)
  const costPerKm = totalDistance > 0 ? minimumFreightValue / totalDistance : 0
  const estimatedHours = Math.round(totalDistance / 65) + (unifiedTimeline.length * 1.5)

  const requirementSet = new Set(selectedSegments.flatMap((s) => s.requirements || []))
  const restrictiveRequirement = getRestrictiveRequirement(requirementSet)

  if (selectedIds.length === 0) {
    return (
      <AppShell title="Workspace de Rota">
        <div className="mx-auto max-w-xl rounded-2xl border border-slate-200 bg-white p-6 text-center">
          <p className="text-sm font-semibold text-slate-500">Nenhum trecho foi selecionado para montagem.</p>
          <Button asChild className="mt-4 h-9 bg-slate-900 text-xs text-white">
            <Link to="/route-segment-management">Voltar para listagem</Link>
          </Button>
        </div>
      </AppShell>
    )
  }

  return (
    <AppShell title="Workspace de Rota" contentClassName="overflow-hidden" innerClassName="h-full min-h-0">
      <div className="flex h-full min-h-0 flex-col gap-3 overflow-hidden">
        
        {/* HEADER DA TELA REESTRUTURADO (Visual limpo integrado com Link/Button) */}
        <div className="flex items-center justify-between border-b border-slate-200 pb-3">
          <Link to="/route-segment-management">
            <Button variant="ghost" className="text-slate-500 hover:text-slate-900 h-auto p-0 font-medium text-sm">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de Trechos
            </Button>
          </Link>

          <Badge className="border-none bg-slate-100 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-slate-700">
            {selectedIds.length} trechos selecionados
          </Badge>
        </div>

        {/* SECTION CENTRAL PRINCIPAL */}
        <section className="grid min-h-0 flex-1 gap-3 overflow-hidden p-[1px] xl:grid-cols-[minmax(0,1.45fr)_340px]">
          
          {/* CARD ESQUERDO: TIMELINE E RESUMO MACRO */}
          <div className="flex min-h-0 flex-col overflow-hidden rounded-xl border border-slate-200 bg-white">
            
            {/* Cabeçalho da esquerda com h-[76px] alinhado */}
            <div className="border-b border-slate-200 px-5 bg-white h-[76px] flex items-center">
              <div className="grid gap-2 grid-cols-4 w-full">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">Total Paradas</p>
                  <p className="mt-0.5 text-base font-semibold text-slate-900">{unifiedTimeline.length}</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">Distância Total</p>
                  <p className="mt-0.5 text-base font-semibold text-slate-900">{formatDistance(totalDistance)}</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">Tempo Estimado (ETA)</p>
                  <p className="mt-0.5 text-base font-semibold text-blue-600 font-mono">~ {estimatedHours}h</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">Frete Mínimo</p>
                  <p className="mt-0.5 font-mono text-base font-semibold text-emerald-600">{formatCurrency(minimumFreightValue)}</p>
                </div>
              </div>
            </div>
            
            {/* Linha do tempo centralizada por cidades */}
            <div className="min-h-0 flex-1 overflow-y-auto px-5 pb-5 pt-4">
              <div className="space-y-2.5 pr-1">
                {unifiedTimeline.map((node, index) => (
                  <div key={node.city} className="rounded-xl border border-slate-200 bg-slate-50/40 p-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-blue-50 text-[10px] font-bold text-blue-700 ring-1 ring-blue-200">
                            {index + 1}
                          </span>
                          <span className="text-[10px] font-bold uppercase tracking-[0.16em] text-blue-700">{node.type}</span>
                        </div>
                        <p className="mt-1.5 text-sm font-semibold text-slate-900">{node.city}</p>
                      </div>
                      <MapPinned size={14} className="mt-0.5 text-slate-300" />
                    </div>

                    <div className="mt-2.5 space-y-1">
                      {node.actions.map((action) => (
                        <div
                          key={`${node.city}-${action}`}
                          className="rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-600"
                        >
                          {action}
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* CARD DIREITO: PARÂMETROS FINANCEIROS E REGRAS DO LEILÃO */}
          <div className="flex min-h-0 flex-col justify-between overflow-hidden rounded-xl border border-slate-200 bg-white">
            <div>
              {/* Cabeçalho da direita com h-[76px] alinhado */}
              <div className="border-b border-slate-200 px-5 bg-white h-[76px] flex items-center">
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400">Configuração Comercial</h2>
              </div>
              
              <div className="px-5 pt-4 space-y-4">
                <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-3 space-y-2.5 text-xs">
                  <div className="flex items-center justify-between">
                    <span className="text-slate-500">Capacidade de Cubagem:</span>
                    <span className="font-bold text-slate-800 font-mono">{formatWeight(maxWeight)} • {formatVolume(maxVolume)}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-slate-500">Pedágio Previsto (Vale):</span>
                    <span className="font-bold text-slate-700 font-mono">{formatCurrency(estimatedTolls)}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-slate-500">Custo de Rodagem:</span>
                    <span className="font-bold text-slate-700 font-mono">{formatCurrency(costPerKm)} / km</span>
                  </div>
                  <div className="flex items-center justify-between border-t border-slate-200 pt-2">
                    <span className="text-slate-500">Equipamento Exigido:</span>
                    <Badge variant="outline" className="bg-white text-[10px] font-bold text-slate-700 border-slate-300">{restrictiveRequirement}</Badge>
                  </div>
                </div>

                <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-3">
                  <div className="flex items-center gap-2 text-slate-500 mb-2">
                    <Clock size={13} />
                    <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Encerramento do Leilão</span>
                  </div>
                  <Input
                    id="workspace-deadline"
                    type="datetime-local"
                    value={auctionDeadline}
                    onChange={(e) => setAuctionDeadline(e.target.value)}
                    className="h-9 border-slate-200 bg-white text-xs font-medium focus:ring-blue-500"
                  />
                </div>

                <div className="space-y-2">
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">Adjudicação da rota</p>
                  <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-3">
                    <div className="flex items-start justify-between gap-3">
                      <div className="space-y-1 pr-2">
                        <p className="text-xs font-semibold text-slate-800">Seleção automática do vencedor</p>
                        <p className="text-[11px] text-slate-500 leading-normal">
                          {isAutoAwardEnabled
                            ? "O sistema adjudica o lance mais próximo do frete mínimo ao fim do cronômetro."
                            : "O operador analisa a lista de propostas e seleciona manualmente o transportador."}
                        </p>
                      </div>
                      <Switch checked={isAutoAwardEnabled} onCheckedChange={setIsAutoAwardEnabled} />
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="border-t border-slate-200 bg-slate-50/50 p-4">
              <Button className="h-10 w-full rounded-lg bg-blue-600 text-xs font-bold tracking-wide text-white hover:bg-blue-700 active:bg-blue-800 transition-colors">
                Iniciar Leilão da Rota
              </Button>
            </div>
          </div>

        </section>
      </div>
    </AppShell>
  )
}