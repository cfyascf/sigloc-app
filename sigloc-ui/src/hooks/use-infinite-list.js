import { useCallback, useEffect, useState } from "react"

/**
 * Generic infinite-scroll list loader for the paged list endpoints. Pages are accumulated for the
 * current filter signature (`filterKey`): a new signature resets the list, and calling `loadMore()`
 * fetches the next page and appends its items.
 *
 * State is only ever set from async callbacks (`.then`/`.catch`), never synchronously inside an
 * effect, mirroring `useTripRequest` so the React Compiler lint rules stay satisfied. `pending` is
 * derived by comparing the in-flight request key with the one the stored result belongs to.
 *
 * `loadPage(page, options)` must resolve to `{ items, totalPages }`.
 */
export function useInfiniteList(loadPage, filterKey) {
  // `target` is the request we want loaded; `state` holds the accumulated result and the key it
  // belongs to. When their request keys disagree a request is in flight (pending).
  const [target, setTarget] = useState({ key: filterKey, page: 1, attempt: 0 })
  const [state, setState] = useState(null)

  // A changed filter signature implicitly resets the target to page 1.
  const active = target.key === filterKey ? target : { key: filterKey, page: 1, attempt: 0 }
  const requestKey = `${active.key}:${active.page}:${active.attempt}`
  const matches = state?.requestKey === requestKey

  useEffect(() => {
    const controller = new AbortController()
    Promise.resolve()
      .then(() => {
        if (controller.signal.aborted) return undefined
        return loadPage(active.page, { signal: controller.signal })
      })
      .then((data) => {
        if (!data || controller.signal.aborted) return
        setState((current) => {
          const sameKey = current?.key === active.key
          const previousItems = sameKey && active.page > 1 ? current.items : []
          return {
            requestKey,
            key: active.key,
            items: [...previousItems, ...(data.items ?? [])],
            page: active.page,
            totalPages: data.totalPages ?? 1,
            error: null,
          }
        })
      })
      .catch((error) => {
        if (controller.signal.aborted) return
        setState((current) => {
          const sameKey = current?.key === active.key
          return {
            requestKey,
            key: active.key,
            items: sameKey ? current.items : [],
            page: sameKey ? current.page : 0,
            totalPages: sameKey ? current.totalPages : 1,
            error,
          }
        })
      })
    return () => controller.abort()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loadPage, requestKey])

  const result = state?.key === filterKey ? state : null
  const items = result?.items ?? []
  const page = result?.page ?? 0
  const totalPages = result?.totalPages ?? 1
  const error = matches ? (result?.error ?? null) : null
  const pending = !matches
  const hasMore = page > 0 && page < totalPages

  const loadMore = useCallback(() => {
    if (!matches || !hasMore) return
    setTarget({ key: filterKey, page: page + 1, attempt: 0 })
  }, [matches, hasMore, filterKey, page])

  const reload = useCallback(() => {
    setTarget((current) => ({ key: filterKey, page: 1, attempt: current.attempt + 1 }))
  }, [filterKey])

  return { items, pending, error, hasMore, loadMore, reload }
}
