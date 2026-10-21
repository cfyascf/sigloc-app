/* eslint-disable react/prop-types */
import { AlertTriangle, PackageOpen, Scale } from "lucide-react"

import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { cn } from "@/lib/utils"
import {
  PRODUCT_CATEGORY_OPTIONS,
  PACKAGING_TYPE_OPTIONS,
  TRANSPORT_ENVIRONMENT_OPTIONS,
} from "@/constants/products"

function FieldLabel({ children, className }) {
  return (
    <label className={cn("text-xs font-bold text-slate-600", className)}>
      {children}
    </label>
  )
}

function FieldError({ message }) {
  if (!message) {
    return null
  }
  return <p className="text-xs font-medium text-red-600">{message}</p>
}

function ToggleCard({ active, onToggle, disabled, title, tone }) {
  const tones = {
    amber: "border-amber-300 bg-amber-50/40",
    rose: "border-rose-300 bg-rose-50/40",
    blue: "border-blue-300 bg-blue-50/40",
  }
  const checkTones = {
    amber: "border-amber-600 bg-amber-500",
    rose: "border-rose-500 bg-rose-500",
    blue: "border-blue-600 bg-blue-600",
  }

  return (
    <div
      className={cn(
        "rounded-xl border p-4 transition-all",
        active ? tones[tone] : "border-slate-200 bg-white"
      )}
    >
      <div className="flex items-start gap-3">
        <button
          type="button"
          disabled={disabled}
          onClick={onToggle}
          className={cn(
            "mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center rounded border transition-colors disabled:cursor-not-allowed",
            active ? checkTones[tone] : "border-slate-300"
          )}
        >
          {active && (
            <svg
              className="h-3 w-3 text-white"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={3}
                d="M5 13l4 4L19 7"
              />
            </svg>
          )}
        </button>
        <div className="flex-1">
          <p className="text-sm font-bold text-slate-800">{title}</p>
        </div>
      </div>
    </div>
  )
}

/**
 * Reusable product form body, shared by the create screen and the edit panel.
 *
 * Renders the three visual blocks (identity, dimensions, operational risk)
 * matching the existing design, wiring every field to the `form`/`setField`
 * pair from {@link useProductForm} and surfacing inline `fieldErrors`.
 */
export function ProductFormFields({
  form,
  setField,
  fieldErrors = {},
  showTemperature,
  showPackaging,
  disabled = false,
}) {
  return (
    <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
      {/* Bloco: Identificação */}
      <div className="rounded-xl border border-slate-200 bg-white">
        <div className="flex h-[52px] items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
          <PackageOpen size={16} className="text-blue-600" />
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">
            Identificação do SKU
          </h2>
        </div>
        <div className="space-y-4 p-5">
          <div className="space-y-2">
            <FieldLabel>Nome do Produto</FieldLabel>
            <Input
              disabled={disabled}
              placeholder="Ex: Peito de Frango Congelado"
              value={form.name}
              onChange={(e) => setField("name", e.target.value)}
              className="h-10 border-slate-200 text-sm"
            />
            <FieldError message={fieldErrors.name} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <FieldLabel>Código SKU</FieldLabel>
              <Input
                disabled={disabled}
                placeholder="Ex: FRG-001"
                value={form.sku}
                onChange={(e) => setField("sku", e.target.value)}
                className="h-10 border-slate-200 font-mono text-sm"
              />
              <FieldError message={fieldErrors.sku} />
            </div>
            <div className="space-y-2">
              <FieldLabel>Categoria</FieldLabel>
              <Select
                disabled={disabled}
                value={form.category}
                onValueChange={(value) => setField("category", value)}
              >
                <SelectTrigger className="h-10 border-slate-200 text-sm">
                  <SelectValue placeholder="Selecione..." />
                </SelectTrigger>
                <SelectContent>
                  {PRODUCT_CATEGORY_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FieldError message={fieldErrors.category} />
            </div>
          </div>
          <div className="space-y-2">
            <FieldLabel>Tipo (opcional)</FieldLabel>
            <Input
              disabled={disabled}
              placeholder="Ex: Alimentício, Químico..."
              value={form.type}
              onChange={(e) => setField("type", e.target.value)}
              className="h-10 border-slate-200 text-sm"
            />
          </div>
        </div>
      </div>

      {/* Bloco: Dimensões e Acomodação */}
      <div className="rounded-xl border border-slate-200 bg-white">
        <div className="flex h-[52px] items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
          <Scale size={16} className="text-slate-600" />
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">
            Dimensões e Acomodação
          </h2>
        </div>
        <div className="space-y-4 p-5">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <FieldLabel>Peso Base (Kg)</FieldLabel>
              <Input
                disabled={disabled}
                type="number"
                min="0"
                step="0.01"
                placeholder="0"
                value={form.defaultWeight}
                onChange={(e) => setField("defaultWeight", e.target.value)}
                className="h-10 border-slate-200 font-mono text-sm"
              />
              <FieldError message={fieldErrors.defaultWeight} />
            </div>
            <div className="space-y-2">
              <FieldLabel>Volume (m³)</FieldLabel>
              <Input
                disabled={disabled}
                type="number"
                min="0"
                step="0.01"
                placeholder="0"
                value={form.defaultVolume}
                onChange={(e) => setField("defaultVolume", e.target.value)}
                className="h-10 border-slate-200 font-mono text-sm"
              />
              <FieldError message={fieldErrors.defaultVolume} />
            </div>
          </div>
          {showPackaging && (
            <div className="space-y-2 duration-200 animate-in fade-in">
              <FieldLabel>Formato de Acomodação</FieldLabel>
              <Select
                disabled={disabled}
                value={form.packagingType}
                onValueChange={(value) => setField("packagingType", value)}
              >
                <SelectTrigger className="h-10 border-slate-200 text-sm">
                  <SelectValue placeholder="Selecione o formato..." />
                </SelectTrigger>
                <SelectContent>
                  {PACKAGING_TYPE_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FieldError message={fieldErrors.packagingType} />
            </div>
          )}
        </div>
      </div>

      {/* Bloco: Regras de Risco Operacional */}
      <div className="rounded-xl border border-slate-200 bg-white md:col-span-2">
        <div className="flex h-[52px] items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
          <AlertTriangle size={16} className="text-amber-600" />
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">
            Regras de Risco Operacional
          </h2>
        </div>

        <div className="space-y-4 p-5">
          <div
            className={cn(
              "rounded-xl border p-4 transition-all",
              showTemperature
                ? "border-sky-300 bg-sky-50/30"
                : "border-slate-200 bg-white"
            )}
          >
            <div className="space-y-2">
              <FieldLabel>Ambiente de Transporte</FieldLabel>
              <Select
                disabled={disabled}
                value={form.transportEnvironment}
                onValueChange={(value) =>
                  setField("transportEnvironment", value)
                }
              >
                <SelectTrigger className="h-10 border-slate-200 text-sm">
                  <SelectValue placeholder="Seco / Ambiente" />
                </SelectTrigger>
                <SelectContent>
                  {TRANSPORT_ENVIRONMENT_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {showTemperature && (
              <div className="mt-4 grid grid-cols-2 gap-4 duration-200 animate-in fade-in zoom-in">
                <div className="space-y-1.5">
                  <label className="text-[11px] font-bold uppercase tracking-wider text-sky-600">
                    Temp. Mínima (°C)
                  </label>
                  <Input
                    disabled={disabled}
                    type="number"
                    step="0.1"
                    placeholder="0"
                    value={form.tempMin}
                    onChange={(e) => setField("tempMin", e.target.value)}
                    className="h-10 border-sky-200 bg-sky-50 font-mono text-sm text-sky-800"
                  />
                  <FieldError message={fieldErrors.tempMin} />
                </div>
                <div className="space-y-1.5">
                  <label className="text-[11px] font-bold uppercase tracking-wider text-rose-600">
                    Temp. Máxima (°C)
                  </label>
                  <Input
                    disabled={disabled}
                    type="number"
                    step="0.1"
                    placeholder="0"
                    value={form.tempMax}
                    onChange={(e) => setField("tempMax", e.target.value)}
                    className="h-10 border-rose-200 bg-rose-50 font-mono text-sm text-rose-800"
                  />
                  <FieldError message={fieldErrors.tempMax} />
                </div>
              </div>
            )}
          </div>

          <div className="flex flex-col gap-3 pt-2">
            <ToggleCard
              tone="amber"
              active={form.dangerous}
              disabled={disabled}
              onToggle={() => setField("dangerous", !form.dangerous)}
              title="Carga Perigosa (Hazmat)"
            />
            <ToggleCard
              tone="rose"
              active={form.fragile}
              disabled={disabled}
              onToggle={() => setField("fragile", !form.fragile)}
              title="Carga Frágil"
            />
          </div>

          <div className="space-y-2 border-t border-slate-100 pt-4">
            <FieldLabel>Observação de Manuseio (Pátio e Motorista)</FieldLabel>
            <Textarea
              disabled={disabled}
              placeholder="Descreva instruções adicionais de manuseio..."
              value={form.handlingRestriction}
              onChange={(e) => setField("handlingRestriction", e.target.value)}
              className="min-h-[80px] border-slate-200 text-sm"
            />
          </div>
        </div>
      </div>
    </div>
  )
}

export default ProductFormFields
