import { useEffect, useState } from "react"

const HOUR_MS = 60 * 60 * 1000

/**
 * Milliseconds left until `deadline` (negative once overdue, null without a valid deadline).
 * Ticks every second within an hour of the deadline and every 30 s otherwise.
 */
export function useCountdown(deadline) {
  const target = deadline ? new Date(deadline).getTime() : Number.NaN
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    if (!Number.isFinite(target)) return undefined
    const delay = Math.abs(target - Date.now()) < HOUR_MS ? 1000 : 30000
    const timer = setTimeout(() => setNow(Date.now()), delay)
    return () => clearTimeout(timer)
  }, [target, now])

  return Number.isFinite(target) ? target - now : null
}
