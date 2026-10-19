/* eslint-disable react/prop-types, react-refresh/only-export-components */
import { createContext, useCallback, useContext, useLayoutEffect, useMemo, useState } from "react"
import { useAuth } from "./AuthContext"
import { ROLES } from "@/constants/roles"

const BrandContext = createContext()

const STORAGE_KEY = "fg-brand-prefs"

// As cores de cada tema ficam em src/themes.css ([data-brand="id"]).
export const THEMES = [
  { id: "corporate", name: "Azul Corporativo", kind: "light", description: "Branco com azul. Limpo e profissional." },
  { id: "ocean", name: "Oceano", kind: "light", description: "Claro com tons de verde-água." },
  { id: "forest", name: "Floresta", kind: "light", description: "Verde suave sobre fundo off-white." },
  { id: "latte", name: "Lilás", kind: "light", description: "Fundo claro com destaque lilás." },
  { id: "solarized", name: "Areia", kind: "light", description: "Creme com destaque em azul." },
  { id: "githublight", name: "Cinza Neutro", kind: "light", description: "Branco e cinza com destaque azul. Bem legível." },
  { id: "nordlight", name: "Gelo", kind: "light", description: "Branco-gelo com azul acinzentado." },
  { id: "gruvlight", name: "Mel", kind: "light", description: "Creme quente com destaque laranja." },
  { id: "midnight", name: "Meia-noite", kind: "dark", description: "Azul-marinho profundo." },
  { id: "github", name: "Grafite", kind: "dark", description: "Cinza-azulado escuro com destaque azul." },
  { id: "nord", name: "Polar", kind: "dark", description: "Cinza-azulado com destaque azul-gelo." },
  { id: "dracula", name: "Ametista", kind: "dark", description: "Grafite com destaque roxo." },
  { id: "tokyo", name: "Crepúsculo", kind: "dark", description: "Azul-noite com destaque azul suave." },
  { id: "gruvbox", name: "Cobre", kind: "dark", description: "Marrom escuro com destaque laranja." },
  { id: "mocha", name: "Lavanda", kind: "dark", description: "Roxo-acinzentado escuro com destaque lilás." },
  { id: "industrial", name: "Industrial", kind: "dark", description: "Preto grafite com destaque verde." },
]

const THEME_IDS = new Set(THEMES.map((theme) => theme.id))

// Classes do shell (sidebar e cabeçalho) apoiadas nas variáveis do tema.
const brandStyle = {
  sidebarBg: "bg-sidebar",
  sidebarBorder: "border-sidebar-border",
  navText: "text-sidebar-foreground",
  navHover: "hover:bg-sidebar-accent hover:text-sidebar-strong",
  navActive:
    "border border-sidebar-active-border bg-sidebar-active text-sidebar-active-foreground font-semibold",
  accent: "text-brand",
  brandBg: "bg-primary",
  brandIcon: "text-primary-foreground",
  headerBg: "bg-header",
  textMain: "text-header-foreground",
  textMuted: "text-header-foreground/60",
  footerBg: "bg-sidebar-accent",
}

const DEFAULT_PREFS = {
  [ROLES.CONTRACTOR]: "corporate",
  [ROLES.CARRIER]: "industrial",
  [ROLES.DEVELOPER]: "midnight",
}

function readPrefs() {
  try {
    const stored = JSON.parse(localStorage.getItem(STORAGE_KEY))
    if (stored && typeof stored === "object") return { ...DEFAULT_PREFS, ...stored }
  } catch {
    // preferências corrompidas: volta ao padrão
  }
  return DEFAULT_PREFS
}

export function BrandProvider({ children }) {
  const { user } = useAuth()
  // Unauthenticated users have no session; fall back to a default role so brand
  // resolution stays valid on public pages (e.g. the auth screen).
  const role = user?.role ?? ROLES.CONTRACTOR

  const [brandPrefs, setBrandPrefs] = useState(readPrefs)

  const stored = brandPrefs[role]
  const currentKey = THEME_IDS.has(stored) ? stored : DEFAULT_PREFS[role] ?? "corporate"
  const currentTheme = THEMES.find((theme) => theme.id === currentKey)

  useLayoutEffect(() => {
    const root = document.documentElement
    root.dataset.brand = currentKey
    root.classList.toggle("dark", currentTheme.kind === "dark")
  }, [currentKey, currentTheme.kind])

  const setBrand = useCallback(
    (styleKey) => {
      if (!THEME_IDS.has(styleKey)) return
      const newPrefs = { ...brandPrefs, [role]: styleKey }
      setBrandPrefs(newPrefs)
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(newPrefs))
      } catch {
        // sem armazenamento: o tema vale só nesta sessão
      }
    },
    [brandPrefs, role]
  )

  const value = useMemo(
    () => ({
      brand: brandStyle,
      setBrand,
      currentKey,
      availableThemes: THEMES,
    }),
    [setBrand, currentKey]
  )

  return <BrandContext.Provider value={value}>{children}</BrandContext.Provider>
}

export const useBrand = () => useContext(BrandContext)
