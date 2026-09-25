import { useCallback } from "react"
import { useLocation, useNavigate } from "react-router-dom"

import { getDefaultRouteForRole } from "@/constants/auth"

/**
 * Returns a callback that redirects the user after a successful auth action.
 *
 * Precedence:
 *   1. The originally requested location (set by RequireAuth on redirect).
 *   2. The role's default route.
 */
export function usePostAuthRedirect() {
  const navigate = useNavigate()
  const location = useLocation()

  return useCallback(
    (role) => {
      const nextPath =
        location.state?.from?.pathname || getDefaultRouteForRole(role)
      navigate(nextPath, { replace: true })
    },
    [location.state, navigate]
  )
}
