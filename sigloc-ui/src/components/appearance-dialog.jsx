/* eslint-disable react/prop-types */
import { useState } from "react"
import { useBrand } from "@/contexts/BrandContext"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { cn } from "@/lib/utils"

const GROUPS = [
  { kind: "light", label: "Claros" },
  { kind: "dark", label: "Escuros" },
]

const NAV_WIDTHS = ["w-8", "w-6", "w-7", "w-5"]

// Miniatura de cores usada nos cartões de tema.
function ThemeSwatch({ themeId }) {
  return (
    <div
      data-brand={themeId}
      className="grid h-14 grid-cols-[22%_1fr] overflow-hidden rounded-md border border-border"
    >
      <div className="bg-sidebar" />
      <div className="flex flex-col justify-center gap-1 bg-background px-1.5">
        <span className="h-1.5 w-3/5 rounded-full bg-primary" />
        <span className="h-2 rounded-sm border border-border bg-card" />
      </div>
    </div>
  )
}

// Versão reduzida do sistema, renderizada com as variáveis do tema escolhido.
function ThemePreview({ themeId }) {
  return (
    <div
      data-brand={themeId}
      className="flex h-80 shrink-0 overflow-hidden rounded-lg border border-border text-foreground"
    >
      <aside className="flex w-[5.5rem] shrink-0 flex-col border-r border-sidebar-border bg-sidebar">
        <div className="flex h-9 shrink-0 items-center gap-1.5 border-b border-sidebar-border px-2">
          <span className="size-4 shrink-0 rounded-[4px] bg-primary shadow-sm" />
          <span className="text-[10px] leading-none font-black tracking-tight text-sidebar-strong">
            Sig<span className="text-brand">loc</span>
          </span>
        </div>
        <div className="flex flex-1 flex-col gap-1 p-2">
          {NAV_WIDTHS.map((width, index) => (
            <span
              key={index}
              className={cn(
                "flex items-center gap-1.5 rounded-md border px-1.5 py-1.5",
                index === 0
                  ? "border-sidebar-active-border bg-sidebar-active text-sidebar-active-foreground"
                  : "border-transparent text-sidebar-foreground"
              )}
            >
              <span className="size-2.5 shrink-0 rounded-[3px] bg-current opacity-80" />
              <span className={cn("h-1 rounded-full bg-current opacity-60", width)} />
            </span>
          ))}
        </div>
        <div className="flex shrink-0 items-center gap-2 border-t border-sidebar-border bg-sidebar-accent p-2">
          <span className="size-4 shrink-0 rounded-full bg-sidebar-border" />
          <div className="flex min-w-0 flex-1 flex-col gap-1">
            <span className="h-1 w-8 rounded-full bg-sidebar-strong/80" />
            <span className="h-1 w-5 rounded-full bg-brand" />
          </div>
          <span className="size-2 shrink-0 rounded-[2px] bg-sidebar-foreground/50" />
        </div>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <div className="flex h-9 shrink-0 items-center border-b border-sidebar-border bg-header px-3">
          <span className="h-1.5 w-14 rounded-full bg-header-foreground/80" />
        </div>
        <div className="flex flex-1 flex-col gap-2 bg-background p-3">
          <div className="grid grid-cols-3 gap-1.5">
            {[0, 1, 2].map((item) => (
              <div key={item} className="flex flex-col gap-1.5 rounded-md border border-border bg-card p-2">
                <span className="h-1 w-6 rounded-full bg-muted-foreground/50" />
                <span className="h-2 w-8 rounded-full bg-foreground/80" />
              </div>
            ))}
          </div>
          <div className="flex flex-col gap-1.5 rounded-md border border-border bg-card p-2">
            <span className="h-1.5 w-3/5 rounded-full bg-muted-foreground/50" />
            <span className="h-1.5 rounded-full bg-border" />
            <span className="h-1.5 w-4/5 rounded-full bg-border" />
          </div>
          <div className="flex gap-1.5">
            <span className="h-4 w-12 rounded-md bg-primary" />
            <span className="h-4 w-12 rounded-md border border-border" />
          </div>
        </div>
      </div>
    </div>
  )
}

export default function AppearanceDialog({ open, onOpenChange }) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[95vh] overflow-y-auto sm:max-w-[72rem]">
        <AppearanceBody onClose={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}

// Monta só enquanto o menu está aberto, então a seleção sempre começa no tema em uso.
function AppearanceBody({ onClose }) {
  const { setBrand, currentKey, availableThemes } = useBrand()
  const [pickedKey, setPickedKey] = useState(currentKey)

  const picked = availableThemes.find((theme) => theme.id === pickedKey)

  const handleApply = () => {
    setBrand(pickedKey)
    onClose()
  }

  return (
    <>
        <DialogHeader>
          <DialogTitle className="text-lg font-bold">Aparência</DialogTitle>
          <DialogDescription>Escolha como o Sigloc aparece para você.</DialogDescription>
        </DialogHeader>

        <div className="grid items-stretch gap-4 md:grid-cols-[minmax(0,1fr)_26rem]">
          <div className="flex min-w-0 flex-col gap-3">
            {GROUPS.map(({ kind, label }) => (
              <div key={kind} className="flex flex-col gap-2">
                <p className="text-[10px] font-extrabold tracking-widest text-muted-foreground uppercase">
                  {label}
                </p>
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                  {availableThemes
                    .filter((theme) => theme.kind === kind)
                    .map((theme) => {
                      const selected = theme.id === pickedKey
                      return (
                        <button
                          key={theme.id}
                          type="button"
                          aria-pressed={selected}
                          onClick={() => setPickedKey(theme.id)}
                          className={cn(
                            "flex min-w-0 flex-col gap-2 rounded-lg border-[1.5px] bg-background p-3 text-left text-sm font-semibold outline-none transition-colors focus-visible:ring-2 focus-visible:ring-ring",
                            selected ? "border-primary ring-1 ring-primary" : "border-border hover:border-primary/50"
                          )}
                        >
                          <ThemeSwatch themeId={theme.id} />
                          <span className="flex items-center justify-between gap-1">
                            <span className="truncate">{theme.name}</span>
                            {theme.id === currentKey ? (
                              <span className="shrink-0 text-[10px] font-medium text-muted-foreground">em uso</span>
                            ) : null}
                          </span>
                        </button>
                      )
                    })}
                </div>
              </div>
            ))}
          </div>

          <div className="flex min-w-0 flex-col gap-2.5 rounded-lg bg-muted p-3">
            <p className="text-xs font-bold">Prévia: {picked?.name}</p>
            <ThemePreview themeId={pickedKey} />
            <p className="text-xs text-muted-foreground">{picked?.description}</p>
            <div className="mt-auto flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={onClose}>
                Cancelar
              </Button>
              <Button size="sm" onClick={handleApply} disabled={pickedKey === currentKey}>
                Aplicar tema
              </Button>
            </div>
          </div>
        </div>
    </>
  )
}
