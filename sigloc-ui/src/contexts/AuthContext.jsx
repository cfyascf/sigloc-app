/* eslint-disable react/prop-types */
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react"

import { ROLES } from "@/constants/roles"
import { authService } from "@/services/auth-service"
import { setAuthTokenProvider } from "@/lib/api-client"

const AuthContext = createContext()

const TOKEN_STORAGE_KEY = "sigloc_token"
const USER_STORAGE_KEY = "sigloc_user"

const VALID_ROLES = new Set([ROLES.CONTRACTOR, ROLES.CARRIER, ROLES.DEVELOPER])
const ROLE_CYCLE = [ROLES.CONTRACTOR, ROLES.CARRIER, ROLES.DEVELOPER]

function readStoredUser() {
  if (globalThis.window === undefined) {
    return null
  }

  const raw = localStorage.getItem(USER_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw)
    return parsed && VALID_ROLES.has(parsed.role) ? parsed : null
  } catch {
    return null
  }
}

function readStoredToken() {
  if (globalThis.window === undefined) {
    return null
  }

  return localStorage.getItem(TOKEN_STORAGE_KEY) || null
}

function writeUser(user) {
  if (globalThis.window !== undefined) {
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user))
  }
}

export function AuthProvider({ children }) {
  // Session is initialized from storage so it survives page refreshes.
  const [token, setToken] = useState(readStoredToken)
  const [user, setUser] = useState(readStoredUser)

  // Expose the token to the API client without re-registering the provider on
  // every token change — the ref always points at the latest value.
  const tokenRef = useRef(token)

  useEffect(() => {
    tokenRef.current = token
  }, [token])

  useEffect(() => {
    setAuthTokenProvider(() => tokenRef.current)
  }, [])

  const isAuthenticated = Boolean(token && user)

  const persistSession = useCallback((session) => {
    setToken(session.token)
    setUser(session.user)

    if (globalThis.window !== undefined) {
      localStorage.setItem(TOKEN_STORAGE_KEY, session.token ?? "")
      writeUser(session.user)
    }
  }, [])

  const logout = useCallback(() => {
    setToken(null)
    setUser(null)

    if (globalThis.window !== undefined) {
      localStorage.removeItem(TOKEN_STORAGE_KEY)
      localStorage.removeItem(USER_STORAGE_KEY)
    }
  }, [])

  // Async wrappers around the auth service that persist the resulting session.
  const login = useCallback(
    async (credentials) => {
      const session = await authService.login(credentials)
      persistSession(session)
      return session
    },
    [persistSession]
  )

  const loginWithGoogle = useCallback(
    async (params) => {
      const session = await authService.loginWithGoogle(params)
      persistSession(session)
      return session
    },
    [persistSession]
  )

  const registerContractor = useCallback(
    async (data) => {
      const session = await authService.registerContractor(data)
      persistSession(session)
      return session
    },
    [persistSession]
  )

  const registerContractorWithGoogle = useCallback(
    async (data) => {
      const session = await authService.registerContractorWithGoogle(data)
      persistSession(session)
      return session
    },
    [persistSession]
  )

  const setRole = useCallback((role) => {
    if (!VALID_ROLES.has(role)) {
      return
    }

    setUser((prev) => {
      const next = { ...(prev ?? {}), role }
      writeUser(next)
      return next
    })
  }, [])

  const toggleRole = useCallback(() => {
    setUser((prev) => {
      if (!prev) {
        return prev
      }

      const next = {
        ...prev,
        role: ROLE_CYCLE[(ROLE_CYCLE.indexOf(prev.role) + 1) % ROLE_CYCLE.length],
      }
      writeUser(next)
      return next
    })
  }, [])

  const value = useMemo(
    () => ({
      user,
      token,
      isAuthenticated,
      login,
      loginWithGoogle,
      registerContractor,
      registerContractorWithGoogle,
      logout,
      setRole,
      toggleRole,
    }),
    [
      user,
      token,
      isAuthenticated,
      login,
      loginWithGoogle,
      registerContractor,
      registerContractorWithGoogle,
      logout,
      setRole,
      toggleRole,
    ]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)
