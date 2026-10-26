/* eslint-disable react/prop-types */
import { useEffect, useRef } from "react"
import { Loader2 } from "lucide-react"

/**
 * Shared infinite-scroll footer for list pages. Renders an invisible sentinel that triggers
 * `onLoadMore` when it scrolls into view, a "loading more" spinner, and an end-of-list summary.
 *
 * - `hasMore`: whether another page can be loaded.
 * - `loadingMore`: a next-page request is in flight.
 * - `onLoadMore`: called when the sentinel becomes visible.
 * - `count`: number of items currently loaded (for the end-of-list summary).
 * - `singular` / `plural`: item noun used in the summary ("viagem"/"viagens").
 */
export function InfiniteScrollSentinel({ hasMore, loadingMore, onLoadMore, count, singular = "item", plural = "itens" }) {
  const sentinelRef = useRef(null)

  useEffect(() => {
    const node = sentinelRef.current
    if (!node || !hasMore || loadingMore) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) {
          onLoadMore()
        }
      },
      { rootMargin: "200px" }
    )
    observer.observe(node)
    return () => observer.disconnect()
  }, [hasMore, loadingMore, onLoadMore])

  if (count === 0) return null

  const noun = count === 1 ? singular : plural

  return (
    <>
      {hasMore && <div ref={sentinelRef} aria-hidden className="h-px w-full" />}

      {loadingMore ? (
        <p className="flex items-center justify-center gap-2 py-3 text-xs text-slate-500">
          <Loader2 size={14} className="animate-spin" /> Carregando mais…
        </p>
      ) : hasMore ? (
        <p className="py-3 text-center text-[11px] text-slate-400">
          {count} {noun} • role para carregar mais
        </p>
      ) : (
        <p className="py-3 text-center text-[11px] text-slate-400">
          {count} {noun} • fim da lista
        </p>
      )}
    </>
  )
}
