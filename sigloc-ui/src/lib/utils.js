import { clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs) {
  return twMerge(clsx(inputs))
}

export function shortId(id, length = 6) {
  if (!id) return ""
  return String(id).replace(/-/g, "").slice(0, length).toUpperCase()
}