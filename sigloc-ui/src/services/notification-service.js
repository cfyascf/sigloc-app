import { apiClient } from "@/lib/api-client"

const BASE = "/api/notificacoes"

export function toNotification(dto) {
  return {
    id: dto.id,
    type: dto.tipo,
    title: dto.titulo,
    message: dto.mensagem,
    // Only in-app paths are followed; anything else would navigate away from the app.
    link: typeof dto.link === "string" && dto.link.startsWith("/") && !dto.link.startsWith("//") ? dto.link : null,
    read: dto.lida === true,
    createdAt: dto.criadaEm ?? null,
  }
}

/** Newest notifications of the signed-in company and its unread count. */
export async function listNotifications({ limit = 20, ...options } = {}) {
  const payload = await apiClient.get(`${BASE}?limite=${encodeURIComponent(limit)}`, options)
  return {
    items: (payload?.itens ?? []).map(toNotification),
    unread: Number.isInteger(payload?.naoLidas) ? payload.naoLidas : 0,
  }
}

export async function getUnreadNotificationCount(options) {
  const payload = await apiClient.get(`${BASE}/nao-lidas`, options)
  return Number.isInteger(payload?.naoLidas) ? payload.naoLidas : 0
}

export function markNotificationRead(id) {
  return apiClient.post(`${BASE}/${encodeURIComponent(id)}/lida`)
}

export function markAllNotificationsRead() {
  return apiClient.post(`${BASE}/lidas`)
}
