import {
  Box,
  CalendarClock,
  DollarSign,
  Flag,
  MapPin,
  Route as RouteIcon,
  Snowflake,
  Thermometer,
  Truck,
} from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { formatDeadline } from "@/lib/deadline-formatters"
import {
  CARGO_CLASS_BADGES,
  segmentShortId,
} from "@/constants/route-segments"
import {
  TRANSPORT_ENVIRONMENT,
  categoryLabel,
  environmentLabel,
  packagingLabel,
} from "@/constants/products"

const numberFormat = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })
const decimalFormat = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 2 })
const currencyFormat = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
  maximumFractionDigits: 2,
})

const formatCurrency = (value) => (value != null ? currencyFormat.format(value) : "—")

const CARGO_CLASS_LABEL = {
  COMUM: { label: "Comum", className: "bg-slate-100 text-slate-600" },
  ...CARGO_CLASS_BADGES,
}

const ENVIRONMENT_BADGE = {
  [TRANSPORT_ENVIRONMENT.CHILLED]: { label: "Refrigerado", className: "bg-sky-50 text-sky-700", Icon: Thermometer },
  [TRANSPORT_ENVIRONMENT.FROZEN]: { label: "Congelado", className: "bg-indigo-50 text-indigo-700", Icon: Snowflake },
}

function formatTemperatureRange(min, max) {
  if (min == null && max == null) return null
  if (min != null && max != null) return `${decimalFormat.format(min)} °C a ${decimalFormat.format(max)} °C`
  return min != null ? `mín. ${decimalFormat.format(min)} °C` : `máx. ${decimalFormat.format(max)} °C`
}

function SmallBadge({ className, children, title }) {
  return (
    <Badge
      variant="secondary"
      title={title}
      className={`text-[10px] font-bold uppercase tracking-wide border-none px-1.5 py-0 ${className}`}
    >
      {children}
    </Badge>
  )
}

function InfoField({ icon: Icon, label, children, className = "" }) {
  return (
    <div className={`min-w-0 space-y-0.5 ${className}`}>
      <div className="flex items-center gap-1 text-[9px] font-bold uppercase tracking-wider text-slate-400">
        {Icon ? <Icon size={10} /> : null} {label}
      </div>
      <div className="text-xs font-semibold text-slate-700 break-words">{children}</div>
    </div>
  )
}

function ProductHandlingBadges({ item }) {
  const environment = ENVIRONMENT_BADGE[item.transportEnvironment]
  const hasAny = item.dangerous || item.fragile || environment
  if (!hasAny) {
    return <span className="text-[11px] text-slate-400">Sem manuseio especial</span>
  }
  return (
    <div className="flex flex-wrap items-center gap-1">
      {item.dangerous && (
        <SmallBadge className={CARGO_CLASS_BADGES.PERIGOSA.className}>Perigosa</SmallBadge>
      )}
      {item.fragile && (
        <SmallBadge className={CARGO_CLASS_BADGES.FRAGIL.className}>Frágil</SmallBadge>
      )}
      {environment && (
        <SmallBadge className={`inline-flex items-center gap-1 ${environment.className}`}>
          <environment.Icon size={10} /> {environment.label}
        </SmallBadge>
      )}
    </div>
  )
}

function ProductRow({ item }) {
  const temperature = formatTemperatureRange(item.tempMin, item.tempMax)
  const details = [
    item.category ? categoryLabel(item.category) : null,
    item.packagingType ? packagingLabel(item.packagingType) : null,
    item.transportEnvironment ? environmentLabel(item.transportEnvironment) : null,
    temperature,
  ].filter(Boolean)

  return (
    <div className="grid grid-cols-[minmax(0,2fr)_minmax(0,1.3fr)_70px_90px_90px] items-start gap-4 px-3 py-2.5">
      <div className="min-w-0">
        <div className="flex min-w-0 items-center gap-2">
          <span className="shrink-0 rounded border border-slate-200 bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] font-bold tracking-wider text-slate-600 uppercase">
            {item.sku}
          </span>
          <span className="truncate text-xs font-semibold text-slate-700" title={item.name}>
            {item.name}
          </span>
        </div>
        {details.length > 0 && (
          <p className="mt-1 text-[11px] font-medium text-slate-400">{details.join(" · ")}</p>
        )}
        {item.handlingRestriction && (
          <p className="mt-1 text-[11px] text-amber-700">
            <span className="font-semibold">Restrição de manuseio:</span> {item.handlingRestriction}
          </p>
        )}
      </div>
      <ProductHandlingBadges item={item} />
      <div className="text-right text-xs font-bold text-slate-700 font-mono">
        {numberFormat.format(item.quantity)}
      </div>
      <div className="text-right text-xs font-semibold text-slate-600 font-mono">
        {numberFormat.format(item.weightSubtotal)} kg
      </div>
      <div className="text-right text-xs font-semibold text-slate-600 font-mono">
        {numberFormat.format(item.volumeSubtotal)} m³
      </div>
    </div>
  )
}

/** Read-only expanded view of a route segment: itinerary, costs, vehicle needs and products. */
export default function RouteSegmentDetails({ segment }) {
  const cargoClass = CARGO_CLASS_LABEL[segment.cargoClass] ?? CARGO_CLASS_LABEL.COMUM
  const requirement = segment.consolidatedVehicleRequirement
  const totals = segment.calculatedTotals

  return (
    <div className="border-t border-slate-100 bg-slate-50/50 p-6 animate-in slide-in-from-top-2 fade-in duration-200 rounded-b-xl space-y-5">
      <div className="grid grid-cols-1 gap-x-6 gap-y-4 md:grid-cols-4">
        <InfoField icon={MapPin} label="Local de coleta (origem)" className="md:col-span-2">
          {segment.origin || "—"}
        </InfoField>
        <InfoField icon={Flag} label="Local de entrega (destino)" className="md:col-span-2">
          {segment.destination || "—"}
        </InfoField>
        <InfoField icon={RouteIcon} label="Distância · tempo estimado">
          {numberFormat.format(segment.distanceKm)} km · ~{numberFormat.format(segment.estimatedTimeHours)}h
        </InfoField>
        <InfoField icon={DollarSign} label="Orçamento teto">
          {formatCurrency(segment.budgetCeiling)}
        </InfoField>
        <InfoField icon={DollarSign} label="Pedágio estimado">
          {formatCurrency(segment.estimatedTollCost)}
        </InfoField>
        <InfoField icon={CalendarClock} label="Criado em">
          {segment.createdAt ? formatDeadline(segment.createdAt) : "—"}
        </InfoField>
        <InfoField icon={Box} label="Classe de carga">
          <SmallBadge
            className={cargoClass.className}
            title="Trechos só podem formar uma rota com outros da mesma classe de carga."
          >
            {cargoClass.label}
          </SmallBadge>
        </InfoField>
        {segment.routeId && (
          <InfoField icon={RouteIcon} label="Rota vinculada">
            <span className="font-mono">{segmentShortId(segment.routeId)}</span>
          </InfoField>
        )}
      </div>

      {requirement && (
        <div className="rounded-lg border border-slate-200 bg-white p-3">
          <div className="mb-2 flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-500">
            <Truck size={12} className="text-slate-400" /> Requisito de veículo
          </div>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            <InfoField label="Carroceria base">{requirement.baseBodyworkType || "—"}</InfoField>
            <InfoField label="Refrigeração mínima">{requirement.minRefrigerationLevel || "—"}</InfoField>
            <InfoField label="Exige MOPP">{requirement.requiresMopp ? "Sim" : "Não"}</InfoField>
            <InfoField label="Fixação de carga">{requirement.requiresCargoFixing ? "Sim" : "Não"}</InfoField>
          </div>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="flex items-center gap-1.5 border-b border-slate-100 px-3 py-2 text-[10px] font-bold uppercase tracking-wider text-slate-500">
          <Box size={12} className="text-slate-400" /> Produtos do trecho
        </div>
        {segment.cargoClass === "MISTA" && (
          <p className="px-3 pt-2 text-[11px] font-medium text-amber-700">
            Este trecho mistura classes de carga (perigosa, frágil e comum não podem ir juntas).
          </p>
        )}
        {segment.items.length === 0 ? (
          <p className="px-3 py-3 text-[11px] text-slate-500">Nenhum produto vinculado.</p>
        ) : (
          <>
            <div className="grid grid-cols-[minmax(0,2fr)_minmax(0,1.3fr)_70px_90px_90px] gap-4 px-3 pt-2 text-[9px] font-bold uppercase tracking-wider text-slate-400">
              <span>Produto</span>
              <span>Manuseio especial</span>
              <span className="text-right">Qtd</span>
              <span className="text-right">Peso</span>
              <span className="text-right">Volume</span>
            </div>
            <div className="divide-y divide-slate-100">
              {segment.items.map((item) => (
                <ProductRow key={item.productId} item={item} />
              ))}
            </div>
            <div className="grid grid-cols-[minmax(0,2fr)_minmax(0,1.3fr)_70px_90px_90px] gap-4 border-t border-slate-100 bg-slate-50/60 px-3 py-2 text-xs font-bold text-slate-700 rounded-b-lg">
              <span className="col-span-2 text-[10px] uppercase tracking-wider text-slate-500">Total</span>
              <span className="text-right font-mono">
                {numberFormat.format(segment.items.reduce((sum, item) => sum + (item.quantity ?? 0), 0))}
              </span>
              <span className="text-right font-mono">{numberFormat.format(totals.totalWeightKg)} kg</span>
              <span className="text-right font-mono">{numberFormat.format(totals.totalVolumeM3)} m³</span>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
