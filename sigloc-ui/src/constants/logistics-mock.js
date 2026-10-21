import { RISK } from "@/constants/risk"

export const cargoItemsMock = [
  {
    id: "CRG-1042",
    productId: "PRD-1001",
    productName: "Peito de Frango",
    category: "Perecível refrigerado",
    quantityLabel: "100 kg",
    routeLabel: "Curitiba, PR → São Paulo, SP",
    origin: "Curitiba, PR",
    destination: "São Paulo, SP",
    pickupWindow: "18/05/2026 08:00 - 10:00",
    deliveryWindow: "18/05/2026 18:00 - 21:00",
    freightValue: 4800,
    weight: "100 kg",
    volume: "4 m³",
    requirements: ["Refrigerado"],
    status: "Planejada",
    risk: RISK.WARNING,
    linkedSegmentId: "TRC-201",
    linkedSegmentName: "Curitiba → São Paulo → Salvador",
    contactName: "Ana Souza",
    contactPhone: "+55 (47) 99999-1234",
    eta: "10h 20m",
    operationalRisk: "Janela curta de coleta e necessidade de refrigeração contínua.",
  },
  {
    id: "CRG-1043",
    productId: "PRD-1002",
    productName: "Bicicleta Urbana",
    category: "Volume fracionado",
    quantityLabel: "2 unidades",
    routeLabel: "São Paulo, SP → Salvador, BA",
    origin: "São Paulo, SP",
    destination: "Salvador, BA",
    pickupWindow: "19/05/2026 09:00 - 12:00",
    deliveryWindow: "21/05/2026 08:00 - 12:00",
    freightValue: 5350,
    weight: "60 kg",
    volume: "6 m³",
    requirements: ["Frágil"],
    status: "Em Leilão",
    risk: RISK.NORMAL,
    linkedSegmentId: "TRC-201",
    linkedSegmentName: "Curitiba → São Paulo → Salvador",
    contactName: "Marcos Lima",
    contactPhone: "+55 (11) 98888-1200",
    eta: "32h 00m",
    operationalRisk: "Transferência entre etapas com consolidação em hub urbano.",
  },
  {
    id: "CRG-1044",
    productId: "PRD-1003",
    productName: "Motor Elétrico",
    category: "Carga industrial",
    quantityLabel: "6 unidades",
    routeLabel: "Joinville, SC → Campinas, SP",
    origin: "Joinville, SC",
    destination: "Campinas, SP",
    pickupWindow: "20/05/2026 07:30 - 09:30",
    deliveryWindow: "20/05/2026 17:00 - 20:00",
    freightValue: 7250,
    weight: "1.5 ton",
    volume: "9 m³",
    requirements: ["Carga seca"],
    status: "Planejada",
    risk: RISK.CRITIC,
    linkedSegmentId: "TRC-202",
    linkedSegmentName: "Joinville → Campinas",
    contactName: "Carla Menezes",
    contactPhone: "+55 (47) 97777-3200",
    eta: "9h 40m",
    operationalRisk: "Item de alto valor e necessidade de doca prioritária na entrega.",
  },
  {
    id: "CRG-1045",
    productId: "PRD-1004",
    productName: "Vacinas Influenza",
    category: "Saúde",
    quantityLabel: "24 caixas",
    routeLabel: "Araucária, PR → Rio de Janeiro, RJ",
    origin: "Araucária, PR",
    destination: "Rio de Janeiro, RJ",
    pickupWindow: "20/05/2026 14:00 - 16:00",
    deliveryWindow: "21/05/2026 10:00 - 13:00",
    freightValue: 6100,
    weight: "320 kg",
    volume: "5 m³",
    requirements: ["Refrigerado", "Frágil"],
    status: "Disponível para composição",
    risk: RISK.WARNING,
    linkedSegmentId: null,
    linkedSegmentName: "Aguardando composição",
    contactName: "Renato Alves",
    contactPhone: "+55 (41) 96666-9999",
    eta: "16h 15m",
    operationalRisk: "Controle térmico contínuo e monitoramento da cadeia fria.",
  },
]

export const segmentPlansMock = [
  {
    id: "TRC-201",
    name: "Curitiba → São Paulo → Salvador",
    status: "Em leilão",
    auctionStatus: "Recebendo lances",
    targetFare: 9800,
    totalWeight: "160 kg",
    totalVolume: "10 m³",
    itemCount: 2,
    legCount: 2,
    risk: RISK.WARNING,
    stops: ["Curitiba, PR", "São Paulo, SP", "Salvador, BA"],
    items: ["CRG-1042", "CRG-1043"],
    plannerNote: "Trecho com consolidação em hub de São Paulo e janela crítica na primeira perna.",
    bidDeadline: "18/05/2026 18:00",
    bestBid: 9150,
    totalBids: 8,
    winningCarrier: "Expresso Sul Ltda",
    coverage: "Cobertura total",
  },
  {
    id: "TRC-202",
    name: "Joinville → Campinas",
    status: "Planejado",
    auctionStatus: "Aguardando publicação",
    targetFare: 7200,
    totalWeight: "1.5 ton",
    totalVolume: "9 m³",
    itemCount: 1,
    legCount: 1,
    risk: RISK.CRITIC,
    stops: ["Joinville, SC", "Campinas, SP"],
    items: ["CRG-1044"],
    plannerNote: "Prioridade alta por valor agregado e necessidade de descarga sincronizada.",
    bidDeadline: "19/05/2026 12:00",
    bestBid: null,
    totalBids: 0,
    winningCarrier: null,
    coverage: "Sem lances",
  },
  {
    id: "TRC-203",
    name: "Araucária → Rio de Janeiro",
    status: "Em montagem",
    auctionStatus: "Em composição",
    targetFare: 6300,
    totalWeight: "320 kg",
    totalVolume: "5 m³",
    itemCount: 1,
    legCount: 1,
    risk: RISK.WARNING,
    stops: ["Araucária, PR", "Rio de Janeiro, RJ"],
    items: ["CRG-1045"],
    plannerNote: "Pronto para consolidar novos itens antes da publicação.",
    bidDeadline: "20/05/2026 15:00",
    bestBid: null,
    totalBids: 0,
    winningCarrier: null,
    coverage: "Rascunho",
  },
]

export const auctionBidsMock = [
  {
    id: "BID-5521",
    segmentRef: "TRC-201",
    carrier: "Expresso Sul Ltda",
    vehicle: "ABC-1234",
    proposedValue: 9150,
    eta: "18/05 19:30",
    status: "Em análise",
    coverage: "Cobertura total",
  },
  {
    id: "BID-5522",
    segmentRef: "TRC-201",
    carrier: "Rota Norte Logística",
    vehicle: "XYZ-9876",
    proposedValue: 9380,
    eta: "19/05 07:30",
    status: "Em análise",
    coverage: "Cobertura parcial",
  },
  {
    id: "BID-5523",
    segmentRef: "TRC-202",
    carrier: "TransVale Transportes",
    vehicle: "JKL-4421",
    proposedValue: 6880,
    eta: "20/05 17:40",
    status: "Em análise",
    coverage: "Cobertura total",
  },
]

export const freightOffersMock = [
  {
    id: "OFT-9921",
    contractor: "Volvo Cars do Brasil",
    segmentId: "TRC-201",
    segmentName: "Curitiba → São Paulo → Salvador",
    risk: RISK.WARNING,
    routeLabel: "Curitiba, PR → São Paulo, SP → Salvador, BA",
    targetValue: 9800,
    itemCount: 2,
    legCount: 2,
    totalWeight: "160 kg",
    totalVolume: "10 m³",
    requirements: ["Refrigerado", "Frágil"],
    urgency: "Alta",
    availableVehicles: 2,
    pickupLabel: "18/05/2026 08:00",
    etaLabel: "21/05/2026 12:00",
    itemsSummary: ["100 kg de peito de frango", "2 bicicletas"],
    bidStatus: "SENT",
    status: "OPEN", // OPEN | CLOSED (encerrada / virou rota ativa)
  },
  {
    id: "OFT-9924",
    contractor: "Indústrias Alpha",
    segmentId: "TRC-202",
    segmentName: "Joinville → Campinas",
    risk: RISK.CRITIC,
    routeLabel: "Joinville, SC → Campinas, SP",
    targetValue: 7200,
    itemCount: 1,
    legCount: 1,
    totalWeight: "1.5 ton",
    totalVolume: "9 m³",
    requirements: ["Carga seca"],
    urgency: "Normal",
    availableVehicles: 0,
    pickupLabel: "20/05/2026 07:30",
    etaLabel: "20/05/2026 20:00",
    itemsSummary: ["6 motores elétricos"],
    status: "OPEN",
  },
  {
    id: "OFT-9925",
    contractor: "TechLogistics",
    segmentId: "TRC-203",
    segmentName: "Araucária → Rio de Janeiro",
    risk: RISK.WARNING,
    routeLabel: "Araucária, PR → Rio de Janeiro, RJ",
    targetValue: 6300,
    itemCount: 1,
    legCount: 1,
    totalWeight: "320 kg",
    totalVolume: "5 m³",
    requirements: ["Refrigerado", "Frágil"],
    urgency: "Normal",
    availableVehicles: 5,
    pickupLabel: "20/05/2026 14:00",
    etaLabel: "21/05/2026 13:00",
    itemsSummary: ["24 caixas de vacina influenza"],
    // Oferta encerrada: o leilão terminou e virou uma rota ativa.
    status: "CLOSED",
    closedReason: "ROUTED",
    activeRouteId: "TRC-203",
    closedAt: "19/05/2026 09:00",
    winningBid: 6100,
  },
]

// ==========================================
// FROTA DE VEÍCULOS DA TRANSPORTADORA
// ==========================================
export const vehicleFleetMock = [
  {
    id: "VHC-001",
    plate: "ABC-1234",
    type: "Carreta Frigorífica",
    capacity: "28 ton",
    volumeM3: 90,
    driver: "João Silva",
    driverPhone: "+55 (41) 99888-7766",
    status: "AVAILABLE", // AVAILABLE | IN_TRANSIT | LOCKED | MAINTENANCE
    currentLocation: "Curitiba, PR",
    
    // Bloqueios (rotas confirmadas com período de lock)
    lockedPeriods: [],
    
    // Características técnicas
    features: ["Refrigeração -25ºC", "Rastreamento GPS", "Seguro Vigente"],
    lastMaintenance: "01/06/2026",
    nextMaintenance: "01/12/2026",
  },
  {
    id: "VHC-002",
    plate: "XYZ-9876",
    type: "Carreta Frigorífica",
    capacity: "28 ton",
    volumeM3: 90,
    driver: "Maria Santos",
    driverPhone: "+55 (41) 99777-5544",
    status: "LOCKED",
    currentLocation: "São Paulo, SP",
    
    lockedPeriods: [
      {
        routeId: "TRC-201",
        routeName: "Curitiba → São Paulo → Salvador",
        startDate: "2026-05-18T08:00:00",
        endDate: "2026-05-21T12:00:00",
        bidId: "BID-5521"
      }
    ],
    
    features: ["Refrigeração -25ºC", "Rastreamento GPS", "Seguro Vigente"],
    lastMaintenance: "15/05/2026",
    nextMaintenance: "15/11/2026",
  },
  {
    id: "VHC-003",
    plate: "JKL-4421",
    type: "Carreta Sider",
    capacity: "25 ton",
    volumeM3: 85,
    driver: "Carlos Pereira",
    driverPhone: "+55 (41) 99666-3322",
    status: "AVAILABLE",
    currentLocation: "Joinville, SC",
    
    lockedPeriods: [],
    
    features: ["Rastreamento GPS", "Seguro Vigente", "Lona Reforçada"],
    lastMaintenance: "10/05/2026",
    nextMaintenance: "10/11/2026",
  },
  {
    id: "VHC-004",
    plate: "MNO-7788",
    type: "Carreta Frigorífica",
    capacity: "28 ton",
    volumeM3: 90,
    driver: "Ana Costa",
    driverPhone: "+55 (41) 99555-1100",
    status: "IN_TRANSIT",
    currentLocation: "Rodovia BR-116, KM 245",
    
    lockedPeriods: [
      {
        routeId: "TRC-150",
        routeName: "Porto Alegre → Curitiba",
        startDate: "2026-06-10T14:00:00",
        endDate: "2026-06-11T18:00:00",
        bidId: "BID-5400"
      }
    ],
    
    features: ["Refrigeração -25ºC", "Rastreamento GPS", "Seguro Vigente"],
    lastMaintenance: "05/06/2026",
    nextMaintenance: "05/12/2026",
  },
  {
    id: "VHC-005",
    plate: "PQR-3344",
    type: "Carreta Baú",
    capacity: "20 ton",
    volumeM3: 75,
    driver: "Roberto Lima",
    driverPhone: "+55 (41) 99444-9988",
    status: "AVAILABLE",
    currentLocation: "Curitiba, PR",
    
    lockedPeriods: [],
    
    features: ["Rastreamento GPS", "Seguro Vigente"],
    lastMaintenance: "20/05/2026",
    nextMaintenance: "20/11/2026",
  },
  {
    id: "VHC-006",
    plate: "STU-5566",
    type: "Carreta Frigorífica",
    capacity: "28 ton",
    volumeM3: 90,
    driver: "Fernando Alves",
    driverPhone: "+55 (41) 99333-7766",
    status: "MAINTENANCE",
    currentLocation: "Oficina - Curitiba, PR",
    
    lockedPeriods: [],
    
    features: ["Refrigeração -25ºC", "Rastreamento GPS"],
    lastMaintenance: "10/06/2026",
    nextMaintenance: "10/07/2026",
    maintenanceReason: "Revisão do sistema de refrigeração",
  },
]

// ==========================================
// FUNÇÕES AUXILIARES PARA GESTÃO DE FROTA
// ==========================================

/**
 * Verifica se um veículo está disponível para uma rota em um período específico
 */
export const isVehicleAvailableForRoute = (vehicleId, routeStartDate, routeEndDate) => {
  const vehicle = vehicleFleetMock.find((v) => v.id === vehicleId)
  if (!vehicle) return false
  
  // Se está em manutenção, não está disponível
  if (vehicle.status === "MAINTENANCE") return false
  
  // Converte datas para comparação
  const routeStart = new Date(routeStartDate)
  const routeEnd = new Date(routeEndDate)
  
  // Verifica se há conflito com períodos bloqueados
  const hasConflict = vehicle.lockedPeriods.some((period) => {
    const periodStart = new Date(period.startDate)
    const periodEnd = new Date(period.endDate)
    
    // Verifica sobreposição de períodos
    return (routeStart < periodEnd && routeEnd > periodStart)
  })
  
  return !hasConflict
}

/**
 * Retorna todos os veículos disponíveis para uma rota
 */
export const getAvailableVehiclesForRoute = (routeStartDate, routeEndDate, requiredType = null) => {
  return vehicleFleetMock.filter((vehicle) => {
    const isAvailable = isVehicleAvailableForRoute(vehicle.id, routeStartDate, routeEndDate)
    const matchesType = !requiredType || vehicle.type.includes(requiredType)
    return isAvailable && matchesType
  })
}

/**
 * Bloqueia um veículo para uma rota específica
 */
export const lockVehicleForRoute = (vehicleId, routeId, routeName, startDate, endDate, bidId) => {
  const vehicle = vehicleFleetMock.find((v) => v.id === vehicleId)
  if (!vehicle) return false
  
  // Adiciona o período de bloqueio
  vehicle.lockedPeriods.push({
    routeId,
    routeName,
    startDate,
    endDate,
    bidId,
  })
  
  vehicle.status = "LOCKED"
  return true
}

/**
 * Libera um veículo de uma rota específica
 */
export const unlockVehicleFromRoute = (vehicleId, routeId) => {
  const vehicle = vehicleFleetMock.find((v) => v.id === vehicleId)
  if (!vehicle) return false
  
  vehicle.lockedPeriods = vehicle.lockedPeriods.filter((period) => period.routeId !== routeId)
  
  // Se não há mais bloqueios, marca como disponível
  if (vehicle.lockedPeriods.length === 0) {
    vehicle.status = "AVAILABLE"
  }
  
  return true
}

export const getCargoItemById = (cargoId) => cargoItemsMock.find((item) => item.id === cargoId)
export const getSegmentById = (segmentId) => segmentPlansMock.find((segment) => segment.id === segmentId)
