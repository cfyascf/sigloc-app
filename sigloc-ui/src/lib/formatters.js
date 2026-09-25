/**
 * Small, pure formatting/normalization helpers shared across forms.
 */

/** Strips every non-digit character. */
export function onlyDigits(value) {
  return typeof value === "string" ? value.replace(/\D/g, "") : ""
}

/**
 * Formats a CNPJ string progressively as the user types:
 *   00.000.000/0000-00
 */
export function formatCnpj(value) {
  const digits = onlyDigits(value).slice(0, 14)

  return digits
    .replace(/^(\d{2})(\d)/, "$1.$2")
    .replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
    .replace(/\.(\d{3})(\d)/, ".$1/$2")
    .replace(/(\d{4})(\d)/, "$1-$2")
}
