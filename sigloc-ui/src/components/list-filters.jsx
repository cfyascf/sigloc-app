import { Button } from "@/components/ui/button"

/** Count of filters with a value, shown on the "Filtros" toggle. */
export const activeFilterCount = (filters) => Object.values(filters).filter(Boolean).length

/** Toggle label: "Filtros" or "Filtros (2)". */
export const filtersLabel = (filters) => {
  const count = activeFilterCount(filters)
  return count ? `Filtros (${count})` : "Filtros"
}

/**
 * Client-side filter bar opened by a list page's "Filtros" button. `filters` maps a key to the
 * selected value ("" = all); `fields` describes each select: `{ key, label, allLabel, options: [{ value, label }] }`.
 */
export function ListFilters({ filters, fields, onChange }) {
  const count = activeFilterCount(filters)
  return (
    <div className="flex flex-wrap items-center gap-3 rounded-xl border border-slate-200 bg-white p-3 text-xs">
      {fields.map((field) => (
        <select
          key={field.key}
          aria-label={field.label}
          value={filters[field.key] ?? ""}
          onChange={(event) => onChange({ ...filters, [field.key]: event.target.value })}
          className="h-9 rounded-md border border-slate-200 bg-white px-2"
        >
          <option value="">{field.allLabel}</option>
          {field.options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
      ))}
      {count > 0 && (
        <Button variant="ghost" className="h-9 text-xs text-slate-500" onClick={() => onChange(Object.fromEntries(Object.keys(filters).map((key) => [key, ""])))}>
          Limpar filtros
        </Button>
      )}
    </div>
  )
}
