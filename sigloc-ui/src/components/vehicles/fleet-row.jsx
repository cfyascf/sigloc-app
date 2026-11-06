/* eslint-disable react/prop-types */
import {
  Car,
  Columns3,
  Container,
  Cylinder,
  Droplets,
  Fence,
  Flame,
  Link2,
  Package,
  PawPrint,
  Refrigerator,
  ShieldAlert,
  ShieldCheck,
  ShieldX,
  Shovel,
  Snowflake,
  Thermometer,
  Tractor,
  Truck,
  Wheat,
} from "lucide-react"

import { cn } from "@/lib/utils"
import { categoriesFor } from "@/constants/cargo-compatibility"
import {
  BODY_TYPE,
  CNH_CATEGORY_BY_VALUE,
  COMPOSITION_BY_VALUE,
  RNTRC_CATEGORY_BY_VALUE,
  RNTRC_SOURCE,
} from "@/constants/vehicles"

/*
 * Pieces of the fleet list row (Gestão de Frota): the body type as an icon, the special
 * equipment as icon markers (same style as the segment handling icons) and the document status
 * as one shield, plus the read-only panel the row expands into.
 */

const BODY_TYPE_ICONS = {
  [BODY_TYPE.BAU]: Package,
  [BODY_TYPE.SIDER]: Columns3,
  [BODY_TYPE.GRADE_BAIXA]: Fence,
  [BODY_TYPE.REFRIGERATED]: Refrigerator,
  [BODY_TYPE.CACAMBA]: Shovel,
  [BODY_TYPE.GRANELEIRO]: Wheat,
  [BODY_TYPE.PRANCHA]: Tractor,
  [BODY_TYPE.TANQUE]: Droplets,
  [BODY_TYPE.CEGONHEIRO]: Car,
  [BODY_TYPE.PORTA_CONTEINER]: Container,
  [BODY_TYPE.SILO]: Cylinder,
  [BODY_TYPE.GAIOLA]: PawPrint,
}

const MARKER = "flex h-6 w-6 items-center justify-center rounded-full"

export function BodyTypeIcon({ bodyType }) {
  const Icon = BODY_TYPE_ICONS[bodyType] ?? Truck
  const carries = categoriesFor(bodyType)
  return (
    <span
      title={`${bodyType}${carries.length ? ` · transporta ${carries.join(", ")}` : ""}`}
      className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-600"
    >
      <Icon size={17} />
      <span className="sr-only">{bodyType}</span>
    </span>
  )
}

/** Refrigeration, MOPP and cargo securing as icon markers; "Padrão" when the vehicle has none. */
export function EquipmentIcons({ vehicle }) {
  const markers = []
  if (vehicle.refrigerationLevel === 2) {
    markers.push({ key: "frozen", Icon: Snowflake, title: "Refrigeração: congelado", className: "bg-indigo-50 text-indigo-700" })
  } else if (vehicle.refrigerationLevel === 1) {
    markers.push({ key: "chilled", Icon: Thermometer, title: "Refrigeração: resfriado", className: "bg-sky-50 text-sky-700" })
  }
  if (vehicle.hasMopp) {
    markers.push({ key: "mopp", Icon: Flame, title: "Motorista com MOPP (carga perigosa)", className: "bg-amber-100 text-amber-700" })
  }
  if (vehicle.hasCargoSecuring) {
    markers.push({ key: "securing", Icon: Link2, title: "Fixação de carga", className: "bg-slate-100 text-slate-600" })
  }

  if (markers.length === 0) {
    return (
      <span title="Sem refrigeração, MOPP ou fixação de carga" className="text-[10px] font-semibold uppercase tracking-wide text-slate-400">
        Padrão
      </span>
    )
  }
  return (
    <div className="flex items-center gap-1">
      {markers.map(({ key, Icon, title, className }) => (
        <span key={key} title={title} className={cn(MARKER, className)}>
          <Icon size={13} />
          <span className="sr-only">{title}</span>
        </span>
      ))}
    </div>
  )
}

/** Document status: green when in order, amber when something is missing or expiring, red when it blocks bids. */
export function ComplianceShield({ issues = [] }) {
  const blocking = issues.some((issue) => issue.severity === "BLOCKING")
  const { Icon, className, label } =
    issues.length === 0
      ? { Icon: ShieldCheck, className: "bg-emerald-50 text-emerald-600", label: "Documentos em dia" }
      : blocking
        ? { Icon: ShieldX, className: "bg-rose-50 text-rose-600", label: "Documentação irregular: não pode dar lances" }
        : { Icon: ShieldAlert, className: "bg-amber-50 text-amber-600", label: `Documentos pendentes (${issues.length})` }
  const title = [label, ...issues.map((issue) => `• ${issue.message}`)].join("\n")
  return (
    <span title={title} className={cn("flex h-7 w-7 items-center justify-center rounded-full", className)}>
      <Icon size={15} />
      <span className="sr-only">{label}</span>
    </span>
  )
}

const formatDate = (iso) => (iso ? iso.split("-").reverse().join("/") : null)

function Detail({ label, children }) {
  return (
    <div className="min-w-0">
      <dt className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{label}</dt>
      <dd className="break-words text-xs font-semibold text-slate-700">{children ?? <span className="text-slate-400">—</span>}</dd>
    </div>
  )
}

function Section({ title, children }) {
  return (
    <div className="space-y-2">
      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-500">{title}</p>
      <dl className="grid grid-cols-2 gap-x-4 gap-y-2">{children}</dl>
    </div>
  )
}

const yesNo = (value) => (value === true ? "Sim" : value === false ? "Não" : null)

/** Read-only panel under the row: equipment, driver, RNTRC and everything the documents still lack. */
export function VehicleDetails({ vehicle }) {
  const thirdParty = Number(vehicle.rntrcSource) === RNTRC_SOURCE.THIRD_PARTY
  const blocking = vehicle.compliance.filter((issue) => issue.severity === "BLOCKING")
  const warnings = vehicle.compliance.filter((issue) => issue.severity !== "BLOCKING")

  return (
    <div className="grid gap-6 border-t border-slate-100 bg-slate-50/50 p-6 md:grid-cols-2 xl:grid-cols-4 rounded-b-xl">
      <Section title="Equipamento">
        <Detail label="Carroceria">{vehicle.bodyType}</Detail>
        <Detail label="Composição">{COMPOSITION_BY_VALUE[vehicle.composition]}</Detail>
        <Detail label="Eixos">{vehicle.axleCount || null}</Detail>
        <Detail label="Refrigeração">{["Nenhuma", "Resfriado", "Congelado"][vehicle.refrigerationLevel]}</Detail>
        <Detail label="Fixação de carga">{yesNo(vehicle.hasCargoSecuring)}</Detail>
        <Detail label="CIV/CIPP até">{formatDate(vehicle.civCippValidUntil)}</Detail>
      </Section>

      <Section title="Motorista">
        <Detail label="Nome">{vehicle.driver || null}</Detail>
        <Detail label="Telefone">{vehicle.driverPhone || null}</Detail>
        <Detail label="CNH">{CNH_CATEGORY_BY_VALUE[vehicle.driverCnhCategory]}</Detail>
        <Detail label="EAR">{yesNo(vehicle.driverHasEar)}</Detail>
        <Detail label="Toxicológico até">{formatDate(vehicle.driverToxicologicalExamValidUntil)}</Detail>
        <Detail label="MOPP">{vehicle.hasMopp ? (formatDate(vehicle.moppValidUntil) ? `Até ${formatDate(vehicle.moppValidUntil)}` : "Sim") : "Não"}</Detail>
      </Section>

      <Section title="Registro ANTT (RNTRC)">
        <Detail label="Vínculo">
          {Number(vehicle.rntrcSource) === RNTRC_SOURCE.CARRIER ? "Da transportadora" : thirdParty ? "De terceiro (agregado)" : null}
        </Detail>
        {thirdParty ? (
          <>
            <Detail label="Número"><span className="font-mono">{vehicle.rntrcNumber}</span></Detail>
            <Detail label="Categoria">{RNTRC_CATEGORY_BY_VALUE[vehicle.rntrcCategory]?.split(" ")[0]}</Detail>
            <Detail label="Revalidar até">{formatDate(vehicle.rntrcValidUntil)}</Detail>
            <Detail label="Titular">{vehicle.rntrcHolderName || null}</Detail>
          </>
        ) : null}
        <Detail label="Rastreador">{vehicle.traccarDeviceId ? `Traccar #${vehicle.traccarDeviceId}` : null}</Detail>
      </Section>

      <div className="space-y-2">
        <p className="text-[10px] font-bold uppercase tracking-wider text-slate-500">Pendências</p>
        {vehicle.compliance.length === 0 ? (
          <p className="flex items-center gap-1.5 text-xs font-semibold text-emerald-700">
            <ShieldCheck size={14} /> Documentos em dia.
          </p>
        ) : (
          <ul className="space-y-1.5">
            {[...blocking, ...warnings].map((issue) => (
              <li
                key={issue.code}
                className={cn(
                  "flex items-start gap-1.5 text-xs",
                  issue.severity === "BLOCKING" ? "font-semibold text-rose-700" : "text-amber-800"
                )}
              >
                {issue.severity === "BLOCKING" ? <ShieldX size={13} className="mt-0.5 shrink-0" /> : <ShieldAlert size={13} className="mt-0.5 shrink-0" />}
                {issue.message}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
