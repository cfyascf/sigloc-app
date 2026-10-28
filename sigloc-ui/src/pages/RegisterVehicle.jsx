import { useMemo, useState } from "react"
import { ArrowLeft, Truck, User, Scale, Box, Save, X, Info } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import VehicleForm from "@/pages/VehicleForm"
import { useAsyncAction } from "@/hooks/use-async-action"
import { vehicleService } from "@/services/vehicle-service"
import { BODY_TYPE_OPTIONS, REFRIGERATION_BY_VALUE } from "@/constants/vehicles"
import { VEHICLE_LIMITS, mapVehicleFieldErrors, validateVehicle } from "@/lib/vehicle-validation"

const FieldError = ({ message }) =>
  message ? <p className="text-[11px] font-semibold text-rose-600">{message}</p> : null

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
  const [clientErrors, setClientErrors] = useState({})
  const serverErrors = useMemo(() => mapVehicleFieldErrors(saveAction.fieldErrors), [saveAction.fieldErrors])
  const errors = { ...serverErrors, ...clientErrors }
  const handleSubmit = async (event) => {
    event.preventDefault()
    if (saveAction.pending) return
    const problems = validateVehicle(formData, { creating: true })
    setClientErrors(problems)
    if (Object.keys(problems).length) return
    const result = await saveAction.run(formData)
    if (result.ok) navigate("/fleet-management")
  }

  return (
    <AppShell title="Cadastro de Veículo">
      {/* Container com scroll blindado e altura calculada em 8.5rem garantindo margem de segurança */}
      <div className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden">
        
        {/* HEADER LIMPO E TEXTUAL */}
        <div className="flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1 mb-5">
          <Link to="/fleet-management">
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de Frota
            </Button>
          </Link>
        </div>

        {saveAction.error && <p role="alert" className="mb-4 rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{saveAction.error.message}</p>}
        <form id="register-vehicle" noValidate onSubmit={handleSubmit} className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pr-2 pb-6">
            <div className="grid items-start gap-6 md:grid-cols-2">

              {/* Coluna esquerda: Identificação + Capacidade */}
              <div className="flex flex-col gap-6">
                <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                  <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5 rounded-t-xl">
                    <Truck size={16} className="text-blue-600" />
                    <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Identificação do Veículo</h2>
                  </div>

                  <div className="grid gap-5 p-5 sm:grid-cols-2">
                    <div className="space-y-2">
                      <label className="text-xs font-bold text-slate-600">Placa do Veículo</label>
                      <Input
                        placeholder="Ex: ABC-1234"
                        value={formData.plate}
                        aria-invalid={Boolean(errors.plate)}
                        onChange={(e) => setFormData({...formData, plate: e.target.value})}
                        className="h-10 border-slate-200 text-sm font-mono font-bold uppercase placeholder:font-sans placeholder:font-normal focus:border-blue-500 focus:ring-blue-500"
                      />
                      <p className="flex items-center gap-1.5 text-[11px] font-medium text-slate-500">
                        <Info size={12} className="text-blue-500" /> Identificador único na frota.
                      </p>
                      <FieldError message={errors.plate} />
                    </div>

                    <div className="space-y-2">
                      <label className="text-xs font-bold text-slate-600">Marca / Modelo</label>
                      <Input
                        placeholder="Ex: Volvo FH 540"
                        maxLength={VEHICLE_LIMITS.model}
                        value={formData.model}
                        aria-invalid={Boolean(errors.model)}
                        onChange={(e) => setFormData({...formData, model: e.target.value})}
                        className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                      <FieldError message={errors.model} />
                    </div>

                    <div className="space-y-2 sm:col-span-2">
                      <label className="text-xs font-bold text-slate-600">Tipo de Carroceria</label>
                      <Select value={formData.bodyType} onValueChange={(v) => setFormData({...formData, bodyType: v})}>
                        <SelectTrigger aria-invalid={Boolean(errors.bodyType)} className="h-10 w-full border-slate-200 text-sm focus:ring-blue-500 data-[size=default]:h-10">
                          <SelectValue placeholder="Selecione o implemento..." />
                        </SelectTrigger>
                        <SelectContent>
                          {BODY_TYPE_OPTIONS.map((option) => <SelectItem key={option} value={option}>{option}</SelectItem>)}
                        </SelectContent>
                      </Select>
                      <FieldError message={errors.bodyType} />
                    </div>
                  </div>
                </div>

                <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                  <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5 rounded-t-xl">
                    <Scale size={16} className="text-blue-600" />
                    <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Capacidade da Carroceria</h2>
                  </div>

                  <div className="grid gap-5 p-5 sm:grid-cols-2">
                    <div className="space-y-2">
                      <label htmlFor="axle-count" className="block text-xs font-bold text-slate-600">Quantidade de eixos</label>
                      <Input id="axle-count" type="number" step="1" placeholder="0" aria-invalid={Boolean(errors.axleCount)} value={formData.axleCount} onChange={(event) => setFormData({ ...formData, axleCount: event.target.value })} className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500" />
                      <FieldError message={errors.axleCount} />
                    </div>

                    <div className="space-y-2">
                      <label htmlFor="refrigeration" className="block text-xs font-bold text-slate-600">Refrigeração</label>
                      <select id="refrigeration" value={formData.refrigerationLevel} onChange={(event) => setFormData({ ...formData, refrigerationLevel: Number(event.target.value) })} className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm">
                        {Object.entries(REFRIGERATION_BY_VALUE).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                      </select>
                    </div>

                    <div className="space-y-2">
                      <label className="flex items-center gap-1.5 text-xs font-bold text-slate-600">
                        <Scale size={13} className="text-slate-400" /> Peso Suportado
                      </label>
                      <div className="relative">
                        <Input
                            type="number"
                            placeholder="0"
                            step="any" value={formData.weightKg}
                            aria-invalid={Boolean(errors.weightKg)}
                            onChange={(e) => setFormData({...formData, weightKg: e.target.value})}
                            className="h-10 border-slate-200 pr-10 text-sm focus:border-blue-500 focus:ring-blue-500"
                        />
                        <span className="absolute right-3 top-2.5 text-xs font-semibold text-slate-400">kg</span>
                      </div>
                      <FieldError message={errors.weightKg} />
                    </div>

                    <div className="space-y-2">
                      <label className="flex items-center gap-1.5 text-xs font-bold text-slate-600">
                        <Box size={13} className="text-slate-400" /> Volume Físico
                      </label>
                      <div className="relative">
                        <Input
                            type="number"
                            placeholder="0"
                            step="any" value={formData.volumeM3}
                            aria-invalid={Boolean(errors.volumeM3)}
                            onChange={(e) => setFormData({...formData, volumeM3: e.target.value})}
                            className="h-10 border-slate-200 pr-10 text-sm focus:border-blue-500 focus:ring-blue-500"
                        />
                        <span className="absolute right-3 top-2.5 text-xs font-semibold text-slate-400">m³</span>
                      </div>
                      <FieldError message={errors.volumeM3} />
                    </div>

                    <div className="grid gap-4 border-t border-slate-100 pt-4 sm:col-span-2 sm:grid-cols-2">
                      <label className="flex items-start gap-2 text-xs text-slate-600">
                        <input type="checkbox" className="mt-0.5" aria-describedby="mopp-help" checked={formData.hasMopp} onChange={(event) => setFormData({ ...formData, hasMopp: event.target.checked })} />
                        <span>
                          <span className="block font-medium">Possui MOPP</span>
                          <span id="mopp-help" className="block text-[11px] text-slate-500">Habilita o transporte de produtos perigosos.</span>
                        </span>
                      </label>
                      <label className="flex items-start gap-2 text-xs text-slate-600">
                        <input type="checkbox" className="mt-0.5" aria-describedby="cargo-securing-help" checked={formData.hasCargoSecuring} onChange={(event) => setFormData({ ...formData, hasCargoSecuring: event.target.checked })} />
                        <span>
                          <span className="block font-medium">Possui fixação de carga</span>
                          <span id="cargo-securing-help" className="block text-[11px] text-slate-500">Habilita o transporte de produtos frágeis.</span>
                        </span>
                      </label>
                    </div>
                  </div>
                </div>
              </div>

              {/* Coluna direita: Atribuição Física + aviso da Mesa de Operações */}
              <div className="flex flex-col gap-6">
                <div className="flex flex-col rounded-xl border border-slate-200 bg-white">
                  <div className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-5 rounded-t-xl">
                    <User size={16} className="text-emerald-600" />
                    <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Atribuição Física</h2>
                  </div>

                  <div className="flex flex-col space-y-5 p-5">
                    <div className="space-y-2">
                      <label className="text-xs font-bold text-slate-600">Motorista Responsável (opcional)</label>
                      <Input
                        placeholder="Nome do motorista"
                        value={formData.driver}
                        onChange={(e) => setFormData({...formData, driver: e.target.value})}
                        className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>

                    <div className="space-y-2">
                      <label className="text-xs font-bold text-slate-600">Pátio / Localização Atual</label>
                      <Input
                        placeholder="Ex: Curitiba, PR"
                        value={formData.location}
                        onChange={(e) => setFormData({...formData, location: e.target.value})}
                        className="h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>

                    <VehicleForm value={formData} onChange={setFormData} disabled={saveAction.pending} idPrefix="register" errors={errors} inputClassName="h-10" />
                  </div>
                </div>

                <div className="rounded-xl border border-emerald-100 bg-emerald-50 p-4">
                  <h4 className="mb-1 text-xs font-bold text-emerald-800">Mesa de Operações</h4>
                  <p className="text-[11px] font-medium leading-relaxed text-emerald-700">
                    Novos veículos são cadastrados com o status <strong className="font-bold">Livre</strong> e serão automaticamente listados na mesa de operação, podendo receber atribuição de leilões e lances ativos.
                  </p>
                </div>

                <div className="flex items-center justify-end gap-3">
                  <Button asChild variant="outline" className="h-10 border-slate-200 bg-white px-5 text-xs font-semibold text-slate-700">
                    <Link to="/fleet-management"><X size={14} className="mr-1.5" /> Cancelar</Link>
                  </Button>
                  <Button type="submit" disabled={saveAction.pending} className="h-10 bg-blue-600 px-5 text-xs font-bold tracking-wide text-white hover:bg-blue-700">
                    <Save size={14} className="mr-1.5" /> {saveAction.pending ? "Salvando..." : "Salvar Veículo"}
                  </Button>
                </div>
              </div>

            </div>
          </div>
        </form>
      </div>
    </AppShell>
  )
}