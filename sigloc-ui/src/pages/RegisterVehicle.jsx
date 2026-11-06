import { useMemo, useState } from "react"
import { ArrowLeft, Truck, User, Scale, Box, Save, X, ShieldCheck, Settings2, Info } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { BodyTypeField, EquipmentChecks, RefrigerationField } from "@/components/vehicles/VehicleEquipmentFields"
import { CivCippField, CompositionField, DriverDocumentFields, RntrcFields } from "@/components/vehicles/VehicleDocumentFields"
import { documentsToForm } from "@/services/vehicle-service"
import { RNTRC_SOURCE } from "@/constants/vehicles"
import { useAsyncAction } from "@/hooks/use-async-action"
import { vehicleService } from "@/services/vehicle-service"
import { VEHICLE_LIMITS, mapVehicleFieldErrors, validateVehicle } from "@/lib/vehicle-validation"

const FieldError = ({ message }) =>
  message ? <p className="text-[11px] font-semibold text-rose-600">{message}</p> : null

const LABEL = "block text-xs font-bold text-slate-600"
const INPUT = "h-10 border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500"

// eslint-disable-next-line react/prop-types
function Card({ icon: Icon, iconClassName = "text-blue-600", title, className = "", children }) {
  return (
    <div className={`flex flex-col rounded-xl border border-slate-200 bg-white ${className}`}>
      <div className="flex h-[52px] items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
        <Icon size={16} className={iconClassName} />
        <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">{title}</h2>
      </div>
      <div className="flex-1 p-5">{children}</div>
    </div>
  )
}

export default function RegisterVehicle() {
  const navigate = useNavigate()

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
    status: "Livre",
    // Documents; a new vehicle runs under the carrier's own RNTRC unless told otherwise.
    ...documentsToForm(null),
    rntrcSource: RNTRC_SOURCE.CARRIER,
  })
  const saveAction = useAsyncAction(vehicleService.createVehicle)
  const [clientErrors, setClientErrors] = useState({})
  const serverErrors = useMemo(() => mapVehicleFieldErrors(saveAction.fieldErrors), [saveAction.fieldErrors])
  const errors = { ...serverErrors, ...clientErrors }
  const set = (field) => (event) => setFormData({ ...formData, [field]: event.target.value })
  const fieldProps = { value: formData, onChange: setFormData, errors, disabled: saveAction.pending, idPrefix: "register" }

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
      <div className="flex h-[calc(100vh-8.5rem)] flex-col overflow-hidden">
        <div className="mb-5 flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1">
          <Link to="/fleet-management">
            <Button variant="ghost" className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900">
              <ArrowLeft size={16} className="mr-2" /> Voltar para Gestão de Frota
            </Button>
          </Link>
          <div className="flex items-center gap-3">
            <Button asChild variant="outline" className="h-9 border-slate-200 bg-white px-4 text-xs font-semibold text-slate-700">
              <Link to="/fleet-management"><X size={14} className="mr-1.5" /> Cancelar</Link>
            </Button>
            <Button type="submit" form="register-vehicle" disabled={saveAction.pending} className="h-9 bg-blue-600 px-4 text-xs font-bold tracking-wide text-white hover:bg-blue-700">
              <Save size={14} className="mr-1.5" /> {saveAction.pending ? "Salvando..." : "Salvar Veículo"}
            </Button>
          </div>
        </div>

        {saveAction.error && <p role="alert" className="mb-4 rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{saveAction.error.message}</p>}
        <form id="register-vehicle" noValidate onSubmit={handleSubmit} className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pb-6 pr-2">
            {/* Vehicle on the left, who runs it on the right; the last card of each column stretches so both end level. */}
            <div className="grid gap-6 md:grid-cols-2">
              <div className="flex flex-col gap-6">
                <Card icon={Truck} title="Veículo">
                  <div className="grid gap-x-5 gap-y-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <label htmlFor="register-plate" className={LABEL}>Placa</label>
                      <Input
                        id="register-plate"
                        placeholder="Ex: ABC-1234"
                        value={formData.plate}
                        aria-invalid={Boolean(errors.plate)}
                        onChange={set("plate")}
                        className={`${INPUT} font-mono font-bold uppercase placeholder:font-sans placeholder:font-normal`}
                      />
                      <FieldError message={errors.plate} />
                    </div>
                    <div className="space-y-2">
                      <label htmlFor="register-model" className={LABEL}>Marca / Modelo</label>
                      <Input id="register-model" placeholder="Ex: Volvo FH 540" maxLength={VEHICLE_LIMITS.model} value={formData.model}
                        aria-invalid={Boolean(errors.model)} onChange={set("model")} className={INPUT} />
                      <FieldError message={errors.model} />
                    </div>
                    <BodyTypeField {...fieldProps} labelClassName={LABEL}
                      triggerClassName="h-10 w-full border-slate-200 text-sm focus:ring-blue-500 data-[size=default]:h-10" />
                    <CompositionField {...fieldProps} className="space-y-2" labelClassName={LABEL} selectClassName="h-10" />
                    <div className="space-y-2">
                      <label htmlFor="register-location" className={LABEL}>Pátio / Localização atual</label>
                      <Input id="register-location" placeholder="Ex: Curitiba, PR" value={formData.location} onChange={set("location")} className={INPUT} />
                    </div>
                    <div className="space-y-2">
                      <label htmlFor="register-tracker" className={LABEL}>ID do rastreador Traccar (opcional)</label>
                      <Input id="register-tracker" type="number" placeholder="Ex: 1024" value={formData.traccarDeviceId}
                        aria-invalid={Boolean(errors.traccarDeviceId)} onChange={set("traccarDeviceId")} className={`${INPUT} font-mono placeholder:font-sans`} />
                      <FieldError message={errors.traccarDeviceId} />
                    </div>
                  </div>
                </Card>

                <Card icon={Settings2} title="Capacidade e Equipamento" className="flex-1">
                  <div className="grid gap-x-5 gap-y-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <label htmlFor="axle-count" className={LABEL}>Eixos</label>
                      <Input id="axle-count" type="number" step="1" placeholder="0" aria-invalid={Boolean(errors.axleCount)}
                        value={formData.axleCount} onChange={set("axleCount")} className={INPUT} />
                      <FieldError message={errors.axleCount} />
                    </div>
                    <RefrigerationField {...fieldProps} labelClassName={LABEL} selectClassName="h-10" />
                    <div className="space-y-2">
                      <label className={`flex items-center gap-1.5 ${LABEL}`}><Scale size={13} className="text-slate-400" /> Peso suportado</label>
                      <div className="relative">
                        <Input type="number" placeholder="0" step="any" value={formData.weightKg} aria-invalid={Boolean(errors.weightKg)}
                          onChange={set("weightKg")} className={`${INPUT} pr-10`} />
                        <span className="absolute right-3 top-2.5 text-xs font-semibold text-slate-400">kg</span>
                      </div>
                      <FieldError message={errors.weightKg} />
                    </div>
                    <div className="space-y-2">
                      <label className={`flex items-center gap-1.5 ${LABEL}`}><Box size={13} className="text-slate-400" /> Volume físico</label>
                      <div className="relative">
                        <Input type="number" placeholder="0" step="any" value={formData.volumeM3} aria-invalid={Boolean(errors.volumeM3)}
                          onChange={set("volumeM3")} className={`${INPUT} pr-10`} />
                        <span className="absolute right-3 top-2.5 text-xs font-semibold text-slate-400">m³</span>
                      </div>
                      <FieldError message={errors.volumeM3} />
                    </div>
                    <EquipmentChecks {...fieldProps} className="border-t border-slate-100 pt-4 sm:col-span-2" />
                    <CivCippField {...fieldProps} labelClassName={LABEL} inputClassName={INPUT} />
                  </div>
                </Card>
              </div>

              <div className="flex flex-col gap-6">
                <Card icon={User} iconClassName="text-emerald-600" title="Motorista">
                  <div className="space-y-4">
                    <div className="grid gap-x-5 gap-y-4 sm:grid-cols-2">
                      <div className="space-y-2">
                        <label htmlFor="register-driver" className={LABEL}>Nome (opcional)</label>
                        <Input id="register-driver" placeholder="Nome do motorista" value={formData.driver} onChange={set("driver")} className={INPUT} />
                      </div>
                      <div className="space-y-2">
                        <label htmlFor="register-phone" className={LABEL}>Telefone (opcional)</label>
                        <Input id="register-phone" type="tel" maxLength={30} value={formData.driverPhone}
                          aria-invalid={Boolean(errors.driverPhone)} onChange={set("driverPhone")} className={INPUT} />
                        <FieldError message={errors.driverPhone} />
                      </div>
                    </div>
                    <DriverDocumentFields {...fieldProps} labelClassName={LABEL} inputClassName={INPUT} selectClassName="h-10" />
                  </div>
                </Card>

                <Card icon={ShieldCheck} title="Registro ANTT (RNTRC)" className="flex-1">
                  <RntrcFields {...fieldProps} labelClassName={LABEL} inputClassName={INPUT} selectClassName="h-10" />
                </Card>
              </div>
            </div>

            <p className="mt-4 flex items-center gap-1.5 text-[11px] font-medium text-slate-500">
              <Info size={13} className="shrink-0 text-blue-500" />
              Novos veículos entram com o status <strong className="font-bold">Livre</strong> e já podem receber leilões e dar lances.
            </p>
          </div>
        </form>
      </div>
    </AppShell>
  )
}
