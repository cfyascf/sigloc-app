import { RISK } from "@/constants/risk"

/**
 * Legacy mock route segments used by the not-yet-integrated route/auction
 * workspace screens (CreateRouteWorkspace). The Route Segment management and
 * offer-freight flows now consume the real API; this mock is retained only so
 * those out-of-scope screens keep rendering until they are integrated too.
 */
export const availableRouteSegments = [
  { id: "TRC-1042", productName: "Peito de Frango Congelado", bodyType: "Frigorífico", loadType: "Paletizado", origin: "Curitiba, PR", destination: "São Paulo, SP", risk: RISK.CRITIC, pickupWindow: "Hoje até às 18:00h", weightKg: 12000, volumeM3: 45, distanceKm: 408, targetPrice: 4200 },
  { id: "TRC-1043", productName: "Laticínios Pasteurizados", bodyType: "Refrigerado", loadType: "Paletizado", origin: "São Paulo, SP", destination: "Campinas, SP", risk: RISK.WARNING, pickupWindow: "Amanhã até às 12:00h", weightKg: 9000, volumeM3: 28, distanceKm: 96, targetPrice: 1100 },
  { id: "TRC-1044", productName: "Eletrônicos de Alto Valor", bodyType: "Baú Sider", loadType: "Caixas Master", origin: "Campinas, SP", destination: "Ribeirão Preto, SP", risk: RISK.WARNING, pickupWindow: "07/06 às 08:00h", weightKg: 4800, volumeM3: 22, distanceKm: 223, targetPrice: 2800 },
  { id: "TRC-1045", productName: "Vacinas Influenza", bodyType: "Frigorífico", loadType: "Isotérmico", origin: "Ribeirão Preto, SP", destination: "Uberlândia, MG", risk: RISK.CRITIC, pickupWindow: "Hoje urgente até às 16:30h", weightKg: 3200, volumeM3: 18, distanceKm: 166, targetPrice: 3100 },
]
