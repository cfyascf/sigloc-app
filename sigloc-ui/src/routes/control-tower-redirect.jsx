import { Navigate } from "react-router-dom"
import { useAuth } from "@/contexts/AuthContext"
import { ROLES } from "@/constants/roles"

/** `/control-tower` used to be a mock page; send old links to the real trip list of the role. */
export default function ControlTowerRedirect() {
  const { user } = useAuth()
  return <Navigate to={user?.role === ROLES.CARRIER ? "/carrier-route-tracking" : "/active-routes"} replace />
}
