import { useEffect, useMemo, useState } from "react"

import { productService } from "@/services/product-service"
import { categoryLabel, environmentLabel } from "@/constants/products"

/**
 * Maps an API list item into the option shape consumed by the product pickers
 * ({ id, value, sku, name, details }). `value` is the product id so selections
 * carry the real identifier used by downstream requests.
 */
function toOption(item) {
  return {
    id: item.id,
    value: item.id,
    sku: item.sku,
    name: item.name,
    details: `${categoryLabel(item.category)} • ${environmentLabel(
      item.transportEnvironment
    )}`,
  }
}

/**
 * Fetches the current contractor's products once and exposes them as picker
 * options. Shared by the load and route-segment screens so both stay in sync
 * with the real catalog instead of hardcoded mocks.
 */
export function useProductOptions() {
  const [options, setOptions] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    let active = true
    const controller = new AbortController()

    async function load() {
      setLoading(true)
      setError(null)
      try {
        const result = await productService.listProducts(
          { page: 1, pageSize: 100 },
          { signal: controller.signal }
        )
        if (active) {
          setOptions(result.items.map(toOption))
        }
      } catch (err) {
        if (active && err?.name !== "AbortError") {
          setError(err)
        }
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    }

    load()

    return () => {
      active = false
      controller.abort()
    }
  }, [])

  return useMemo(
    () => ({ options, loading, error }),
    [options, loading, error]
  )
}
