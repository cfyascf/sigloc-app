import { Navigate, Route, Routes } from "react-router-dom"
import { ROLES } from "@/constants/roles"
import { Auth as AuthAccess } from "@/constants/auth"
import { PublicOnly, RequireAuth, RequireRole } from "@/routes/route-guards"
import { ROUTES } from "@/routes/route-config"
import { NotFound } from "@/pages/NotFound"
import Invite from "@/pages/Invite"

const publicRoutes     = ROUTES.filter((r) => r.access === AuthAccess.PUBLIC)
const authRoutes       = ROUTES.filter((r) => r.access === AuthAccess.AUTHENTICATED)
const sharedBusinessRoutes = ROUTES.filter((r) => Array.isArray(r.access) && r.access.includes(ROLES.CONTRACTOR) && r.access.includes(ROLES.CARRIER))
const contractorRoutes = ROUTES.filter((r) => Array.isArray(r.access) && r.access.includes(ROLES.CONTRACTOR) && !r.access.includes(ROLES.CARRIER))
const carrierRoutes    = ROUTES.filter((r) => Array.isArray(r.access) && r.access.includes(ROLES.CARRIER) && !r.access.includes(ROLES.CONTRACTOR))

export function Router() {
  return (
    <Routes>
      {/* Invite landing — reachable whether the visitor is logged in or not. */}
      <Route path="/convite/:token" element={<Invite />} />
      <Route path="/invite/:token" element={<Invite />} />

      {/* Unauthenticated-only pages */}
      <Route element={<PublicOnly />}>
        {publicRoutes.map((r) => (
          <Route key={r.path} path={r.path} element={r.element} />
        ))}
      </Route>

      {/* Authenticated pages */}
      <Route element={<RequireAuth />}>
        {authRoutes.map((r) => (
          <Route key={r.path} path={r.path} element={r.element} />
        ))}

        <Route element={<RequireRole allowedRoles={[ROLES.CONTRACTOR, ROLES.CARRIER]} />}>
          {sharedBusinessRoutes.map((r) => (
            <Route key={r.path} path={r.path} element={r.element} />
          ))}
        </Route>

        <Route element={<RequireRole allowedRoles={[ROLES.CONTRACTOR]} />}>
          {contractorRoutes.map((r) => (
            <Route key={r.path} path={r.path} element={r.element} />
          ))}
        </Route>

        <Route element={<RequireRole allowedRoles={[ROLES.CARRIER]} />}>
          {carrierRoutes.map((r) => (
            <Route key={r.path} path={r.path} element={r.element} />
          ))}
        </Route>
      </Route>

      <Route path="*" element={<NotFound />} />
      <Route path="/home" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default Router