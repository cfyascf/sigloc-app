/* eslint-disable react/prop-types */
import { useEffect, useState } from "react"
import {
  Bell,
  Check,
  ChevronLeft,
  ChevronRight,
  Shield,
  User,
  Settings,
  Palette,
  ChevronDown,
} from "lucide-react"
import { NavLink } from "react-router-dom"
import { useAuth } from "@/contexts/AuthContext"
import { useBrand } from "@/contexts/BrandContext"
import { Button } from "@/components/ui/button"
import { ROLE_OPTIONS, ROLES } from "@/constants/roles"
import { getNavItems } from "@/routes/route-config"
import { cn } from "@/lib/utils"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

const SIDEBAR_STORAGE_KEY = "sigloc_sidebar_collapsed"

export default function AppShell({ title, children, contentClassName, innerClassName }) {
  const { user, setRole } = useAuth()
  const { brand, setBrand, currentKey, availableThemes } = useBrand()
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(() => {
    if (globalThis.localStorage === undefined) {
      return false
    }

    return globalThis.localStorage.getItem(SIDEBAR_STORAGE_KEY) === "true"
  })
  const showSidebarText = isSidebarCollapsed === false

  useEffect(() => {
    if (globalThis.localStorage === undefined) {
      return
    }

    globalThis.localStorage.setItem(SIDEBAR_STORAGE_KEY, String(isSidebarCollapsed))
  }, [isSidebarCollapsed])

  // Derive nav items and labels from role — no hardcoded menus
  const activeMenus = getNavItems(user.role)

  let panelLabel = "Painel Transportador"
  let roleLabel = "Transportador"

  if (user.role === ROLES.DEVELOPER) {
    panelLabel = "Painel Desenvolvedor"
    roleLabel = "Desenvolvedor"
  } else if (user.role === ROLES.CONTRACTOR) {
    panelLabel = "Painel Contratante"
    roleLabel = "Contratante"
  }

  return (
    <div className="flex h-screen overflow-hidden bg-slate-100 font-sans">
      {/* SIDEBAR COM TEMA DINÂMICO */}
      <aside
        className={cn(
          "z-20 flex h-full shrink-0 flex-col border-r transition-all duration-300",
          isSidebarCollapsed ? "w-20" : "w-64",
          brand.sidebarBg,
          brand.sidebarBorder
        )}
      >
        <div
          className={cn(
            "flex h-16 shrink-0 items-center border-b transition-colors duration-500",
            isSidebarCollapsed ? "justify-center px-3" : "px-6",
            brand.sidebarBorder
          )}
        >
          <div
            className={cn(
              "mr-3 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg shadow-sm transition-colors duration-500",
              brand.brandBg,
              brand.brandIcon
            )}
          >
            <Shield size={18} strokeWidth={2.5} />
          </div>
          {showSidebarText ? (
            <span
              className={cn("text-xl font-black tracking-tight", brand.textMain)}
            >
              Sig
              <span
                className={cn("transition-colors duration-500", brand.accent)}
              >
                loc
              </span>
            </span>
          ) : null}
        </div>

        <nav className={cn("flex-1 space-y-1.5 overflow-y-auto text-xs", isSidebarCollapsed ? "p-3" : "p-4")}>
          {showSidebarText ? (
            <p
              className={cn(
                "mb-3 px-2 text-[10px] font-bold tracking-widest uppercase opacity-60",
                brand.textMuted
              )}
            >
              {panelLabel}
            </p>
          ) : null}
          {activeMenus.map(({ label, path, icon: Icon }) => (
            <NavLink
              key={path}
              to={path}
              title={label}
              className={({ isActive }) =>
                cn(
                  "flex rounded-lg py-2.5 transition-all duration-300",
                  isSidebarCollapsed ? "justify-center px-2" : "items-center px-3",
                  isActive ? brand.navActive : cn(brand.navText, brand.navHover)
                )
              }
            >
              <Icon size={18} className={cn("shrink-0", isSidebarCollapsed ? "mr-0" : "mr-3")} />
              {showSidebarText ? <span className="text-sm font-medium">{label}</span> : null}
            </NavLink>
          ))}
        </nav>

        {/* Rodapé da Sidebar - Botão de Mudar Role */}
        <div
          className={cn(
            "flex shrink-0 flex-col gap-3 border-t p-4 transition-colors duration-500",
            brand.sidebarBorder,
            brand.footerBg
          )}
        >
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                title={`Perfil atual: ${roleLabel}`}
                className={cn(
                  "text-[10px] font-bold shadow-none",
                  isSidebarCollapsed ? "w-full justify-center px-0" : "w-full",
                  brand.sidebarBorder,
                  brand.navHover,
                  brand.textMuted,
                  brand.footerBg
                )}
              >
                {isSidebarCollapsed ? <User size={14} /> : <span>Perfil: {roleLabel}</span>}
                {showSidebarText ? <ChevronDown size={14} className="ml-2" /> : null}
              </Button>
            </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-52">
                <DropdownMenuLabel className="text-[10px] font-bold tracking-widest text-slate-400 uppercase">
                  Alternar Perfil
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                {ROLE_OPTIONS.map((roleOption) => (
                  <DropdownMenuItem
                    key={roleOption.value}
                    onClick={() => setRole(roleOption.value)}
                    className="cursor-pointer text-xs font-semibold"
                  >
                    <Check
                      size={14}
                      className={cn(
                        "mr-2",
                        user.role === roleOption.value ? "opacity-100" : "opacity-0"
                      )}
                    />
                    {roleOption.label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>

          <div className={cn("mt-2 flex items-center", isSidebarCollapsed ? "justify-center" : "") }>
            <div
              className={cn(
                "flex h-9 w-9 shrink-0 items-center justify-center rounded-full shadow-inner",
                brand.footerBg,
                brand.textMain
              )}
            >
              <User size={18} />
            </div>
            {showSidebarText ? (
              <div className={cn("ml-3 overflow-hidden", brand.textMain)}>
                <p className="truncate text-sm font-bold">{user?.name ?? "Usuário"}</p>
                <p
                  className={cn(
                    "text-[10px] font-black uppercase opacity-70",
                    brand.accent
                  )}
                >
                  {roleLabel}
                </p>
              </div>
            ) : null}
          </div>
        </div>
      </aside>

      {/* ÁREA PRINCIPAL */}
      <main className="relative flex flex-1 flex-col overflow-hidden">
        {/* HEADER COM ENGRENAGEM (Menu iterado dinamicamente) */}
        <header
          className={cn(
            "z-10 flex h-16 shrink-0 items-center justify-between border-b px-8 transition-colors duration-500",
            brand.headerBg,
            brand.sidebarBorder,
            brand.textMain
          )}
        >
          <h1 className="text-xl font-bold tracking-tight">{title}</h1>

          <div className="flex items-center space-x-4">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setIsSidebarCollapsed((current) => !current)}
              title={isSidebarCollapsed ? "Expandir sidebar" : "Recolher sidebar"}
              className={cn("hover:bg-black/5", brand.textMuted)}
            >
              {isSidebarCollapsed ? <ChevronRight size={20} /> : <ChevronLeft size={20} />}
            </Button>

            <Button
              variant="ghost"
              size="icon"
              className={cn("hover:bg-black/5", brand.textMuted)}
            >
              <Bell size={20} />
            </Button>

            {/* Menu de Configurações (Engrenagem) */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className={cn("hover:bg-black/5", brand.textMuted)}
                >
                  <Settings size={20} />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuLabel className="text-[10px] font-bold tracking-widest text-slate-400 uppercase">
                  Personalizar Tema
                </DropdownMenuLabel>
                <DropdownMenuSeparator />

                {/* Iterando sobre os temas disponíveis no Contexto */}
                {availableThemes.map((t) => (
                  <DropdownMenuItem
                    key={t.id}
                    onClick={() => setBrand(t.id)}
                    className={cn(
                      "cursor-pointer font-medium",
                      currentKey === t.id &&
                        "bg-blue-50 text-blue-600 focus:bg-blue-100 focus:text-blue-700"
                    )}
                  >
                    <Palette
                      size={16}
                      className={cn(
                        "mr-2",
                        currentKey === t.id ? "text-blue-600" : "text-slate-400"
                      )}
                    />
                    {t.name}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>

          </div>
        </header>

        {/* Conteúdo */}
        <div className={cn("flex-1 overflow-auto bg-slate-50 p-8", contentClassName)}>
          <div className={cn("mx-auto max-w-7xl animate-in duration-500 fade-in", innerClassName)}>
            {children}
          </div>
        </div>
      </main>
    </div>
  )
}
