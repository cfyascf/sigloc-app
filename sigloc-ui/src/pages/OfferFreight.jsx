import { useCallback, useEffect, useMemo, useState } from "react"
import { useLocation, useNavigate, Link } from "react-router-dom"
import {
  ArrowLeft,
  Box,
  Clock,
  DollarSign,
  Gavel,
  Layers,
  Loader2,
  Scale,
  Truck,
} from "lucide-react"

import AppShell from "@/components/app-shell"
import TravelPlan from "@/components/travel-plan"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { FormAlert } from "@/components/auth/FormAlert"
import { useAsyncAction } from "@/hooks/use-async-action"
import { segmentShortId } from "@/constants/route-segments"
import { getRouteSegment } from "@/services/route-segment-service"
import { previewRoute } from "@/services/route-service"
import { createAuction } from "@/services/auction-service"

const numberFormat = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })
const formatWeight = (value) => `${numberFormat.format(value ?? 0)} kg`
const formatVolume = (value) => `${numberFormat.format(value ?? 0)} m³`
const formatCurrency = (value) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    maximumFractionDigits: 0,
  }).format(value ?? 0)

const cityLabel = (value) => (value ? value.split(",")[0] : "—")

/** Builds a de-duplicated, ordered itinerary from the resolved segments. */
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
    const label = s.items?.[0]?.name ?? "Carga"
    const ref = segmentShortId(s.id)
    addAction(s.originCity ?? s.origin, `Coleta · ${label} · ${ref}`)
    addAction(s.destinationCity ?? s.destination, `Descarga · ${label} · ${ref}`)
  })

  return nodes.map((node) => ({ ...node, city: cityLabel(node.city) }))
}

function SectionHeader({ icon: Icon, iconClassName, children }) {
  return (
    <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5">
      <Icon size={16} className={iconClassName} />
      <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">
        {children}
      </h2>
    </div>
  )
}

function BackButton() {
  return (
    <div className="mb-5 flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1">
      <Link to="/route-segment-management">
        <Button
          type="button"
          variant="ghost"
          className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900"
        >
          <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de Trechos
        </Button>
      </Link>
    </div>
  )
}

export default function OfferFreight() {
  const location = useLocation()
  const navigate = useNavigate()

  const segmentIds = useMemo(
    () => location.state?.segmentIds ?? [],
    [location.state]
  )

  const [preview, setPreview] = useState(location.state?.preview ?? null)
  const [segments, setSegments] = useState([])
  const [auctionDeadline, setAuctionDeadline] = useState("")
  const [isAutoAwardEnabled, setIsAutoAwardEnabled] = useState(true)

  const previewAction = useAsyncAction((ids) => previewRoute(ids))
  const segmentsAction = useAsyncAction((ids) =>
    Promise.all(ids.map((id) => getRouteSegment(id)))
  )
  const auctionAction = useAsyncAction((payload) => createAuction(payload))

  const loadContext = useCallback(async () => {
    if (segmentIds.length === 0) {
      return
    }

    if (!preview) {
      const previewResult = await previewAction.run(segmentIds)
      if (previewResult.ok) {
        setPreview(previewResult.data)
      }
    }

    const segmentsResult = await segmentsAction.run(segmentIds)
    if (segmentsResult.ok) {
      setSegments(segmentsResult.data.filter(Boolean))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [segmentIds])

  useEffect(() => {
    loadContext()
  }, [loadContext])

  const unifiedTimeline = useMemo(
    () => buildUnifiedTimeline(segments),
    [segments]
  )

  const handleStartAuction = async () => {
    const result = await auctionAction.run({
      segmentIds,
      expiresAt: auctionDeadline,
      automaticAward: isAutoAwardEnabled,
    })
    if (result.ok) {
      navigate("/route-segment-management")
    }
  }

  if (segmentIds.length === 0) {
    return (
      <AppShell title="Workspace de Rota">
        <div className="mx-auto max-w-xl rounded-xl border border-slate-200 bg-white p-6 text-center">
          <p className="text-sm font-semibold text-slate-500">
            Nenhum trecho foi selecionado para montagem.
          </p>
          <Button asChild className="mt-4 h-9 bg-slate-900 text-xs text-white">
            <Link to="/route-segment-management">Voltar para listagem</Link>
          </Button>
        </div>
      </AppShell>
    )
  }

  const totals = preview?.aggregatedTotals ?? {
    totalWeightKg: 0,
    totalVolumeM3: 0,
  }
  const requirement = preview?.consolidatedVehicleRequirement ?? null
  const contextPending = previewAction.pending || segmentsAction.pending

  return (
    <AppShell title="Workspace de Rota">
      <div className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden">
        <BackButton />

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full space-y-6 overflow-y-auto pb-6 pr-2">
            {previewAction.error ? (
              <FormAlert message={previewAction.error.message} />
            ) : null}
            {segmentsAction.error ? (
              <FormAlert message={segmentsAction.error.message} />
            ) : null}
            {auctionAction.error ? (
              <FormAlert message={auctionAction.error.message} />
            ) : null}

            {/* HEADER DA ROTA */}
            <div>
              <div className="mb-2 flex items-center gap-2">
                <Layers size={16} className="text-blue-500" />
                <Badge
                  variant="secondary"
                  className="border-none bg-slate-100 px-2 py-0 text-[10px] font-bold uppercase tracking-wider text-slate-600"
                >
                  Rota Consolidada
                </Badge>
                <Badge
                  variant="outline"
                  className="border-amber-200 bg-amber-50 px-2 py-0 text-[10px] font-bold uppercase tracking-wider text-amber-700"
                >
                  {segmentIds.length} trechos selecionados
                </Badge>
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">
                Oferecer Frete
              </h1>
            </div>

            {/* ITINERÁRIO (reutiliza o componente de Plano de Viagem) */}
            {contextPending ? (
              <div className="flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white p-10 text-sm text-slate-500">
                <Loader2 size={16} className="animate-spin" /> Carregando
                itinerário...
              </div>
            ) : (
              <TravelPlan
                stops={unifiedTimeline}
                totalDistanceKm={preview?.totalDistanceKm ?? 0}
                emptyMessage="Não foi possível montar o itinerário."
              />
            )}

            <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
              <div className="min-w-0 space-y-6">
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <SectionHeader icon={DollarSign} iconClassName="text-emerald-600">
                  Cenário Financeiro da Rota
                </SectionHeader>
                <div className="flex h-full flex-col justify-center space-y-4 p-5">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                        Teto Consolidado
                      </p>
                      <p className="font-mono text-lg font-black text-slate-800">
                        {formatCurrency(preview?.consolidatedCeiling)}
                      </p>
                    </div>
                    <div className="border-l border-slate-100 pl-5 text-right">
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                        Piso ANTT (est.)
                      </p>
                      <p className="font-mono text-base font-bold text-slate-400">
                        {formatCurrency(preview?.estimatedAnttFloor)}
                      </p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 border-t border-slate-100 pt-4">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                        Pedágio Previsto (Vale)
                      </p>
                      <p className="mt-1 font-mono text-sm font-semibold text-slate-800">
                        {formatCurrency(preview?.estimatedToll)}
                      </p>
                    </div>
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                        Custo de Rodagem
                      </p>
                      <p className="mt-1 font-mono text-sm font-semibold text-slate-800">
                        {formatCurrency(preview?.costPerKm)} / km
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <SectionHeader icon={Truck} iconClassName="text-blue-600">
                  Exigência do Caminhão
                </SectionHeader>
                <div className="space-y-4 p-5">
                  <div>
                    <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                      Equipamento Exigido
                    </p>
                    <p className="mt-1 text-sm font-bold text-slate-800">
                      {requirement?.baseBodyworkType || "—"}
                    </p>
                  </div>
                  <div className="grid grid-cols-2 gap-4 border-t border-slate-100 pt-4">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                        Peso Total Consolidado
                      </p>
                      <p className="mt-1 flex items-center gap-1.5 font-mono text-sm font-semibold text-slate-800">
                        <Scale size={14} className="text-slate-400" />
                        {formatWeight(totals.totalWeightKg)}
                      </p>
                    </div>
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                        Cubagem Total
                      </p>
                      <p className="mt-1 flex items-center gap-1.5 font-mono text-sm font-semibold text-slate-800">
                        <Box size={14} className="text-slate-400" />
                        {formatVolume(totals.totalVolumeM3)}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
              </div>

            {/* CONFIGURAÇÃO DO LEILÃO */}
            <div className="flex flex-col rounded-xl border border-slate-200 bg-white lg:h-full">
              <SectionHeader icon={Gavel} iconClassName="text-blue-600">
                Configuração do Leilão
              </SectionHeader>

              <div className="space-y-6 p-5">
                <div className="space-y-2">
                  <label
                    htmlFor="workspace-deadline"
                    className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400"
                  >
                    <Clock size={12} /> Encerramento do Leilão
                  </label>
                  <Input
                    id="workspace-deadline"
                    type="datetime-local"
                    value={auctionDeadline}
                    onChange={(e) => setAuctionDeadline(e.target.value)}
                    className="h-9 border-slate-200 bg-white text-xs font-medium focus:ring-blue-500"
                  />
                  <p className="text-[11px] text-slate-500">
                    Tempo estimado de viagem: ~{" "}
                    {numberFormat.format(preview?.estimatedTimeHours ?? 0)}h
                  </p>
                </div>

                <div className="space-y-2">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                    Adjudicação da rota
                  </p>
                  <div className="flex items-start justify-between gap-3 rounded-lg border border-slate-200 bg-slate-50/50 p-3">
                    <div className="space-y-1 pr-2">
                      <p className="text-xs font-semibold text-slate-800">
                        Seleção automática do vencedor
                      </p>
                      <p className="text-[11px] leading-normal text-slate-500">
                        {isAutoAwardEnabled
                          ? "O sistema adjudica o lance mais próximo do frete mínimo ao fim do cronômetro."
                          : "O operador analisa a lista de propostas e seleciona manualmente o transportador."}
                      </p>
                    </div>
                    <Switch
                      checked={isAutoAwardEnabled}
                      onCheckedChange={setIsAutoAwardEnabled}
                    />
                  </div>
                </div>
              </div>

              <div className="mt-auto flex justify-end border-t border-slate-100 bg-slate-50/50 px-5 py-3">
                <Button
                  onClick={handleStartAuction}
                  disabled={auctionAction.pending}
                  className="h-10 rounded-lg bg-blue-600 px-6 text-xs font-bold tracking-wide text-white transition-colors hover:bg-blue-700 active:bg-blue-800 disabled:opacity-60"
                >
                  {auctionAction.pending ? (
                    <Loader2 size={14} className="mr-1.5 animate-spin" />
                  ) : null}
                  Iniciar Leilão da Rota
                </Button>
              </div>
            </div>
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  )
}
