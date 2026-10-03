import { useEffect, useState } from "react"

/** A request owns its result only until its parameters change or it is cancelled. */
export function useTripRequest(load, requestKey) {
  const [attempt, setAttempt] = useState(0)
  const [state, setState] = useState(null)
  const key = `${requestKey}:${attempt}`

  useEffect(() => {
    const controller = new AbortController()
    Promise.resolve()
      .then(() => {
        if (controller.signal.aborted) return
        return load({ signal: controller.signal })
      })
      .then((data) => {
        if (!controller.signal.aborted) setState({ key, data, error: null })
      })
      .catch((error) => {
        if (!controller.signal.aborted) setState({ key, data: null, error })
      })
    return () => controller.abort()
  }, [load, key])

  return {
    data: state?.key === key ? state.data : null,
    error: state?.key === key ? state.error : null,
    pending: state?.key !== key,
    reload: () => setAttempt((value) => value + 1),
  }
}
