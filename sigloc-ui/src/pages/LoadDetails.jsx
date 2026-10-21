import { useState } from "react"
import { ArrowLeft, CalendarRange, Clock3, MapPin, Package, Route, ShieldAlert, Truck, Wallet, Workflow } from "lucide-react"
import { Link, useNavigate, useParams } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { cargoItemsMock, getCargoItemById } from "@/constants/logistics-mock"

// eslint-disable-next-line react/prop-types
function DetailItem({ icon: Icon, label, value }) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-slate-200 bg-white p-4">
      <div className="mt-0.5 rounded-md bg-blue-50 p-2 text-blue-600">
        <Icon size={16} />
      </div>
      <div>
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">{label}</p>
        <p className="text-sm font-medium text-slate-800">{value}</p>
      </div>
    </div>
  )
}

export default function LoadDetails() {
  const navigate = useNavigate()
  const { loadId } = useParams()
  const fallbackLoad = cargoItemsMock[0]
  const resolvedLoad = getCargoItemById(loadId) || fallbackLoad
  const initialLoad = { ...resolvedLoad, id: loadId || resolvedLoad.id }
  const [load, setLoad] = useState(initialLoad)
  const [isUpdateOpen, setIsUpdateOpen] = useState(false)
  const [draftLoad, setDraftLoad] = useState(initialLoad)

  const formatCurrency = (value) => {
    return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value)
  }

  const openUpdateDialog = () => {
    setDraftLoad(load)
    setIsUpdateOpen(true)
  }

  const handleDraftChange = (field, value) => {
    setDraftLoad((current) => ({
      ...current,
      [field]: field === "freightValue" ? Number(value) || 0 : value,
    }))
  }

  const handleSaveUpdate = () => {
    setLoad(draftLoad)
    setIsUpdateOpen(false)
  }

  const handleDeleteLoad = () => {
    const shouldDelete = globalThis.confirm(`Tem certeza que deseja excluir o item ${load.id}?`)

    if (!shouldDelete) {
      return
    }

    setIsUpdateOpen(false)
    navigate("/load-management")
  }

  return (
    <AppShell title={`Detalhes do Item ${load.id}`}>
      <div className="mx-auto max-w-7xl space-y-6">
        <div className="flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
          <Link to="/load-management">
            <Button variant="ghost" className="text-slate-500 hover:text-slate-900">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Itens de Carga
            </Button>
          </Link>

          <div className="flex flex-wrap gap-3">
            <Button variant="outline" className="border-slate-200 bg-white text-slate-700" onClick={openUpdateDialog}>
              Atualizar Item
            </Button>
            {load.linkedSegmentId ? (
              <div className="flex flex-wrap gap-3">
                <Button asChild variant="outline" className="border-slate-200 bg-white text-slate-700 hover:bg-slate-50">
                  <Link to={`/segment-details/${load.linkedSegmentId}`}>Abrir trecho</Link>
                </Button>
                <Button asChild className="bg-blue-600 text-white hover:bg-blue-700">
                  <Link to={`/auction-bids/${load.linkedSegmentId}`}>Ver lances do trecho</Link>
                </Button>
              </div>
            ) : (
              <Button disabled className="bg-slate-300 text-slate-600">
                Sem trecho vinculado
              </Button>
            )}
          </div>
        </div>

        <div className="grid gap-6 lg:grid-cols-3">
          <Card className="gap-0 border-slate-200 py-0 shadow-sm lg:col-span-2">
            <CardHeader className="border-b border-slate-100 bg-slate-50/70 py-3">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <CardTitle className="flex items-center gap-2 text-lg font-bold text-slate-800">
                    <Route size={18} className="text-blue-600" /> {load.routeLabel}
                  </CardTitle>
                  <CardDescription>
                    Visão operacional do item que alimenta o planejamento do trecho.
                  </CardDescription>
                </div>
                <Badge className="border-none bg-blue-100 text-blue-800">{load.status}</Badge>
              </div>
            </CardHeader>

            <CardContent className="space-y-6 p-6">
              <div className="grid gap-4 md:grid-cols-2">
                <DetailItem icon={CalendarRange} label="Janela de Coleta" value={load.pickupWindow} />
                <DetailItem icon={Truck} label="Janela de Entrega" value={load.deliveryWindow} />
                <DetailItem icon={Wallet} label="Valor do Frete" value={formatCurrency(load.freightValue)} />
                <DetailItem icon={Clock3} label="ETA estimado" value={load.eta} />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <DetailItem icon={MapPin} label="Origem" value={load.origin} />
                <DetailItem icon={MapPin} label="Destino" value={load.destination} />
              </div>

              <div className="grid gap-4 md:grid-cols-3">
                <DetailItem icon={Package} label="Produto" value={load.productName} />
                <DetailItem icon={ShieldAlert} label="Peso / Volume" value={`${load.weight} • ${load.volume}`} />
                <DetailItem icon={Route} label="Quantidade" value={load.quantityLabel} />
              </div>
            </CardContent>
          </Card>

          <div className="space-y-6">
            <Card className="gap-0 border-slate-200 py-0 shadow-sm">
              <CardHeader className="border-b border-slate-100 bg-slate-50/70 py-3">
                <CardTitle className="text-base font-bold text-slate-800">Planejamento e Leilão</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4 p-6">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Trecho vinculado</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{load.linkedSegmentId || "Ainda não vinculado"}</p>
                  <p className="text-sm text-slate-500">{load.linkedSegmentName}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Contato da Operação</p>
                  <p className="mt-1 text-sm font-medium text-slate-800">{load.contactName}</p>
                  <p className="text-sm text-slate-500">{load.contactPhone}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Requisitos do item</p>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {load.requirements.map((requirement) => (
                      <Badge key={requirement} variant="outline" className="border-slate-200 bg-slate-50 text-slate-700">
                        {requirement}
                      </Badge>
                    ))}
                  </div>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Risco Operacional</p>
                  <p className="mt-1 text-sm text-slate-700">{load.operationalRisk}</p>
                </div>
                <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
                  <div className="flex items-center gap-2 text-sm font-semibold text-slate-800">
                    <Workflow size={16} className="text-blue-600" /> Papel no trecho
                  </div>
                  <p className="mt-2 text-sm text-slate-600">
                    Este item pode compor uma perna simples ou fazer parte de uma consolidação com outras cargas e múltiplas paradas.
                  </p>
                </div>
              </CardContent>
            </Card>

          </div>
        </div>

        <Dialog open={isUpdateOpen} onOpenChange={setIsUpdateOpen}>
          <DialogContent className="sm:max-w-[680px]">
            <DialogHeader>
              <DialogTitle>Atualizar Item de Carga</DialogTitle>
              <DialogDescription>
                Ajuste os principais dados operacionais do item. As alterações aparecerão na tela após salvar.
              </DialogDescription>
            </DialogHeader>

            <div className="grid gap-4 py-2 md:grid-cols-2">
              <div className="space-y-2 md:col-span-2">
                <p className="text-sm font-medium text-slate-700">Janela de Coleta</p>
                <Input value={draftLoad.pickupWindow} onChange={(e) => handleDraftChange("pickupWindow", e.target.value)} />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-slate-700">Origem</p>
                <Input value={draftLoad.origin} onChange={(e) => handleDraftChange("origin", e.target.value)} />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-slate-700">Destino</p>
                <Input value={draftLoad.destination} onChange={(e) => handleDraftChange("destination", e.target.value)} />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-slate-700">Valor do Frete (R$)</p>
                <Input type="number" value={draftLoad.freightValue} onChange={(e) => handleDraftChange("freightValue", e.target.value)} />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-slate-700">Quantidade</p>
                <Input value={draftLoad.quantityLabel} onChange={(e) => handleDraftChange("quantityLabel", e.target.value)} />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-slate-700">ETA</p>
                <Input value={draftLoad.eta} onChange={(e) => handleDraftChange("eta", e.target.value)} />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-slate-700">Trecho vinculado</p>
                <Input value={draftLoad.linkedSegmentName} onChange={(e) => handleDraftChange("linkedSegmentName", e.target.value)} />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-slate-700">Risco Operacional</p>
                <Input value={draftLoad.operationalRisk} onChange={(e) => handleDraftChange("operationalRisk", e.target.value)} />
              </div>
            </div>

            <DialogFooter>
              <Button variant="destructive" onClick={handleDeleteLoad}>
                Excluir item
              </Button>
              <Button variant="outline" onClick={() => setIsUpdateOpen(false)}>
                Cancelar
              </Button>
              <Button className="bg-blue-600 text-white hover:bg-blue-700" onClick={handleSaveUpdate}>
                Salvar alterações
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    </AppShell>
  )
}
