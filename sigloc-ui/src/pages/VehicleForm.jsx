import { Input } from "@/components/ui/input"

/** Optional fields shared by registration and the inline fleet editor. */
export default function VehicleForm({ value, onChange, disabled = false, idPrefix = "vehicle", preserveTrackerOnBlank = false, errors = {}, inputClassName = "h-9" }) {
  return (
    <>
      <div className="space-y-1.5">
        <label htmlFor={`${idPrefix}-phone`} className="text-xs font-bold text-slate-600">Telefone do motorista (opcional)</label>
        <Input id={`${idPrefix}-phone`} type="tel" maxLength={30} value={value.driverPhone ?? ""} aria-invalid={Boolean(errors.driverPhone)} onChange={(event) => onChange({ ...value, driverPhone: event.target.value })} disabled={disabled} className={`${inputClassName} border-slate-200 bg-white text-sm`} />
        {errors.driverPhone && <p className="text-[11px] font-semibold text-rose-600">{errors.driverPhone}</p>}
      </div>
      <div className="space-y-1.5">
        <label htmlFor={`${idPrefix}-tracker`} className="text-xs font-bold text-slate-600">ID do dispositivo Traccar (opcional)</label>
        <Input id={`${idPrefix}-tracker`} type="number" value={value.traccarDeviceId ?? ""} aria-invalid={Boolean(errors.traccarDeviceId)} onChange={(event) => onChange({ ...value, traccarDeviceId: event.target.value })} disabled={disabled} aria-describedby={`${idPrefix}-tracker-help`} className={`${inputClassName} border-slate-200 bg-white font-mono text-sm`} />
        {preserveTrackerOnBlank
          ? <p id={`${idPrefix}-tracker-help`} className="text-xs text-slate-500">Em branco mantém o dispositivo atual. Para substituir, informe outro ID.</p>
          : <p id={`${idPrefix}-tracker-help`} className="text-[11px] font-medium text-amber-700">Sem o ID do dispositivo não será possível rastrear o veículo por GPS.</p>}
        {errors.traccarDeviceId && <p className="text-[11px] font-semibold text-rose-600">{errors.traccarDeviceId}</p>}
      </div>
    </>
  )
}
