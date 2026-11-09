/* eslint-disable react/prop-types */
import { useId } from "react"
import {
  AlertTriangle,
  Info,
  Link2,
  Layers,
  PackageOpen,
  Snowflake,
  Tag,
  Truck,
} from "lucide-react"

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
  PRODUCT_LIMITS,
  TRANSPORT_ENVIRONMENT,
  TRANSPORT_ENVIRONMENT_OPTIONS,
  categoryLabel,
} from "@/constants/products"
import {
  acceptedBodies,
  allowsDangerous,
  allowsFragile,
  describeList,
  environmentsFor,
  isSingleProductCategory,
} from "@/constants/cargo-compatibility"
import {
  cargoClassName,
  cargoClassOfProduct,
  formatTemperatureRange,
} from "@/constants/route-segments"

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

function ToggleCard({ active, onToggle, disabled, title, hint, tone }) {
  const hintId = useId()
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
        "rounded-xl border p-3 transition-all",
        active ? tones[tone] : "border-slate-200 bg-white"
      )}
    >
      <div className="flex items-start gap-3">
        <button
          type="button"
          role="checkbox"
          aria-checked={Boolean(active)}
          aria-label={title}
          aria-describedby={hint ? hintId : undefined}
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
        <div className="min-w-0 flex-1 break-words">
          <p
            className={cn(
              "text-sm font-bold",
              disabled ? "text-slate-400" : "text-slate-800"
            )}
          >
            {title}
          </p>
          {hint && <p id={hintId} className="mt-0.5 text-xs text-slate-500">{hint}</p>}
        </div>
      </div>
    </div>
  )
}

function SectionCard({
  icon: Icon,
  iconClassName,
  title,
  className,
  bodyClassName,
  children,
}) {
  return (
    <div
      className={cn(
        "flex flex-col rounded-xl border border-slate-200 bg-white",
        className
      )}
    >
      <div className="flex h-[52px] items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-5">
        <Icon size={16} className={iconClassName} />
        <h2 className="text-xs font-bold tracking-wider text-slate-700 uppercase">
          {title}
        </h2>
      </div>
      <div className={cn("flex-1 space-y-4 p-5", bodyClassName)}>
        {children}
      </div>
    </div>
  )
}

const REFRIGERATION_LABEL = {
  [TRANSPORT_ENVIRONMENT.DRY]: "Nenhuma",
  [TRANSPORT_ENVIRONMENT.CHILLED]: "Resfriado",
  [TRANSPORT_ENVIRONMENT.FROZEN]: "Congelado",
}

function SummaryRow({ icon: Icon, label, value, active }) {
  return (
    <div className="flex items-center justify-between gap-3 py-2">
      <span className="flex items-center gap-2 text-xs font-medium text-slate-500">
        <Icon size={14} className="text-slate-400" /> {label}
      </span>
      <span
        className={cn(
          "text-xs font-bold",
          active ? "text-amber-700" : "text-slate-700"
        )}
      >
        {value}
      </span>
    </div>
  )
}

/**
 * The vehicle a carrier needs for this product, worked out from the form as it is filled in
 * (same rules the API applies when it computes the requirement and audits bids).
 */
function VehicleRequirementSummary({ form }) {
  const categoryHint = PRODUCT_CATEGORY_OPTIONS.find(
    (option) => option.value === form.category
  )?.hint
  const bodies = form.category
    ? acceptedBodies(form.category, form.transportEnvironment, form.fragile)
    : []
  const refrigeration =
    REFRIGERATION_LABEL[form.transportEnvironment] ?? "Nenhuma"
  const cargoClass = cargoClassName(cargoClassOfProduct(form))
  const companions = shareableWith(form, cargoClass)

  return (
    <div className="order-first flex flex-col rounded-xl border border-blue-200 bg-white @4xl:order-none">
      <div className="flex h-[52px] items-center gap-2 rounded-t-xl border-b border-blue-100 bg-blue-50/60 px-5">
        <Truck size={16} className="text-blue-600" />
        <h2 className="text-xs font-bold tracking-wider text-slate-700 uppercase">
          Requisito de Veículo
        </h2>
      </div>
      <div className="flex flex-1 flex-col gap-3 p-5">
        <p className="text-[11px] text-slate-500">
          Calculado a partir da carga. Atualiza enquanto você preenche.
        </p>

        <div className="space-y-2">
          <p className="text-[11px] font-bold tracking-wider text-slate-400 uppercase">
            Carrocerias aceitas
          </p>
          {bodies.length > 0 ? (
            <div className="flex flex-wrap gap-1.5">
              {bodies.map((body) => (
                <span
                  key={body}
                  className="rounded-md border border-blue-200 bg-blue-50 px-2 py-0.5 text-xs font-semibold text-blue-700"
                >
                  {body}
                </span>
              ))}
            </div>
          ) : (
            <p className="text-xs font-semibold text-rose-600">
              {describeList(bodies)}
            </p>
          )}
        </div>

        <div className="divide-y divide-slate-100 border-t border-slate-100">
          <SummaryRow
            icon={Tag}
            label="Classe de carga"
            value={cargoClass
              .replace("carga ", "")
              .replace(/^./, (c) => c.toUpperCase())}
            active={form.dangerous || form.fragile}
          />
          <SummaryRow
            icon={Snowflake}
            label="Refrigeração"
            value={refrigeration}
            active={refrigeration !== "Nenhuma"}
          />
          <SummaryRow
            icon={AlertTriangle}
            label="Motorista com MOPP"
            value={form.dangerous ? "Exigido" : "Não exigido"}
            active={form.dangerous}
          />
          <SummaryRow
            icon={Link2}
            label="Fixação de carga"
            value={form.fragile ? "Exigida" : "Não exigida"}
            active={form.fragile}
          />
        </div>

        {categoryHint && (
          <p className="flex items-start gap-1.5 rounded-lg bg-slate-50 p-3 text-xs text-slate-600">
            <Info size={13} className="mt-0.5 shrink-0 text-blue-500" />
            <span>
              <span className="font-bold">{categoryLabel(form.category)}:</span>{" "}
              {categoryHint}
            </span>
          </p>
        )}

        {/* Pinned to the bottom so the panel ends level with the form beside it. */}
        {companions && (
          <div className="mt-auto rounded-lg border border-dashed border-slate-200 p-3 text-xs text-slate-600">
            <p className="mb-1 text-[11px] font-bold tracking-wider text-slate-400 uppercase">
              Divide trecho e rota com
            </p>
            {companions}
          </div>
        )}
      </div>
    </div>
  )
}

/**
 * What this product can travel with: segments and routes hold one category, one cargo class
 * and one temperature (same rules the API enforces when saving segments and consolidating routes).
 */
function shareableWith(form, cargoClass) {
  if (!form.category) return null
  if (isSingleProductCategory(form.category)) {
    return `Nenhum outro: ${categoryLabel(form.category)} leva um único produto por trecho, e cada trecho é ofertado sozinho.`
  }
  const min = form.tempMin === "" ? NaN : Number(form.tempMin)
  const max = form.tempMax === "" ? NaN : Number(form.tempMax)
  const temperature =
    form.transportEnvironment === TRANSPORT_ENVIRONMENT.DRY
      ? "em temperatura ambiente"
      : Number.isFinite(min) && Number.isFinite(max)
        ? `que tolerem ${formatTemperatureRange({ min, max })}`
        : "na mesma faixa de temperatura"
  return `Produtos ${categoryLabel(form.category)} de ${cargoClass}, ${temperature}.`
}

/**
 * Reusable product form body, shared by the create screen and the edit panel.
 *
 * One compact card: identification across the top, then the cargo fields
 * (dimensions, classification, temperature) beside the risk flags and the
 * handling note, so the register page fits the screen. A side panel shows the
 * vehicle requirement live. Container queries size it to the space it gets:
 * side by side on the register page, stacked in the narrow edit panel. Every
 * field is wired to the `form`/`setField` pair from {@link useProductForm} and
 * surfaces inline `fieldErrors`.
 */
export function ProductFormFields({
  form,
  setField,
  fieldErrors = {},
  showTemperature,
  showPackaging,
  disabled = false,
}) {
  const environmentOptions = TRANSPORT_ENVIRONMENT_OPTIONS.filter((option) =>
    environmentsFor(form.category).includes(option.value)
  )
  const fragileAllowed = allowsFragile(form.category)
  const dangerousAllowed = allowsDangerous(form.category)

  return (
    <div className="@container">
      <div className="grid grid-cols-1 gap-6 @4xl:grid-cols-[minmax(0,1fr)_300px]">
        <div className="flex flex-col gap-6">
          <SectionCard
            icon={PackageOpen}
            iconClassName="text-blue-600"
            title="Identificação do SKU"
          >
            <div className="grid grid-cols-1 gap-x-4 gap-y-3 @md:grid-cols-2 @2xl:grid-cols-3">
              <div className="space-y-1.5 @md:col-span-2 @2xl:col-span-1">
                <FieldLabel>Nome do Produto</FieldLabel>
                <Input
                  disabled={disabled}
                  placeholder="Ex: Peito de Frango Congelado"
                  value={form.name}
                  maxLength={PRODUCT_LIMITS.name}
                  onChange={(e) => setField("name", e.target.value)}
                  className="h-10 border-slate-200 text-sm"
                />
                <FieldError message={fieldErrors.name} />
              </div>
              <div className="space-y-1.5">
                <FieldLabel>Código SKU</FieldLabel>
                <Input
                  disabled={disabled}
                  placeholder="Ex: FRG-001"
                  value={form.sku}
                  maxLength={PRODUCT_LIMITS.sku}
                  onChange={(e) => setField("sku", e.target.value)}
                  className="h-10 border-slate-200 font-mono text-sm"
                />
                <FieldError message={fieldErrors.sku} />
              </div>
              <div className="space-y-1.5">
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
          </SectionCard>

          {/* Stretches so both cards end level with the requirement panel beside them. */}
          <div className="grid flex-1 grid-cols-1 gap-6 @3xl:grid-cols-2">
            <SectionCard
              icon={Layers}
              iconClassName="text-blue-600"
              title="Carga"
            >
              <div className="grid grid-cols-2 content-start gap-x-4 gap-y-3">
                <div className="space-y-1.5">
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
                <div className="space-y-1.5">
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

                <div className="space-y-1.5">
                  <FieldLabel>Categoria</FieldLabel>
                  <Select
                    disabled={disabled}
                    value={form.category}
                    onValueChange={(value) => setField("category", value)}
                  >
                    <SelectTrigger className="h-10 w-full border-slate-200 text-sm">
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
                <div className="space-y-1.5">
                  <FieldLabel>Ambiente de Transporte</FieldLabel>
                  <Select
                    disabled={disabled}
                    value={form.transportEnvironment}
                    onValueChange={(value) =>
                      setField("transportEnvironment", value)
                    }
                  >
                    <SelectTrigger className="h-10 w-full border-slate-200 text-sm">
                      <SelectValue placeholder="Seco / Ambiente" />
                    </SelectTrigger>
                    <SelectContent>
                      {environmentOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FieldError message={fieldErrors.transportEnvironment} />
                </div>
                {/* Full row under category and environment, so it fits on one line. */}
                {environmentOptions.length <
                  TRANSPORT_ENVIRONMENT_OPTIONS.length && (
                  <p className="col-span-2 -mt-3 text-[11px] leading-snug text-slate-500">
                    {environmentOptions.length > 1
                      ? "Até resfriado: nenhuma carroceria desta categoria congela."
                      : "Somente seco: nenhuma carroceria desta categoria refrigera."}
                  </p>
                )}

                {/* Always rendered so nothing moves; only Geral has a packaging format. */}
                <div className="space-y-1.5">
                  <FieldLabel>Formato de Acomodação</FieldLabel>
                  <Select
                    disabled={disabled || !showPackaging}
                    value={showPackaging ? form.packagingType : ""}
                    onValueChange={(value) => setField("packagingType", value)}
                  >
                    <SelectTrigger className="h-10 w-full border-slate-200 text-sm">
                      <SelectValue
                        placeholder={
                          showPackaging ? "Selecione..." : "Não se aplica"
                        }
                      />
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
                {/* Also always rendered; only chilled or frozen loads take a temperature range. */}
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <label
                      className={cn(
                        "text-xs font-bold",
                        showTemperature ? "text-sky-600" : "text-slate-600"
                      )}
                    >
                      Mín. (°C)
                    </label>
                    <Input
                      disabled={disabled || !showTemperature}
                      type="number"
                      step="0.1"
                      placeholder={showTemperature ? "0" : "—"}
                      value={showTemperature ? form.tempMin : ""}
                      onChange={(e) => setField("tempMin", e.target.value)}
                      className={cn(
                        "h-10 font-mono text-sm",
                        showTemperature
                          ? "border-sky-200 bg-sky-50 text-sky-800"
                          : "border-slate-200"
                      )}
                    />
                    <FieldError message={fieldErrors.tempMin} />
                  </div>
                  <div className="space-y-1.5">
                    <label
                      className={cn(
                        "text-xs font-bold",
                        showTemperature ? "text-rose-600" : "text-slate-600"
                      )}
                    >
                      Máx. (°C)
                    </label>
                    <Input
                      disabled={disabled || !showTemperature}
                      type="number"
                      step="0.1"
                      placeholder={showTemperature ? "0" : "—"}
                      value={showTemperature ? form.tempMax : ""}
                      onChange={(e) => setField("tempMax", e.target.value)}
                      className={cn(
                        "h-10 font-mono text-sm",
                        showTemperature
                          ? "border-rose-200 bg-rose-50 text-rose-800"
                          : "border-slate-200"
                      )}
                    />
                    <FieldError message={fieldErrors.tempMax} />
                  </div>
                </div>
              </div>
            </SectionCard>

            <SectionCard
              icon={AlertTriangle}
              iconClassName="text-amber-600"
              title="Riscos e Manuseio"
              bodyClassName="flex flex-col gap-3 space-y-0"
            >
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <ToggleCard
                    tone="amber"
                    active={form.dangerous}
                    disabled={disabled || !dangerousAllowed}
                    onToggle={() => setField("dangerous", !form.dangerous)}
                    title="Carga Perigosa"
                    hint={
                      dangerousAllowed
                        ? "Exige motorista com MOPP."
                        : `Indisponível para ${categoryLabel(form.category)}.`
                    }
                  />
                  <FieldError message={fieldErrors.dangerous} />
                </div>
                <div className="space-y-1">
                  <ToggleCard
                    tone="rose"
                    active={form.fragile}
                    disabled={disabled || !fragileAllowed}
                    onToggle={() => setField("fragile", !form.fragile)}
                    title="Carga Frágil"
                    hint={
                      fragileAllowed
                        ? "Exige carroceria fechada e fixação."
                        : "Só Geral ou Conteinerizada."
                    }
                  />
                  <FieldError message={fieldErrors.fragile} />
                </div>
              </div>

              <div className="flex flex-1 flex-col gap-1.5">
                <FieldLabel>
                  Observação de Manuseio (Pátio e Motorista)
                </FieldLabel>
                <Textarea
                  disabled={disabled}
                  placeholder="Descreva instruções adicionais de manuseio..."
                  value={form.handlingRestriction}
                  maxLength={PRODUCT_LIMITS.handlingRestriction}
                  onChange={(e) =>
                    setField("handlingRestriction", e.target.value)
                  }
                  className="min-h-[80px] flex-1 resize-none border-slate-200 text-sm"
                />
                <FieldError message={fieldErrors.handlingRestriction} />
              </div>
            </SectionCard>
          </div>
        </div>

        <VehicleRequirementSummary form={form} />
      </div>
    </div>
  )
}

export default ProductFormFields
