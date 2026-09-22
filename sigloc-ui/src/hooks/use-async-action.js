import { useCallback, useEffect, useRef, useState } from "react"

import { ApiError } from "@/lib/api-client"

/**
 * Generic hook for running an async action (typically an API call) from the UI
 * while tracking loading state and a normalized error.
 *
 * Returns:
 *   - `run(...args)`   — invokes the action, updating `pending`/`error`.
 *   - `pending`        — true while the action is in flight.
 *   - `error`          — `{ message, code, details }` on failure, else null.
 *   - `fieldErrors`    — map of `{ [field]: message }` from validation errors.
 *   - `reset()`        — clears the current error/field errors.
 *
 * Reusing this keeps every form free of duplicated try/catch/loading wiring.
 */
export function useAsyncAction(action) {
  const [pending, setPending] = useState(false)
  const [error, setError] = useState(null)
  const [fieldErrors, setFieldErrors] = useState({})

  // Guard against setting state after the component unmounts mid-request.
  const mountedRef = useRef(true)
  const actionRef = useRef(action)

  useEffect(() => {
    actionRef.current = action
  }, [action])

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
    }
  }, [])

  const reset = useCallback(() => {
    setError(null)
    setFieldErrors({})
  }, [])

  const run = useCallback(async (...args) => {
    setPending(true)
    setError(null)
    setFieldErrors({})

    try {
      const result = await actionRef.current(...args)
      return { ok: true, data: result }
    } catch (err) {
      if (!mountedRef.current) {
        return { ok: false, error: err }
      }

      if (err instanceof ApiError) {
        setError({ message: err.message, code: err.code, details: err.details })

        if (Array.isArray(err.details)) {
          const mapped = {}
          for (const detail of err.details) {
            if (detail?.field && !mapped[detail.field]) {
              mapped[detail.field] = detail.message
            }
          }
          setFieldErrors(mapped)
        }
      } else {
        setError({
          message: "Ocorreu um erro inesperado. Tente novamente.",
          code: "UNKNOWN_ERROR",
          details: null,
        })
      }

      return { ok: false, error: err }
    } finally {
      if (mountedRef.current) {
        setPending(false)
      }
    }
  }, [])

  return { run, pending, error, fieldErrors, reset }
}
