/* eslint-disable react/prop-types */
import { Navigate, Outlet, useLocation } from "react-router-dom"
import { useAuth } from "@/contexts/AuthContext"
import { getDefaultRouteForRole } from "@/constants/auth"
import { ROLES } from "@/constants/roles"

export function RequireAuth() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    // Send the user to login page but store the location they tried to access,
    // so they can be redirected to it after they login
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  // Outlet renders the matched child route
  return <Outlet />
}

export function PublicOnly() {
  const { isAuthenticated, user } = useAuth()
  const location = useLocation()

  if (isAuthenticated) {
    // This guard re-renders as soon as login succeeds, before the form's own redirect runs, so
    // it must also honour the page RequireAuth saved; otherwise deep links land on the dashboard.
    const target = location.state?.from?.pathname || getDefaultRouteForRole(user?.role)
    return <Navigate to={target} replace />
  }

  // Outlet renders the matched child route
  return <Outlet />
}

export function RequireRole({ allowedRoles }) {
  const { user } = useAuth()

  const hasRoleAccess =
    user.role === ROLES.DEVELOPER || allowedRoles.includes(user.role)

  if (!hasRoleAccess) {
    return <Navigate to={getDefaultRouteForRole(user?.role)} replace />
  }

  // Outlet renders the matched child route
  return <Outlet />
}
