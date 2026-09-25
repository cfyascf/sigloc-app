/* eslint-disable react/prop-types */
import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"

/**
 * Labeled input with inline field-level error, matching the auth screen style.
 * Keeps every form field consistent and reduces boilerplate on the Auth page.
 */
export function FormField({ id, label, error, className, ...inputProps }) {
  return (
    <div className="space-y-2">
      {label ? (
        <label className="text-sm font-medium text-slate-700" htmlFor={id}>
          {label}
        </label>
      ) : null}
      <Input
        id={id}
        aria-invalid={error ? true : undefined}
        className={cn("border-slate-200", className)}
        {...inputProps}
      />
      {error ? <p className="text-xs font-medium text-red-600">{error}</p> : null}
    </div>
  )
}

export default FormField
