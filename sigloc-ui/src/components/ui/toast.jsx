import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react"
import { CheckCircle2, AlertTriangle, X } from "lucide-react"

import { cn } from "@/lib/utils"

const ToastContext = createContext(null)

const VARIANT_STYLES = {
  success: {
    container: "border-emerald-200 bg-emerald-50 text-emerald-900",
    icon: "text-emerald-600",
    Icon: CheckCircle2,
  },
  error: {
    container: "border-rose-200 bg-rose-50 text-rose-900",
    icon: "text-rose-600",
    Icon: AlertTriangle,
  },
}

/**
 * Lightweight, dependency-free toast system rendered as a fixed lateral stack
 * (top-right). Green for success, red for error — used by the bid workspace to
 * surface Constraint Engine rejections with the exact backend motive.
 */
export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])
  const timers = useRef(new Map())

  const dismiss = useCallback((id) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
    const timer = timers.current.get(id)
    if (timer) {
      clearTimeout(timer)
      timers.current.delete(id)
    }
  }, [])

  const show = useCallback(
    ({ variant = "success", title, description, duration = 5000 }) => {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
      setToasts((current) => [...current, { id, variant, title, description }])

      if (duration > 0) {
        const timer = setTimeout(() => dismiss(id), duration)
        timers.current.set(id, timer)
      }

      return id
    },
    [dismiss]
  )

  const value = useMemo(
    () => ({
      show,
      dismiss,
      success: (title, description) => show({ variant: "success", title, description }),
      error: (title, description) => show({ variant: "error", title, description }),
    }),
    [show, dismiss]
  )

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="pointer-events-none fixed top-4 right-4 z-[100] flex w-full max-w-sm flex-col gap-2">
        {toasts.map((toast) => {
          const styles = VARIANT_STYLES[toast.variant] ?? VARIANT_STYLES.success
          const Icon = styles.Icon
          return (
            <div
              key={toast.id}
              role="status"
              className={cn(
                "pointer-events-auto flex items-start gap-3 rounded-xl border p-3 shadow-lg",
                styles.container
              )}
            >
              <Icon size={18} className={cn("mt-0.5 shrink-0", styles.icon)} />
              <div className="min-w-0 flex-1">
                {toast.title && <p className="text-sm font-bold leading-snug">{toast.title}</p>}
                {toast.description && (
                  <p className="mt-0.5 text-xs leading-snug opacity-90">{toast.description}</p>
                )}
              </div>
              <button
                type="button"
                onClick={() => dismiss(toast.id)}
                className="shrink-0 rounded-md p-0.5 opacity-60 transition-opacity hover:opacity-100"
                aria-label="Fechar"
              >
                <X size={14} />
              </button>
            </div>
          )
        })}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error("useToast must be used within a ToastProvider")
  }
  return context
}
