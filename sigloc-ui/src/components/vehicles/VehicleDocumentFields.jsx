/* eslint-disable react/prop-types */
import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { AlertTriangle, ShieldCheck } from "lucide-react"

import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"
import { carriesBulk } from "@/constants/cargo-compatibility"
import {
  CNH_CATEGORY_BY_VALUE,
  CNH_E,
  COMPOSITION_BY_VALUE,
  RIGID_COMPOSITION,
  RNTRC_CATEGORY_BY_VALUE,
  RNTRC_SOURCE,
} from "@/constants/vehicles"
import { profileService } from "@/services/profile-service"

/*
 * Documents for running paid freight, shared by vehicle registration and the inline fleet
 * editor: composition, the RNTRC (ANTT) the vehicle runs under, the driver's CNH and the MOPP and
 * CIV/CIPP validity. The API checks the same rules; missing data on older vehicles only warns.
 */

const FieldError = ({ message }) =>
  message ? <p className="text-[11px] font-semibold text-rose-600">{message}</p> : null

const SELECT_CLASS =
  "w-full rounded-md border border-slate-200 bg-white px-3 text-sm disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500"

const formatDate = (iso) => (iso ? iso.split("-").reverse().join("/") : "")

function Field({ id, label, labelClassName, error, className, children }) {
  return (
    <div className={cn("space-y-1.5", className)}>
      <label htmlFor={id} className={labelClassName}>{label}</label>
      {children}
      <FieldError message={error} />
    </div>
  )
}

function OptionSelect({ id, value, options, placeholder, onChange, disabled, error, className }) {
  return (
    <select
      id={id}
      value={value ?? ""}
      disabled={disabled}
      aria-invalid={Boolean(error)}
      onChange={(event) => onChange(event.target.value === "" ? "" : Number(event.target.value))}
      className={cn(SELECT_CLASS, className)}
    >
      <option value="">{placeholder}</option>
      {Object.entries(options).map(([optionValue, label]) => (
        <option key={optionValue} value={optionValue}>{label}</option>
      ))}
    </select>
  )
}

export function CompositionField({ value, onChange, errors = {}, disabled, idPrefix, className, labelClassName, selectClassName }) {
  return (
    <Field id={`${idPrefix}-composition`} label="Composição" labelClassName={labelClassName} error={errors.composition} className={className}>
      <OptionSelect
        id={`${idPrefix}-composition`}
        value={value.composition}
        options={COMPOSITION_BY_VALUE}
        placeholder="Selecione..."
        disabled={disabled}
        error={errors.composition}
        onChange={(composition) => onChange({ ...value, composition })}
        className={selectClassName}
      />
    </Field>
  )
}

/** The carrier's own RNTRC from the profile, shown when the vehicle runs under it. */
export function useCarrierRntrc() {
  const [rntrc, setRntrc] = useState(undefined)
  useEffect(() => {
    let active = true
    profileService
      .getProfile()
      .then((profile) => active && setRntrc(profile))
      .catch(() => active && setRntrc(null))
    return () => {
      active = false
    }
  }, [])
  return rntrc
}

export function RntrcFields({ value, onChange, errors = {}, disabled, idPrefix, labelClassName, inputClassName, selectClassName }) {
  const carrier = useCarrierRntrc()
  const source = value.rntrcSource === "" ? "" : Number(value.rntrcSource)
  const set = (patch) => onChange({ ...value, ...patch })

  return (
    <div className="space-y-3">
      <div className="space-y-1.5">
        <span className={labelClassName}>RNTRC do veículo</span>
        <div role="radiogroup" aria-label="De quem é o RNTRC do veículo" className="flex w-fit rounded-md bg-slate-100 p-0.5">
          {[
            { value: RNTRC_SOURCE.CARRIER, label: "Da transportadora" },
            { value: RNTRC_SOURCE.THIRD_PARTY, label: "De terceiro (agregado)" },
          ].map((option) => (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={source === option.value}
              disabled={disabled}
              onClick={() => set({ rntrcSource: option.value })}
              className={cn(
                "rounded px-2.5 py-1 text-[11px] font-semibold transition-colors",
                source === option.value ? "bg-white text-slate-800" : "text-slate-500 hover:text-slate-700"
              )}
            >
              {option.label}
            </button>
          ))}
        </div>
        <FieldError message={errors.rntrcSource} />
      </div>

      {source === RNTRC_SOURCE.CARRIER && carrier !== undefined ? (
        carrier?.rntrcNumber ? (
          <p className="flex items-start gap-1.5 rounded-lg bg-slate-50 p-3 text-xs text-slate-600">
            <ShieldCheck size={14} className="mt-0.5 shrink-0 text-emerald-600" />
            <span>
              RNTRC <span className="font-mono font-bold">{carrier.rntrcNumber}</span> ·{" "}
              {RNTRC_CATEGORY_BY_VALUE[carrier.rntrcCategory] ?? "categoria não informada"}
              {carrier.rntrcValidUntil ? ` · revalidar até ${formatDate(carrier.rntrcValidUntil)}` : ""}
            </span>
          </p>
        ) : (
          <p className="flex items-start gap-1.5 rounded-lg bg-amber-50 p-3 text-xs text-amber-800">
            <AlertTriangle size={14} className="mt-0.5 shrink-0" />
            <span>
              A transportadora ainda não cadastrou o próprio RNTRC.{" "}
              <Link to="/profile" className="font-semibold underline">Cadastrar no Perfil</Link> ou use o RNTRC de terceiro.
            </span>
          </p>
        )
      ) : null}

      {source === RNTRC_SOURCE.THIRD_PARTY ? (
        <div className="grid gap-3 sm:grid-cols-2">
          <Field id={`${idPrefix}-rntrc-number`} label="Número do RNTRC" labelClassName={labelClassName} error={errors.rntrcNumber}>
            <Input id={`${idPrefix}-rntrc-number`} inputMode="numeric" maxLength={11} placeholder="8 dígitos" disabled={disabled}
              value={value.rntrcNumber} onChange={(event) => set({ rntrcNumber: event.target.value })}
              className={cn("font-mono", inputClassName)} />
          </Field>
          <Field id={`${idPrefix}-rntrc-category`} label="Categoria" labelClassName={labelClassName} error={errors.rntrcCategory}>
            <OptionSelect id={`${idPrefix}-rntrc-category`} value={value.rntrcCategory} options={RNTRC_CATEGORY_BY_VALUE}
              placeholder="Selecione..." disabled={disabled} error={errors.rntrcCategory}
              onChange={(rntrcCategory) => set({ rntrcCategory })} className={selectClassName} />
          </Field>
          <Field id={`${idPrefix}-rntrc-holder`} label="Titular" labelClassName={labelClassName} error={errors.rntrcHolderName}>
            <Input id={`${idPrefix}-rntrc-holder`} maxLength={120} placeholder="Motorista autônomo ou cooperativa" disabled={disabled}
              value={value.rntrcHolderName} onChange={(event) => set({ rntrcHolderName: event.target.value })} className={inputClassName} />
          </Field>
          <Field id={`${idPrefix}-rntrc-document`} label="CPF / CNPJ do titular" labelClassName={labelClassName} error={errors.rntrcHolderDocument}>
            <Input id={`${idPrefix}-rntrc-document`} inputMode="numeric" maxLength={18} disabled={disabled}
              placeholder={Number(value.rntrcCategory) === 1 ? "CPF (TAC é pessoa física)" : "CPF ou CNPJ"}
              value={value.rntrcHolderDocument} onChange={(event) => set({ rntrcHolderDocument: event.target.value })}
              className={cn("font-mono", inputClassName)} />
          </Field>
          <Field id={`${idPrefix}-rntrc-valid`} label="Revalidar até" labelClassName={labelClassName} error={errors.rntrcValidUntil}>
            <Input id={`${idPrefix}-rntrc-valid`} type="date" disabled={disabled}
              value={value.rntrcValidUntil} onChange={(event) => set({ rntrcValidUntil: event.target.value })} className={inputClassName} />
          </Field>
        </div>
      ) : null}
    </div>
  )
}

export function DriverDocumentFields({ value, onChange, errors = {}, disabled, idPrefix, labelClassName, inputClassName, selectClassName }) {
  const set = (patch) => onChange({ ...value, ...patch })
  const needsE = value.composition !== "" && Number(value.composition) !== RIGID_COMPOSITION
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <Field id={`${idPrefix}-cnh`} label="Categoria da CNH" labelClassName={labelClassName} error={errors.driverCnhCategory}>
        <OptionSelect id={`${idPrefix}-cnh`} value={value.driverCnhCategory} options={CNH_CATEGORY_BY_VALUE}
          placeholder="Selecione..." disabled={disabled} error={errors.driverCnhCategory}
          onChange={(driverCnhCategory) => set({ driverCnhCategory })} className={selectClassName} />
        {needsE && value.driverCnhCategory !== "" && Number(value.driverCnhCategory) !== CNH_E ? (
          <p className="text-[11px] font-medium text-amber-700">{COMPOSITION_BY_VALUE[value.composition]} exige CNH E.</p>
        ) : null}
      </Field>
      <Field id={`${idPrefix}-ear`} label="CNH com EAR" labelClassName={labelClassName} error={errors.driverHasEar}>
        <select id={`${idPrefix}-ear`} disabled={disabled} aria-invalid={Boolean(errors.driverHasEar)}
          value={value.driverHasEar === true ? "yes" : value.driverHasEar === false ? "no" : ""}
          onChange={(event) => set({ driverHasEar: event.target.value === "" ? null : event.target.value === "yes" })}
          className={cn(SELECT_CLASS, selectClassName)}>
          <option value="">Selecione...</option>
          <option value="yes">Sim</option>
          <option value="no">Não</option>
        </select>
      </Field>
      <Field id={`${idPrefix}-tox`} label="Toxicológico válido até" labelClassName={labelClassName} error={errors.driverToxicologicalExamValidUntil}>
        <Input id={`${idPrefix}-tox`} type="date" disabled={disabled} value={value.driverToxicologicalExamValidUntil}
          onChange={(event) => set({ driverToxicologicalExamValidUntil: event.target.value })} className={inputClassName} />
      </Field>
      {value.hasMopp ? (
        <Field id={`${idPrefix}-mopp`} label="MOPP válido até" labelClassName={labelClassName} error={errors.moppValidUntil}>
          <Input id={`${idPrefix}-mopp`} type="date" disabled={disabled} value={value.moppValidUntil}
            onChange={(event) => set({ moppValidUntil: event.target.value })} className={inputClassName} />
        </Field>
      ) : null}
    </div>
  )
}

/** CIV + CIPP (Inmetro) validity, only for bodies that carry bulk. */
export function CivCippField({ value, onChange, errors = {}, disabled, idPrefix, className, labelClassName, inputClassName }) {
  if (!carriesBulk(value.bodyType)) return null
  return (
    <Field id={`${idPrefix}-civ-cipp`} label="CIV/CIPP válidos até" labelClassName={labelClassName}
      error={errors.civCippValidUntil} className={className}>
      <Input id={`${idPrefix}-civ-cipp`} type="date" disabled={disabled} value={value.civCippValidUntil}
        onChange={(event) => onChange({ ...value, civCippValidUntil: event.target.value })} className={inputClassName} />
    </Field>
  )
}
