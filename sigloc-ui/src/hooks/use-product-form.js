import { useCallback, useMemo, useState } from "react"

import {
  PRODUCT_CATEGORY,
  TRANSPORT_ENVIRONMENT,
  requiresPackaging,
  requiresTemperature,
} from "@/constants/products"

/** The empty form model, aligned with `ProductRequestDto`. */
export const EMPTY_PRODUCT_FORM = {
  sku: "",
  name: "",
  type: "",
  category: PRODUCT_CATEGORY.GENERAL,
  transportEnvironment: TRANSPORT_ENVIRONMENT.DRY,
  tempMin: "",
  tempMax: "",
  packagingType: "",
  dangerous: false,
  fragile: false,
  defaultWeight: "",
  defaultVolume: "",
  handlingRestriction: "",
}

/** Builds a form model from a fetched product (or the empty model). */
export function productToForm(product) {
  if (!product) {
    return { ...EMPTY_PRODUCT_FORM }
  }

  return {
    sku: product.sku ?? "",
    name: product.name ?? "",
    type: product.type ?? "",
    category: product.category ?? PRODUCT_CATEGORY.GENERAL,
    transportEnvironment:
      product.transportEnvironment ?? TRANSPORT_ENVIRONMENT.DRY,
    tempMin: product.tempMin ?? "",
    tempMax: product.tempMax ?? "",
    packagingType: product.packagingType ?? "",
    dangerous: Boolean(product.dangerous),
    fragile: Boolean(product.fragile),
    defaultWeight: product.defaultWeight ?? "",
    defaultVolume: product.defaultVolume ?? "",
    handlingRestriction: product.handlingRestriction ?? "",
  }
}

/**
 * Manages the product form state, exposing a stable `setField` updater and
 * derived flags for the conditional temperature/packaging blocks.
 */
export function useProductForm(initial) {
  const [form, setForm] = useState(() => productToForm(initial))

  const setField = useCallback((field, value) => {
    setForm((current) => ({ ...current, [field]: value }))
  }, [])

  const reset = useCallback((next) => {
    setForm(productToForm(next))
  }, [])

  const showTemperature = useMemo(
    () => requiresTemperature(form.transportEnvironment),
    [form.transportEnvironment]
  )

  const showPackaging = useMemo(
    () => requiresPackaging(form.category),
    [form.category]
  )

  return { form, setForm, setField, reset, showTemperature, showPackaging }
}
