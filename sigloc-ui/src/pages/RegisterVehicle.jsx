import { useState } from "react"
import { ArrowLeft, Truck, User, Activity, Scale, Box, Save, X, Info } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import VehicleForm from "@/pages/VehicleForm"
import { useAsyncAction } from "@/hooks/use-async-action"
import { vehicleService } from "@/services/vehicle-service"
import { BODY_TYPE_OPTIONS, REFRIGERATION_BY_VALUE } from "@/constants/vehicles"

export default function RegisterVehicle() {
  const navigate = useNavigate()
  
  // Estado espelhado aos campos do modo "update" da tela anterior
  const [formData, setFormData] = useState({
    plate: "",
    model: "",
    driver: "",
    driverPhone: "",
    traccarDeviceId: "",
    axleCount: "",
    refrigerationLevel: 0,
    hasMopp: false,
    hasCargoSecuring: false,
    location: "",
    weightKg: "",
    volumeM3: "",
    bodyType: "",
    status: "Livre"
  })
  const saveAction = useAsyncAction(vehicleService.createVehicle)
  const handleSubmit = async (event) => {
    event.preventDefault()
    if (saveAction.pending) return
    const result = await saveAction.run(formData)
    if (result.ok) navigate("/fleet-management")
  }

  return (
    <AppShell title="Cadastro de Veículo">
      {/* Container com scroll blindado e altura calculada em 8.5rem garantindo margem de segurança */}
      <div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-5xl flex-col overflow-hidden">
        
        {/* HEADER LIMPO E TEXTUAL */}
        <div className="flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1 mb-5">
          <Link to="/fleet-management">
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de Frota
            </Button>
          </Link>

          <div className="flex items-center gap-3">
            <Button asChild variant="outline" className="h-9 border-slate-200 text-xs font-semibold text-slate-700 bg-white">
              <Link to="/fleet-management"><X size={14} className="mr-1.5" /> Cancelar</Link>
            </Button>
            <Button type="submit" form="register-vehicle" disabled={saveAction.pending} className="h-9 bg-blue-600 text-xs font-bold tracking-wide text-white hover:bg-blue-700">
              <Save size={14} className="mr-1.5" /> {saveAction.pending ? "Salvando..." : "Salvar Veículo"}
            </Button>
          </div>
        </div>

        {saveAction.error && <p role="alert" className="mb-4 rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{saveAction.error.message}</p>}
        <form id="register-vehicle" onSubmit={handleSubmit} className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">
          
            {/* O segredo do alinhamento: grid-rows-[auto_1fr] obriga as caixas a andarem em pares */}
            <div className="grid min-h-full gap-6 md:grid-cols-2 md:grid-rows-[auto_1fr]">
              
              {/* ========================================================
                  LINHA 1: IDENTIFICAÇÃO (Esq) + ALOCAÇÃO (Dir)
                  ======================================================== */}
              
              {/* Bloco 1: Identificação Básica (Esquerda, Topo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5 rounded-t-xl">
                  <Truck size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Identificação do Veículo</h2>
                </div>
                
                <div className="flex flex-col p-5 space-y-5">
                  <div className="space-y-2">
                    <label className="text-xs font-bold text-slate-600">Placa do Veículo</label>
                    <Input 
                      required placeholder="Ex: ABC-1234"
                      value={formData.plate}
                      onChange={(e) => setFormData({...formData, plate: e.target.value})}
                      className="h-10 border-slate-200 text-sm font-mono font-bold uppercase placeholder:font-sans placeholder:font-normal focus:border-blue-500 focus:ring-blue-500" 
                    />
                  </div>

                  <div className="space-y-2">
                    <label className="text-xs font-bold text-slate-600">Marca / Modelo</label>
                    <Input 
                      required placeholder="Ex: Volvo FH 540"
                      value={formData.model}
                      onChange={(e) => setFormData({...formData, model: e.target.value})}
                      className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500" 
                    />
                  </div>

                  <div className="flex items-center justify-between rounded-lg border border-dashed border-slate-200 bg-slate-50/70 px-3 py-3 mt-2">
                    <p className="text-[11px] font-medium text-slate-500 flex items-center gap-1.5">
                        <Info size={14} className="text-blue-500"/>
                        A placa será usada como identificador único na frota.
                    </p>
                  </div>
                </div>
              </div>

              {/* Bloco 2: Alocação e Localização (Direita, Topo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5 rounded-t-xl">
                  <User size={16} className="text-emerald-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Atribuição Física</h2>
                </div>
                
                <div className="flex flex-col p-5">
                  {/* Reaproveitamento visual da linha conectora do Itinerário original */}
                  <div className="relative space-y-6">
                    <VehicleForm value={formData} onChange={setFormData} disabled={saveAction.pending} idPrefix="register" />
                    <div className="absolute bottom-5 left-3.5 top-8 w-px border-l-2 border-dashed border-slate-200" />
                    
                    <div className="relative space-y-2 pl-8">
                      <span className="absolute left-1.5 top-2.5 h-4 w-4 rounded-full border-[3px] border-white bg-slate-800" />
                      <label className="text-xs font-bold text-slate-600">Motorista Responsável</label>
                      <Input 
                        placeholder="Nome do motorista (Opcional)" 
                        value={formData.driver}
                        onChange={(e) => setFormData({...formData, driver: e.target.value})}
                        className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500" 
                      />
                    </div>

                    <div className="relative space-y-2 pl-8">
                      <span className="absolute left-1.5 top-2.5 h-4 w-4 rounded-full border-[3px] border-white bg-emerald-500" />
                      <label className="text-xs font-bold text-slate-600">Pátio / Localização Atual</label>
                      <Input 
                        placeholder="Ex: Curitiba, PR" 
                        value={formData.location}
                        onChange={(e) => setFormData({...formData, location: e.target.value})}
                        className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500" 
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* ========================================================
                  LINHA 2: CUBAGEM E TIPO (Esq) + STATUS (Dir)
                  ======================================================== */}

              {/* Bloco 3: Dimensionamento e Equipamento (Esquerda, Baixo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5 rounded-t-xl">
                  <Scale size={16} className="text-blue-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Capacidade da Carroceria</h2>
                </div>
                
                <div className="flex flex-1 flex-col p-5">
                  <div className="mb-5 space-y-2">
                    <label htmlFor="axle-count" className="text-xs font-bold text-slate-600">Quantidade de eixos</label>
                    <Input id="axle-count" type="number" min="1" step="1" required value={formData.axleCount} onChange={(event) => setFormData({ ...formData, axleCount: event.target.value })} />
                    <label htmlFor="refrigeration" className="block text-xs font-bold text-slate-600">Refrigeração</label>
                    <select id="refrigeration" value={formData.refrigerationLevel} onChange={(event) => setFormData({ ...formData, refrigerationLevel: Number(event.target.value) })} className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm">
                      {Object.entries(REFRIGERATION_BY_VALUE).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                    </select>
                    <label className="flex items-center gap-2 text-xs text-slate-600"><input type="checkbox" checked={formData.hasMopp} onChange={(event) => setFormData({ ...formData, hasMopp: event.target.checked })} /> Possui MOPP</label>
                    <label className="flex items-center gap-2 text-xs text-slate-600"><input type="checkbox" checked={formData.hasCargoSecuring} onChange={(event) => setFormData({ ...formData, hasCargoSecuring: event.target.checked })} /> Possui fixação de carga</label>
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <label className="flex items-center gap-1.5 text-xs font-bold text-slate-600">
                        <Scale size={13} className="text-slate-400" /> Peso Suportado
                      </label>
                      <div className="relative">
                        <Input 
                            type="number" 
                            placeholder="0" 
                            required min="0.001" step="any" value={formData.weightKg}
                            onChange={(e) => setFormData({...formData, weightKg: e.target.value})}
                            className="h-10 border-slate-200 pr-10 text-sm focus:border-blue-500 focus:ring-blue-500" 
                        />
                        <span className="absolute right-3 top-2.5 text-xs font-semibold text-slate-400">kg</span>
                      </div>
                    </div>
                    
                    <div className="space-y-2">
                      <label className="flex items-center gap-1.5 text-xs font-bold text-slate-600">
                        <Box size={13} className="text-slate-400" /> Volume Físico
                      </label>
                      <div className="relative">
                        <Input 
                            type="number" 
                            placeholder="0" 
                            required min="0.001" step="any" value={formData.volumeM3}
                            onChange={(e) => setFormData({...formData, volumeM3: e.target.value})}
                            className="h-10 border-slate-200 pr-10 text-sm focus:border-blue-500 focus:ring-blue-500" 
                        />
                        <span className="absolute right-3 top-2.5 text-xs font-semibold text-slate-400">m³</span>
                      </div>
                    </div>
                  </div>

                  {/* MT-AUTO espelhado: Força a Restrição pro exato fundo da caixa */}
                  <div className="mt-auto border-t border-slate-100 pt-5">
                    <div className="space-y-2">
                      <label className="text-xs font-bold text-slate-600">Tipo de Carroceria</label>
                      <Select required value={formData.bodyType} onValueChange={(v) => setFormData({...formData, bodyType: v})}>
                        <SelectTrigger className="h-10 border-slate-200 text-sm focus:ring-blue-500">
                          <SelectValue placeholder="Selecione o implemento..." />
                        </SelectTrigger>
                        <SelectContent>
                          {BODY_TYPE_OPTIONS.map((option) => <SelectItem key={option} value={option}>{option}</SelectItem>)}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </div>
              </div>

              {/* Bloco 4: Status Operacional (Direita, Baixo) */}
              <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5 rounded-t-xl">
                  <Activity size={16} className="text-amber-600" />
                  <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Disponibilidade e Status</h2>
                </div>
                
                <div className="flex flex-1 flex-col p-5">
                  <div className="space-y-2">
                    <label className="text-xs font-bold text-slate-600">Status Operacional Inicial</label>
                    <Select value="Livre" disabled>
                        <SelectTrigger className="h-10 border-slate-200 text-sm focus:ring-blue-500">
                          <SelectValue placeholder="Defina o status atual do veículo..." />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Livre" className="font-semibold text-emerald-700">Livre (Disponível para carga)</SelectItem>
                          <SelectItem value="Em Trânsito" className="font-semibold text-blue-700">Em Trânsito</SelectItem>
                          <SelectItem value="Manutenção" className="font-semibold text-amber-700">Em Manutenção</SelectItem>
                        </SelectContent>
                    </Select>
                  </div>

                  {/* MT-AUTO espelhado: Força a info box pro exato fundo da caixa, igual ao "Orçamento Teto" anterior */}
                  <div className="mt-auto border-t border-slate-100 pt-5">
                    <div className="rounded-lg bg-emerald-50 border border-emerald-100 p-4">
                        <h4 className="text-xs font-bold text-emerald-800 mb-1">Mesa de Operações</h4>
                        <p className="text-[11px] font-medium text-emerald-700 leading-relaxed">
                          Veículos criados com o status <strong className="font-bold">Livre</strong> serão automaticamente listados na mesa de operação e poderão receber atribuição de leilões e lances ativos.
                        </p>
                    </div>
                  </div>
                </div>
              </div>

            </div>
          </div>
        </form>
      </div>
    </AppShell>
  )
}