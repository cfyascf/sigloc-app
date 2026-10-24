/* eslint-disable react/prop-types */
import { useCallback, useEffect, useRef, useState } from "react"
import { Bell, CheckCheck, Loader2 } from "lucide-react"
import { useNavigate } from "react-router-dom"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { cn } from "@/lib/utils"
import {
  getUnreadNotificationCount,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
} from "@/services/notification-service"

const POLL_MS = 30_000
const relative = new Intl.RelativeTimeFormat("pt-BR", { numeric: "auto" })

/** "agora", "há 5 minutos", "ontem"... */
export function formatNotificationTime(value, now = Date.now()) {
  const time = value ? new Date(value).getTime() : NaN
  if (Number.isNaN(time)) return ""
  const seconds = Math.round((time - now) / 1000)
  if (seconds > -60) return "agora"
  const minutes = Math.round(seconds / 60)
  if (minutes > -60) return relative.format(minutes, "minute")
  const hours = Math.round(minutes / 60)
  if (hours > -24) return relative.format(hours, "hour")
  return relative.format(Math.round(hours / 24), "day")
}

/**
 * Header bell: polls the unread count every 30 s while the tab is visible, loads the list when
 * opened, and marks a notification read when it is clicked (then follows its link).
 */
export function NotificationBell({ className }) {
  const navigate = useNavigate()
  const [unread, setUnread] = useState(0)
  const [items, setItems] = useState([])
  const [state, setState] = useState("idle") // idle | loading | ready | error
  const listController = useRef(null)

  const refreshCount = useCallback(async (signal) => {
    try {
      setUnread(await getUnreadNotificationCount({ signal }))
    } catch {
      // The badge is best-effort; the next poll retries.
    }
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    refreshCount(controller.signal)
    const timer = setInterval(() => {
      if (document.visibilityState === "visible") refreshCount(controller.signal)
    }, POLL_MS)
    const onVisible = () => { if (document.visibilityState === "visible") refreshCount(controller.signal) }
    document.addEventListener("visibilitychange", onVisible)
    return () => {
      controller.abort()
      clearInterval(timer)
      document.removeEventListener("visibilitychange", onVisible)
      listController.current?.abort()
    }
  }, [refreshCount])

  const loadList = async () => {
    listController.current?.abort()
    const controller = new AbortController()
    listController.current = controller
    setState("loading")
    try {
      const result = await listNotifications({ signal: controller.signal })
      setItems(result.items)
      setUnread(result.unread)
      setState("ready")
    } catch (error) {
      if (error?.name !== "AbortError") setState("error")
    }
  }

  const handleOpenChange = (open) => {
    if (open) loadList()
    else listController.current?.abort()
  }

  const handleSelect = (item) => {
    if (!item.read) {
      setItems((current) => current.map((n) => (n.id === item.id ? { ...n, read: true } : n)))
      setUnread((count) => Math.max(0, count - 1))
      markNotificationRead(item.id).catch(() => refreshCount())
    }
    if (item.link) navigate(item.link)
  }

  const handleMarkAll = async (event) => {
    // Keep the menu open so the user sees every item turn read.
    event.preventDefault()
    setItems((current) => current.map((n) => ({ ...n, read: true })))
    setUnread(0)
    try {
      await markAllNotificationsRead()
    } catch {
      refreshCount()
    }
  }

  const badge = unread > 99 ? "99+" : String(unread)

  return (
    <DropdownMenu onOpenChange={handleOpenChange}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          aria-label={unread ? `Notificações (${unread} não lidas)` : "Notificações"}
          className={cn("relative hover:bg-black/5", className)}
        >
          <Bell size={20} />
          {unread > 0 && (
            <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-600 px-1 text-[10px] font-bold leading-none text-white">
              {badge}
            </span>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-96 p-0">
        <div className="flex items-center justify-between px-3 py-2">
          <DropdownMenuLabel className="p-0 text-xs font-bold text-slate-700">Notificações</DropdownMenuLabel>
          {unread > 0 && (
            <button type="button" onClick={handleMarkAll} className="flex items-center gap-1 text-[11px] font-semibold text-blue-600 hover:underline">
              <CheckCheck size={12} /> Marcar todas como lidas
            </button>
          )}
        </div>
        <DropdownMenuSeparator className="m-0" />
        <div className="max-h-[420px] overflow-y-auto">
          {state === "loading" && !items.length && (
            <p className="flex items-center gap-2 p-4 text-xs text-slate-500"><Loader2 size={14} className="animate-spin" /> Carregando...</p>
          )}
          {state === "error" && <p className="p-4 text-xs text-rose-600">Não foi possível carregar as notificações.</p>}
          {state === "ready" && !items.length && <p className="p-4 text-xs text-slate-500">Nenhuma notificação por enquanto.</p>}
          {items.map((item) => (
            <DropdownMenuItem
              key={item.id}
              onSelect={() => handleSelect(item)}
              className={cn("flex cursor-pointer items-start gap-2 rounded-none px-3 py-2.5", !item.read && "bg-blue-50/60")}
            >
              <span className={cn("mt-1.5 h-2 w-2 shrink-0 rounded-full", item.read ? "bg-transparent" : "bg-blue-600")} />
              <span className="min-w-0 flex-1">
                <span className={cn("block text-xs text-slate-800", !item.read && "font-bold")}>{item.title}</span>
                <span className="block text-[11px] leading-snug text-slate-600">{item.message}</span>
                <span className="mt-0.5 block text-[10px] text-slate-400">{formatNotificationTime(item.createdAt)}</span>
              </span>
            </DropdownMenuItem>
          ))}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
