/* eslint-disable react/prop-types */
import { Info } from "lucide-react"

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { cn } from "@/lib/utils"
import { BODY_TYPE_OPTIONS, REFRIGERATION_BY_VALUE } from "@/constants/vehicles"
import {
  categoriesFor,
  normalizeVehicleEquipment,
  refrigerationLevelsFor,
  requiresCargoSecuring,
} from "@/constants/cargo-compatibility"

/*
 * Equipment fields shared by vehicle registration and the inline fleet editor. The body type
 * decides which refrigeration the vehicle can have and whether cargo securing is mandatory, so
 * every change goes through `normalizeVehicleEquipment` (the API enforces the same rules).
 */

const FieldError = ({ message }) =>
  message ? <p className="text-[11px] font-semibold text-rose-600">{message}</p> : null

const Help = ({ children }) => <p className="text-[11px] font-medium text-slate-500">{children}</p>

const update = (value, onChange, patch) => onChange(normalizeVehicleEquipment({ ...value, ...patch }))

export function BodyTypeField({ value, onChange, errors = {}, disabled, className, labelClassName, triggerClassName }) {
  const categories = value.bodyType ? categoriesFor(value.bodyType) : []
  return (
    <div className={cn("space-y-2", className)}>
      <label className={labelClassName}>Tipo de Carroceria</label>
      <Select value={value.bodyType} disabled={disabled} onValueChange={(bodyType) => update(value, onChange, { bodyType })}>
        <SelectTrigger aria-invalid={Boolean(errors.bodyType)} className={triggerClassName}>
          <SelectValue placeholder="Selecione o implemento..." />
        </SelectTrigger>
        <SelectContent>
          {BODY_TYPE_OPTIONS.map((option) => <SelectItem key={option} value={option}>{option}</SelectItem>)}
        </SelectContent>
      </Select>
      {categories.length > 0 && (
        <p className="flex items-center gap-1.5 text-[11px] font-medium text-slate-500">
          <Info size={12} className="shrink-0 text-blue-500" /> Transporta: {categories.join(", ")}.
        </p>
      )}
      <FieldError message={errors.bodyType} />
    </div>
  )
}

export function RefrigerationField({ value, onChange, errors = {}, disabled, idPrefix, className, labelClassName, selectClassName }) {
  const levels = value.bodyType ? refrigerationLevelsFor(value.bodyType) : Object.keys(REFRIGERATION_BY_VALUE).map(Number)
  const locked = levels.length === 1
  return (
    <div className={cn("space-y-2", className)}>
      <label htmlFor={`${idPrefix}-refrigeration`} className={labelClassName}>Refrigeração</label>
      <select
        id={`${idPrefix}-refrigeration`}
        value={value.refrigerationLevel ?? 0}
        disabled={disabled || locked}
        aria-invalid={Boolean(errors.refrigerationLevel)}
        onChange={(event) => update(value, onChange, { refrigerationLevel: Number(event.target.value) })}
        className={cn("w-full rounded-md border border-slate-200 bg-white px-3 text-sm disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500", selectClassName)}
      >
        {levels.map((level) => <option key={level} value={level}>{REFRIGERATION_BY_VALUE[level]}</option>)}
      </select>
      {locked && value.bodyType && <Help>{value.bodyType} não tem refrigeração.</Help>}
      <FieldError message={errors.refrigerationLevel} />
    </div>
  )
}

export function EquipmentChecks({ value, onChange, errors = {}, disabled, idPrefix, className }) {
  const securingRequired = requiresCargoSecuring(value.bodyType)
  return (
    <div className={cn("grid gap-4 sm:grid-cols-2", className)}>
      <label className="flex items-start gap-2 text-xs text-slate-600">
        <input type="checkbox" className="mt-0.5" disabled={disabled} aria-describedby={`${idPrefix}-mopp-help`} checked={Boolean(value.hasMopp)} onChange={(event) => update(value, onChange, { hasMopp: event.target.checked })} />
        <span>
          <span className="block font-medium">Motorista com MOPP</span>
          <span id={`${idPrefix}-mopp-help`} className="block text-[11px] text-slate-500">
            Curso obrigatório para o motorista transportar produtos perigosos.
          </span>
        </span>
      </label>
      <label className="flex items-start gap-2 text-xs text-slate-600">
        <input type="checkbox" className="mt-0.5" disabled={disabled || securingRequired} aria-describedby={`${idPrefix}-cargo-securing-help`} checked={Boolean(value.hasCargoSecuring)} onChange={(event) => update(value, onChange, { hasCargoSecuring: event.target.checked })} />
        <span>
          <span className="block font-medium">Possui fixação de carga</span>
          <span id={`${idPrefix}-cargo-securing-help`} className="block text-[11px] text-slate-500">
            {securingRequired
              ? `Obrigatória na carroceria ${value.bodyType}: a carga deve ser amarrada (CONTRAN Res. 945/2022).`
              : "Cintas, barras e travas. Exigida para cargas frágeis."}
          </span>
          <FieldError message={errors.hasCargoSecuring} />
        </span>
      </label>
    </div>
  )
}
