/* eslint-disable react/prop-types */
import { AlertCircle } from "lucide-react"

/**
 * Inline error banner for auth forms. Renders nothing when there is no message.
 */
export function FormAlert({ message }) {
  if (!message) {
    return null
  }

  return (
    <div
      role="alert"
      className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700"
    >
      <AlertCircle size={16} className="mt-0.5 shrink-0" />
      <span>{message}</span>
    </div>
  )
}

export default FormAlert
