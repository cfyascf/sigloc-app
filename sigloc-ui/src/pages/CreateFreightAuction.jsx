import { useState } from "react"
import { ArrowLeft, CalendarRange, Gavel, MapPin, Package, Plus, Route, Workflow } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { cargoItemsMock, getSegmentById, segmentPlansMock } from "@/constants/logistics-mock"

export default function CreateFreightAuction() {
  const navigate = useNavigate()
  const [selectedSegmentId, setSelectedSegmentId] = useState("TRC-201")
  const [minimumBid, setMinimumBid] = useState("")
  const [auctionDeadline, setAuctionDeadline] = useState("")
  const [auctionDuration, setAuctionDuration] = useState("24h")
  const [notes, setNotes] = useState("")
  const [isPublished, setIsPublished] = useState(false)

  const selectedSegment = getSegmentById(selectedSegmentId)
  const selectedItems = cargoItemsMock.filter((item) => selectedSegment?.items.includes(item.id))

  const handlePublish = () => {
    setIsPublished(true)
    navigate("/freights-panel")
  }

  return (
    <AppShell title="Publicar Trecho em Leilão">
      <div className="mx-auto max-w-6xl space-y-6">
        <div className="flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
          <Link to="/freights-panel">
            <Button variant="ghost" className="text-slate-500 hover:text-slate-900">
              <ArrowLeft size={16} className="mr-2" /> Voltar para o Painel
            </Button>
          </Link>

          <div className="flex flex-wrap gap-3">
            <Button variant="outline" className="border-slate-200 bg-white text-slate-700" onClick={() => navigate("/freights-panel") }>
              Cancelar
            </Button>
            <Button className="bg-blue-600 text-white hover:bg-blue-700" onClick={handlePublish}>
              <Gavel size={16} className="mr-2" /> Publicar Leilão
            </Button>
          </div>
        </div>

        <div className="grid gap-6 lg:grid-cols-3">
          <Card className="gap-0 border-slate-200 py-0 shadow-sm lg:col-span-2">
            <CardHeader className="border-b border-slate-100 bg-slate-50/70 py-3">
              <CardTitle className="flex items-center gap-2 text-lg font-bold text-slate-800">
                <Gavel size={18} className="text-blue-600" /> Dados do Leilão
              </CardTitle>
              <CardDescription>
                Selecione o trecho planejado e defina as condições comerciais da publicação.
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6 p-6">
              <div className="space-y-2">
                <label htmlFor="auction-segment" className="text-sm font-medium text-slate-700">Trecho base</label>
                <Select value={selectedSegmentId} onValueChange={setSelectedSegmentId}>
                  <SelectTrigger id="auction-segment" className="border-slate-200 bg-white">
                    <SelectValue placeholder="Selecione o trecho para o leilão" />
                  </SelectTrigger>
                  <SelectContent>
                    {segmentPlansMock.map((segment) => (
                      <SelectItem key={segment.id} value={segment.id}>
                        <div className="flex items-center gap-2">
                          <Route size={14} className="text-slate-500" />
                          {segment.id} - {segment.name}
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {selectedSegment && (
                <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge className="bg-blue-100 text-blue-800">{selectedSegment.id}</Badge>
                    <Badge variant="outline" className="border-slate-200 bg-white text-slate-600">{selectedSegment.itemCount} itens</Badge>
                    <Badge variant="outline" className="border-slate-200 bg-white text-slate-600">{selectedSegment.legCount} pernas</Badge>
                  </div>
                  <div className="mt-3 space-y-2 text-sm text-slate-700">
                    <p className="font-semibold text-slate-900">{selectedSegment.name}</p>
                    <p className="flex items-center gap-2"><MapPin size={14} className="text-slate-400" /> {selectedSegment.stops.join(" → ")}</p>
                    <p className="flex items-center gap-2"><Package size={14} className="text-slate-400" /> {selectedSegment.totalWeight} • {selectedSegment.totalVolume}</p>
                  </div>
                </div>
              )}

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <label htmlFor="minimum-bid" className="text-sm font-medium text-slate-700">Lance mínimo inicial (R$)</label>
                  <Input
                    id="minimum-bid"
                    type="number"
                    value={minimumBid}
                    onChange={(e) => setMinimumBid(e.target.value)}
                    placeholder="Ex: 1800"
                    className="border-slate-200"
                  />
                </div>

                <div className="space-y-2">
                  <label htmlFor="auction-duration" className="text-sm font-medium text-slate-700">Duração do leilão</label>
                  <Select value={auctionDuration} onValueChange={setAuctionDuration}>
                    <SelectTrigger id="auction-duration" className="border-slate-200 bg-white">
                      <SelectValue placeholder="Selecione a duração" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="12h">12 horas</SelectItem>
                      <SelectItem value="24h">24 horas</SelectItem>
                      <SelectItem value="48h">48 horas</SelectItem>
                      <SelectItem value="72h">72 horas</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="auction-deadline" className="text-sm font-medium text-slate-700">Encerramento do leilão</label>
                <div className="relative">
                  <CalendarRange size={16} className="absolute left-3 top-3 text-slate-400" />
                  <Input
                    id="auction-deadline"
                    type="datetime-local"
                    value={auctionDeadline}
                    onChange={(e) => setAuctionDeadline(e.target.value)}
                    className="border-slate-200 pl-9"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="auction-notes" className="text-sm font-medium text-slate-700">Observações</label>
                <textarea
                  id="auction-notes"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="Ex: transportar preferencialmente com veículo baú refrigerado, atender janela curta de coleta..."
                  className="min-h-[140px] w-full rounded-md border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none focus:border-slate-300"
                />
              </div>

              <div className="rounded-xl border border-dashed border-slate-200 p-4">
                <div className="flex items-center gap-2 text-sm font-semibold text-slate-800">
                  <Workflow size={16} className="text-blue-600" /> Itens incluídos na publicação
                </div>
                <div className="mt-3 grid gap-2 md:grid-cols-2">
                  {selectedItems.map((item) => (
                    <div key={item.id} className="rounded-lg border border-slate-200 bg-white p-3">
                      <p className="text-sm font-medium text-slate-800">{item.id} • {item.productName}</p>
                      <p className="mt-1 text-xs text-slate-500">{item.quantityLabel} • {item.routeLabel}</p>
                    </div>
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>

          <div className="space-y-6">
            <Card className="gap-0 border-slate-200 py-0 shadow-sm">
              <CardHeader className="border-b border-slate-100 bg-slate-50/70 py-3">
                <CardTitle className="text-base font-bold text-slate-800">Resumo</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4 p-6">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Trecho selecionado</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{selectedSegment?.name || "Nenhum selecionado"}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Duração</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{auctionDuration}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Lance mínimo</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{minimumBid ? `R$ ${minimumBid}` : "Não definido"}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Encerramento</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{auctionDeadline || "Não definido"}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Cobertura</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{selectedSegment?.coverage || "Não definido"}</p>
                </div>
              </CardContent>
            </Card>

            <Card className="gap-0 border-slate-200 py-0 shadow-sm">
              <CardHeader className="border-b border-slate-100 bg-slate-50/70 py-3">
                <CardTitle className="text-base font-bold text-slate-800">Publicação</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4 p-6">
                <p className="text-sm text-slate-600">
                  Ao publicar, o trecho completo ficará visível para as transportadoras com sua composição, paradas e itens envolvidos.
                </p>
                <Button className="w-full bg-blue-600 text-white hover:bg-blue-700" onClick={handlePublish}>
                  <Plus size={16} className="mr-2" /> Publicar agora
                </Button>
                {isPublished && (
                  <p className="text-xs font-medium text-emerald-600">Leilão publicado com sucesso.</p>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </AppShell>
  )
}
