/**
 * CENTRALIZED ROUTE MANIFEST
 *
 * Each entry in ROUTES is the single source of truth for a route's:
 *   - path       — URL path used by React Router
 *   - element    — Page component to render
 *   - access     — Who can see this route:
 *                    "public"          → unauthenticated users only (PublicOnly guard)
 *                    "auth"            → any authenticated user (RequireAuth guard)
 *                    [ROLES.X, ...]    — specific role(s) (RequireRole guard)
 *   - nav        — true if this route should appear in the sidebar navigation
 *   - label      — sidebar display text (required when nav: true)
 *   - icon       — Lucide icon component (required when nav: true)
 *
 * Adding a new page = add one entry here. Route guard and sidebar nav update automatically.
 */

import {
  Box,
  ChartBar,
  Link2,
  Package,
  Truck,
} from "lucide-react"

import AuthPage from "@/pages/Auth"
import ContractorDashboard from "@/pages/ContractorDashboard"
import CarrierDashboard from "@/pages/CarrierDashboard"
import ActiveRoutes from "@/pages/ActiveRoutes"
import ControlTower from "@/pages/ControlTower"
import RegisterVehicle from "@/pages/RegisterVehicle"
import FreightsOffersOverview from "@/pages/FreightsOffersOverview"
import RouteSegmentManagement from "@/pages/RouteSegmentManagement"
import CreateRouteSegment from "@/pages/CreateRouteSegment"
import RouteSegmentDetails from "@/pages/RouteSegmentDetails"
import ActiveRouteTracking from "@/pages/ActiveRouteTracking"
import OfferFreight from "@/pages/OfferFreight"
import RegisterProduct from "@/pages/RegisterProduct"
import AuctionBids from "@/pages/AuctionBids"
import FreightsOfferedOverview from "@/pages/FreightsOfferedOverview"
import BidAnalysis from "@/pages/BidAnalysis"
import ProductManagement from "@/pages/ProductManagement"
import FleetManagement from "@/pages/FleetManagement"
import PartnerNetwork from "@/pages/PartnerNetwork"

import { ROLES } from "@/constants/roles"
import { Auth as AuthAccess } from "@/constants/auth"

export const ROUTES = [
  // Public only (unauthenticated access only)
  { path: "/",        element: <AuthPage />,  access: AuthAccess.PUBLIC, nav: false },
  { path: "/auth",    element: <AuthPage />,  access: AuthAccess.PUBLIC, nav: false },
  { path: "/login",   element: <AuthPage />,  access: AuthAccess.PUBLIC, nav: false },
  { path: "/register",element: <AuthPage />,  access: AuthAccess.PUBLIC, nav: false },


  // Dashboards by role
  {
    path: "/contractor-dashboard",
    element: <ContractorDashboard />,
    label: "Visão Geral",
    icon: ChartBar,
    access: [ROLES.DEVELOPER, ROLES.CONTRACTOR],
    nav: true,
  },
  {
    path: "/carrier-dashboard",
    element: <CarrierDashboard />,
    label: "Visão Geral",
    icon: ChartBar,
    access: [ROLES.DEVELOPER, ROLES.CARRIER],
    nav: true,
  },

  // Authenticated only (all roles)
  { path: "/control-tower", element: <ControlTower />, access: AuthAccess.AUTHENTICATED, nav: false },
  { path: "/register-vehicle",       element: <RegisterVehicle />,       access: AuthAccess.AUTHENTICATED, nav: false },
  {
    path: "/partner-network",
    element: <PartnerNetwork />,
    label: "Rede de Parceiros",
    icon: Link2,
    access: [ROLES.CONTRACTOR, ROLES.CARRIER],
    nav: true,
  },


  // Contractor only
  {
    path: "/freights-offered-overview",
    element: <FreightsOfferedOverview />,
    label: "Painel de Leilões",
    icon: Truck,
    access: [ROLES.CONTRACTOR],
    nav: true,
  },
  {
    path: "/route-segment-management",
    element: <RouteSegmentManagement />,
    label: "Gestão de Trechos",
    icon: Box,
    access: [ROLES.CONTRACTOR],
    nav: true,
  },
  { path: "/create-route-segment", element: <CreateRouteSegment />, access: [ROLES.CONTRACTOR], nav: false },
  { path: "/route-segment-details/:loadId", element: <RouteSegmentDetails />, access: [ROLES.CONTRACTOR], nav: false },
  {
    path: "/active-routes",
    element: <ActiveRoutes />,
    label: "Rotas Ativas",
    icon: Truck,
    access: [ROLES.CONTRACTOR],
    nav: true,
  },
  { path: "/active-route-tracking/:routeId", element: <ActiveRouteTracking />, access: [ROLES.CONTRACTOR], nav: false },
  { path: "/offer-freight", element: <OfferFreight />, access: [ROLES.CONTRACTOR], nav: false },
  { path: "/register-product", element: <RegisterProduct />, access: [ROLES.CONTRACTOR], nav: false },
  { path: "/auction-bids/:segmentId", element: <AuctionBids />, access: [ROLES.CONTRACTOR], nav: false },
  {
    path: "/product-management",
    element: <ProductManagement />,
    label: "Produtos",
    icon: Package,
    access: [ROLES.CONTRACTOR],
    nav: true,
  },

  
  // Carrier only
  {
    path: "/freights-offers-overview",
    element: <FreightsOffersOverview />,
    label: "Oportunidades de Frete",
    icon: Truck,
    access: [ROLES.CARRIER],
    nav: true,
  },
  { path: "/bid-analysis/:segmentId", element: <BidAnalysis />, access: [ROLES.CARRIER], nav: false },
  {
    path: "/fleet-management",
    element: <FleetManagement />,
    label: "Gestão de Frota",
    icon: Truck,
    access: [ROLES.CARRIER],
    nav: true,
  }
]

/**
 * Returns the sidebar nav items visible to a given role.
 *
 * Rules:
 *  - nav: false  → always excluded
 *  - access: AuthAccess.PUBLIC         → excluded (not authenticated pages)
 *  - access: AuthAccess.AUTHENTICATED  → included for every role
 *  - access: [roles]  → included if role is in the array OR role is DEVELOPER
 *    (DEVELOPER sees everything, mirroring RequireRole's bypass logic)
 */
export function getNavItems(role) {
  const navItems = ROUTES.filter((route) => {
    if (!route.nav) return false
    if (route.access === AuthAccess.PUBLIC) return false
    if (route.access === AuthAccess.AUTHENTICATED) return true
    return role === ROLES.DEVELOPER || route.access.includes(role)
  })

  const partnerNetworkIndex = navItems.findIndex((route) => route.path === "/partner-network")

  if (partnerNetworkIndex === -1) {
    return navItems
  }

  const [partnerNetworkRoute] = navItems.splice(partnerNetworkIndex, 1)
  navItems.push(partnerNetworkRoute)

  return navItems
}
