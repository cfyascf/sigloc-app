/**
 * Which vehicles carry which cargo: the UI copy of the API's `CargoCompatibility` rule table.
 * The API enforces these rules (product and vehicle validation, bid check); the forms use them to
 * only offer valid combinations and to explain each choice.
 *
 * Body types are the labels from `constants/vehicles`; refrigeration levels are the integer
 * `RefrigerationLevel` values (0 Nenhuma, 1 Resfriado, 2 Congelado).
 */

import { PRODUCT_CATEGORY, TRANSPORT_ENVIRONMENT, categoryLabel } from "./products.js"
import { BODY_TYPE, BODY_TYPE_OPTIONS } from "./vehicles.js"

export const REFRIGERATION = { NONE: 0, CHILLED: 1, FROZEN: 2 }

const BODIES_BY_CATEGORY = {
  [PRODUCT_CATEGORY.GENERAL]: [
    BODY_TYPE.BAU, BODY_TYPE.REFRIGERATED, BODY_TYPE.SIDER,
    BODY_TYPE.GRADE_BAIXA, BODY_TYPE.GRANELEIRO, BODY_TYPE.PRANCHA,
  ],
  [PRODUCT_CATEGORY.SOLID_BULK]: [BODY_TYPE.GRANELEIRO, BODY_TYPE.CACAMBA],
  [PRODUCT_CATEGORY.LIQUID_BULK]: [BODY_TYPE.TANQUE],
  [PRODUCT_CATEGORY.PRESSURIZED_BULK]: [BODY_TYPE.SILO],
  [PRODUCT_CATEGORY.CONTAINERIZED]: [BODY_TYPE.PORTA_CONTEINER],
  [PRODUCT_CATEGORY.NEO_BULK]: [BODY_TYPE.GRADE_BAIXA, BODY_TYPE.PRANCHA, BODY_TYPE.CEGONHEIRO],
  [PRODUCT_CATEGORY.LIVE_CARGO]: [BODY_TYPE.GAIOLA],
}

// Refrigerated box for chilled or frozen loads; insulated tanks chill (milk); reefer containers do
// both. Every other body runs at ambient temperature.
const REFRIGERATION_BY_BODY = {
  [BODY_TYPE.REFRIGERATED]: [REFRIGERATION.CHILLED, REFRIGERATION.FROZEN],
  [BODY_TYPE.TANQUE]: [REFRIGERATION.NONE, REFRIGERATION.CHILLED],
  [BODY_TYPE.PORTA_CONTEINER]: [REFRIGERATION.NONE, REFRIGERATION.CHILLED, REFRIGERATION.FROZEN],
}

/** Open bodies leave the load exposed, so they can't take fragile cargo. */
const OPEN_BODIES = new Set([
  BODY_TYPE.GRADE_BAIXA, BODY_TYPE.GRANELEIRO, BODY_TYPE.CACAMBA, BODY_TYPE.PRANCHA, BODY_TYPE.CEGONHEIRO,
])

/** Open platforms carrying unit loads must tie them down (CONTRAN Res. 945/2022). */
const SECURING_REQUIRED_BODIES = new Set([BODY_TYPE.GRADE_BAIXA, BODY_TYPE.PRANCHA, BODY_TYPE.CEGONHEIRO])

const FRAGILE_CATEGORIES = new Set([PRODUCT_CATEGORY.GENERAL, PRODUCT_CATEGORY.CONTAINERIZED])

/**
 * Bulk loads fill one tank, hold or silo and live animals share one cage: a segment of these
 * categories carries one product and is offered on its own, never combined with other segments.
 */
const SINGLE_PRODUCT_CATEGORIES = new Set([
  PRODUCT_CATEGORY.SOLID_BULK, PRODUCT_CATEGORY.LIQUID_BULK, PRODUCT_CATEGORY.PRESSURIZED_BULK, PRODUCT_CATEGORY.LIVE_CARGO,
])

export const isSingleProductCategory = (category) => SINGLE_PRODUCT_CATEGORIES.has(category)

const REQUIRED_REFRIGERATION = {
  [TRANSPORT_ENVIRONMENT.DRY]: REFRIGERATION.NONE,
  [TRANSPORT_ENVIRONMENT.CHILLED]: REFRIGERATION.CHILLED,
  [TRANSPORT_ENVIRONMENT.FROZEN]: REFRIGERATION.FROZEN,
}

/** Refrigeration levels a vehicle with this body may declare. */
export function refrigerationLevelsFor(bodyType) {
  return REFRIGERATION_BY_BODY[bodyType] ?? [REFRIGERATION.NONE]
}

const maxRefrigeration = (bodyType) => Math.max(...refrigerationLevelsFor(bodyType))

/** Body types that carry the category at this environment, without open bodies when fragile. */
export function acceptedBodies(category, environment = TRANSPORT_ENVIRONMENT.DRY, fragile = false) {
  const required = REQUIRED_REFRIGERATION[environment] ?? REFRIGERATION.NONE
  return (BODIES_BY_CATEGORY[category] ?? [])
    .filter((body) => maxRefrigeration(body) >= required)
    .filter((body) => !fragile || !OPEN_BODIES.has(body))
}

/** Environments (wire values) a product of this category can travel in. */
export function environmentsFor(category) {
  const coldest = Math.max(REFRIGERATION.NONE, ...(BODIES_BY_CATEGORY[category] ?? []).map(maxRefrigeration))
  return Object.entries(REQUIRED_REFRIGERATION)
    .filter(([, level]) => level <= coldest)
    .map(([environment]) => environment)
}

/** Category labels a body type carries, in category order. */
export function categoriesFor(bodyType) {
  return Object.entries(BODIES_BY_CATEGORY)
    .filter(([, bodies]) => bodies.includes(bodyType))
    .map(([category]) => categoryLabel(category))
}

export const allowsFragile = (category) => FRAGILE_CATEGORIES.has(category)

/** Live animals are never dangerous goods. */
export const allowsDangerous = (category) => category !== PRODUCT_CATEGORY.LIVE_CARGO

export const requiresCargoSecuring = (bodyType) => SECURING_REQUIRED_BODIES.has(bodyType)

const BULK_CATEGORIES = [PRODUCT_CATEGORY.SOLID_BULK, PRODUCT_CATEGORY.LIQUID_BULK, PRODUCT_CATEGORY.PRESSURIZED_BULK]

/** Bodies that carry bulk: dangerous bulk needs CIV/CIPP in date, so only they ask for it. */
export const carriesBulk = (bodyType) => BULK_CATEGORIES.some((category) => BODIES_BY_CATEGORY[category].includes(bodyType))

/** "Baú, Sider ou Grade Baixa"; "Nenhuma carroceria compatível" when empty. */
export function describeList(items) {
  if (items.length === 0) return "Nenhuma carroceria compatível"
  if (items.length === 1) return items[0]
  return `${items.slice(0, -1).join(", ")} ou ${items.at(-1)}`
}

/**
 * Product form after a change: drops the environment and flags the new category can't have, so
 * switching category never leaves an invalid combination behind.
 */
export function normalizeProductForm(form) {
  const next = { ...form }
  if (next.category && !environmentsFor(next.category).includes(next.transportEnvironment)) {
    next.transportEnvironment = TRANSPORT_ENVIRONMENT.DRY
  }
  if (!allowsFragile(next.category)) next.fragile = false
  if (!allowsDangerous(next.category)) next.dangerous = false
  return next
}

/**
 * Vehicle form after a change: keeps the refrigeration within what the body allows (the closest
 * allowed level) and turns cargo securing on where the law requires it.
 */
export function normalizeVehicleEquipment(form) {
  if (!BODY_TYPE_OPTIONS.includes(form.bodyType)) return form
  const next = { ...form }
  const levels = refrigerationLevelsFor(next.bodyType)
  const current = Number(next.refrigerationLevel ?? REFRIGERATION.NONE)
  if (!levels.includes(current)) {
    next.refrigerationLevel = current > Math.max(...levels) ? Math.max(...levels) : Math.min(...levels)
  }
  if (requiresCargoSecuring(next.bodyType)) next.hasCargoSecuring = true
  return next
}
