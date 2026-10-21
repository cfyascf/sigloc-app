import { useCallback, useEffect, useMemo, useState } from "react"
import { useLocation, useNavigate, Link } from "react-router-dom"
import { ArrowLeft, MapPinned, Clock, Loader2 } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { FormAlert } from "@/components/auth/FormAlert"
import { useAsyncAction } from "@/hooks/use-async-action"
import { getRouteSegment } from "@/services/route-segment-service"
import { previewRoute } from "@/services/route-service"
import { createAuction } from "@/services/auction-service"

const numberFormat = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })
const formatWeight = (value) => `${numberFormat.format(value ?? 0)} kg`
const formatVolume = (value) => `${numberFormat.format(value ?? 0)} m³`
const formatDistance = (value) => `${numberFormat.format(value ?? 0)} km`
const formatCurrency = (value) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    maximumFractionDigits: 0,
  }).format(value ?? 0)

function getNodeType(index, totalNodes) {
  if (index === 0) return "Origem"
  if (index === totalNodes - 1) return "Destino final"
  return "Parada intermediária"
}

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
    const ref = String(s.id).slice(0, 8)
    addAction(s.origin, `📥 Coleta · ${label} · ${ref}`)
    addAction(s.destination, `📤 Descarga · ${label} · ${ref}`)
  })

  return nodes.map((node, idx) => ({
    ...node,
    type: getNodeType(idx, nodes.length),
  }))
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
        <div className="mx-auto max-w-xl rounded-2xl border border-slate-200 bg-white p-6 text-center">
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
    <AppShell
      title="Workspace de Rota"
      contentClassName="overflow-hidden"
      innerClassName="h-full min-h-0"
    >
      <div className="flex h-full min-h-0 flex-col gap-3 overflow-hidden">
        {/* HEADER */}
        <div className="flex items-center justify-between border-b border-slate-200 pb-3">
          <Link to="/route-segment-management">
            <Button
              variant="ghost"
              className="text-slate-500 hover:text-slate-900 h-auto p-0 font-medium text-sm"
            >
              <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de
              Trechos
            </Button>
          </Link>

          <Badge className="border-none bg-slate-100 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-slate-700">
            {segmentIds.length} trechos selecionados
          </Badge>
        </div>

        {previewAction.error ? (
          <FormAlert message={previewAction.error.message} />
        ) : null}
        {segmentsAction.error ? (
          <FormAlert message={segmentsAction.error.message} />
        ) : null}
        {auctionAction.error ? (
          <FormAlert message={auctionAction.error.message} />
        ) : null}

        {/* SECTION CENTRAL PRINCIPAL */}
        <section className="grid min-h-0 flex-1 gap-3 overflow-hidden p-[1px] xl:grid-cols-[minmax(0,1.45fr)_340px]">
          {/* CARD ESQUERDO: TIMELINE E RESUMO MACRO */}
          <div className="flex min-h-0 flex-col overflow-hidden rounded-xl border border-slate-200 bg-white">
            <div className="border-b border-slate-200 px-5 bg-white h-[76px] flex items-center">
              <div className="grid gap-2 grid-cols-4 w-full">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                    Total Paradas
                  </p>
                  <p className="mt-0.5 text-base font-semibold text-slate-900">
                    {unifiedTimeline.length}
                  </p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                    Distância Total
                  </p>
                  <p className="mt-0.5 text-base font-semibold text-slate-900">
                    {formatDistance(preview?.totalDistanceKm)}
                  </p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                    Tempo Estimado (ETA)
                  </p>
                  <p className="mt-0.5 text-base font-semibold text-blue-600 font-mono">
                    ~ {numberFormat.format(preview?.estimatedTimeHours ?? 0)}h
                  </p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                    Piso ANTT (est.)
                  </p>
                  <p className="mt-0.5 font-mono text-base font-semibold text-emerald-600">
                    {formatCurrency(preview?.estimatedAnttFloor)}
                  </p>
                </div>
              </div>
            </div>

            <div className="min-h-0 flex-1 overflow-y-auto px-5 pb-5 pt-4">
              {contextPending ? (
                <div className="flex items-center justify-center gap-2 py-10 text-sm text-slate-500">
                  <Loader2 size={16} className="animate-spin" /> Carregando
                  itinerário...
                </div>
              ) : unifiedTimeline.length === 0 ? (
                <div className="py-10 text-center text-sm text-slate-500">
                  Não foi possível montar o itinerário.
                </div>
              ) : (
                <div className="space-y-2.5 pr-1">
                  {unifiedTimeline.map((node, index) => (
                    <div
                      key={node.city}
                      className="rounded-xl border border-slate-200 bg-slate-50/40 p-3"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-blue-50 text-[10px] font-bold text-blue-700 ring-1 ring-blue-200">
                              {index + 1}
                            </span>
                            <span className="text-[10px] font-bold uppercase tracking-[0.16em] text-blue-700">
                              {node.type}
                            </span>
                          </div>
                          <p className="mt-1.5 text-sm font-semibold text-slate-900">
                            {cityLabel(node.city)}
                          </p>
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
              )}
            </div>
          </div>

          {/* CARD DIREITO: PARÂMETROS FINANCEIROS E REGRAS DO LEILÃO */}
          <div className="flex min-h-0 flex-col justify-between overflow-hidden rounded-xl border border-slate-200 bg-white">
            <div>
              <div className="border-b border-slate-200 px-5 bg-white h-[76px] flex items-center">
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-400">
                  Configuração Comercial
                </h2>
              </div>

              <div className="px-5 pt-4 space-y-4">
                <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-3 space-y-2.5 text-xs">
                  <div className="flex items-center justify-between">
                    <span className="text-slate-500">Capacidade de Cubagem:</span>
                    <span className="font-bold text-slate-800 font-mono">
                      {formatWeight(totals.totalWeightKg)} •{" "}
                      {formatVolume(totals.totalVolumeM3)}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-slate-500">Pedágio Previsto (Vale):</span>
                    <span className="font-bold text-slate-700 font-mono">
                      {formatCurrency(preview?.estimatedToll)}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-slate-500">Custo de Rodagem:</span>
                    <span className="font-bold text-slate-700 font-mono">
                      {formatCurrency(preview?.costPerKm)} / km
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-slate-500">Teto Consolidado:</span>
                    <span className="font-bold text-slate-700 font-mono">
                      {formatCurrency(preview?.consolidatedCeiling)}
                    </span>
                  </div>
                  {requirement ? (
                    <div className="flex items-center justify-between border-t border-slate-200 pt-2">
                      <span className="text-slate-500">Equipamento Exigido:</span>
                      <Badge
                        variant="outline"
                        className="bg-white text-[10px] font-bold text-slate-700 border-slate-300"
                      >
                        {requirement.baseBodyworkType || "—"}
                      </Badge>
                    </div>
                  ) : null}
                </div>

                <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-3">
                  <div className="flex items-center gap-2 text-slate-500 mb-2">
                    <Clock size={13} />
                    <span className="text-xs font-bold uppercase tracking-wider text-slate-400">
                      Encerramento do Leilão
                    </span>
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
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                    Adjudicação da rota
                  </p>
                  <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-3">
                    <div className="flex items-start justify-between gap-3">
                      <div className="space-y-1 pr-2">
                        <p className="text-xs font-semibold text-slate-800">
                          Seleção automática do vencedor
                        </p>
                        <p className="text-[11px] text-slate-500 leading-normal">
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
              </div>
            </div>

            <div className="border-t border-slate-200 bg-slate-50/50 p-4">
              <Button
                onClick={handleStartAuction}
                disabled={auctionAction.pending}
                className="h-10 w-full rounded-lg bg-blue-600 text-xs font-bold tracking-wide text-white hover:bg-blue-700 active:bg-blue-800 transition-colors disabled:opacity-60"
              >
                {auctionAction.pending ? (
                  <Loader2 size={14} className="mr-1.5 animate-spin" />
                ) : null}
                Iniciar Leilão da Rota
              </Button>
            </div>
          </div>
        </section>
      </div>
    </AppShell>
  )
}
