import { Input } from "@/components/ui/input"

/** Optional fields shared by registration and the inline fleet editor. */
export default function VehicleForm({ value, onChange, disabled = false, idPrefix = "vehicle", preserveTrackerOnBlank = false }) {
  return (
    <>
      <div className="space-y-1.5">
        <label htmlFor={`${idPrefix}-phone`} className="text-xs font-bold text-slate-600">Telefone do motorista (opcional)</label>
        <Input id={`${idPrefix}-phone`} type="tel" maxLength={30} value={value.driverPhone ?? ""} onChange={(event) => onChange({ ...value, driverPhone: event.target.value })} disabled={disabled} className="h-9 border-slate-200 bg-white text-sm" />
      </div>
      <div className="space-y-1.5">
        <label htmlFor={`${idPrefix}-tracker`} className="text-xs font-bold text-slate-600">ID do dispositivo Traccar (opcional)</label>
        <Input id={`${idPrefix}-tracker`} type="number" min="1" step="1" max={Number.MAX_SAFE_INTEGER} value={value.traccarDeviceId ?? ""} onChange={(event) => onChange({ ...value, traccarDeviceId: event.target.value })} disabled={disabled} aria-describedby={preserveTrackerOnBlank ? `${idPrefix}-tracker-help` : undefined} className="h-9 border-slate-200 bg-white font-mono text-sm" />
        {preserveTrackerOnBlank && <p id={`${idPrefix}-tracker-help`} className="text-xs text-slate-500">Em branco mantém o dispositivo atual. Para substituir, informe outro ID.</p>}
      </div>
    </>
  )
}
