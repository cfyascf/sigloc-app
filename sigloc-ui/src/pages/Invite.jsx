import { useCallback, useEffect, useState } from "react"
import { Link, useParams } from "react-router-dom"
import { AlertCircle, ArrowRight, CheckCircle2, Loader2, Shield } from "lucide-react"

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { InviteRegisterForm } from "@/components/auth/InviteRegisterForm"
import { InviteLoginForm } from "@/components/auth/InviteLoginForm"
import { useAuth } from "@/contexts/AuthContext"
import { useAsyncAction } from "@/hooks/use-async-action"
import { ROLES } from "@/constants/roles"
import { getDefaultRouteForRole } from "@/constants/auth"
import { authService } from "@/services/auth-service"

function InviteShell({ children }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-4 font-sans">
      <div className="w-full max-w-md">
        <div className="mb-8 flex flex-col items-center gap-3 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-blue-600">
            <Shield size={28} className="text-white" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-900">
            Sig<span className="text-blue-600">loc</span>
          </h1>
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
          {children}
        </div>
      </div>
    </div>
  )
}

function CenteredState({ icon, title, description, children }) {
  return (
    <div className="flex flex-col items-center gap-3 text-center">
      {icon}
      <h2 className="text-xl font-bold text-slate-900">{title}</h2>
      {description ? <p className="text-sm text-slate-500">{description}</p> : null}
      {children}
    </div>
  )
}

export default function Invite() {
  const { token } = useParams()
  const { isAuthenticated, user } = useAuth()

  const validateAction = useAsyncAction(() => authService.validateInvite(token))
  const autoConnectAction = useAsyncAction(() => authService.connectByInvite(token))

  const [validation, setValidation] = useState(null)
  const [connected, setConnected] = useState(null)

  const contractorName = connected ?? validation?.contractorName ?? "o embarcador"

  const isLoggedCarrier = isAuthenticated && user?.role === ROLES.CARRIER
  const isLoggedContractor = isAuthenticated && user?.role === ROLES.CONTRACTOR

  useEffect(() => {
    let active = true
    validateAction.run().then((result) => {
      if (active && result.ok) {
        setValidation(result.data)
      }
    })
    return () => {
      active = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

  // A logged-in carrier accepts the invite automatically once it is validated.
  useEffect(() => {
    if (!isLoggedCarrier || !validation?.valid || connected !== null) {
      return
    }

    autoConnectAction.run().then((result) => {
      if (result.ok || result.error?.code === "PARCERIA_JA_EXISTE") {
        setConnected(result.data?.contractorName ?? validation.contractorName ?? null)
      }
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoggedCarrier, validation, connected])

  // Registration succeeds with an active partnership already created; show the
  // success state (the user is now authenticated as a carrier).
  const handleSuccess = useCallback(() => {
    setConnected(validation?.contractorName ?? "")
  }, [validation])

  const handleConnected = useCallback((name) => {
    setConnected(name ?? "")
  }, [])

  // 1. Validating the token.
  if (validateAction.pending) {
    return (
      <InviteShell>
        <CenteredState
          icon={<Loader2 size={28} className="animate-spin text-blue-600" />}
          title="Validando convite..."
        />
      </InviteShell>
    )
  }

  // 2. Invalid / expired token.
  if (validateAction.error || !validation?.valid) {
    return (
      <InviteShell>
        <CenteredState
          icon={<AlertCircle size={28} className="text-rose-500" />}
          title="Convite inválido"
          description={
            validateAction.error?.message ||
            validation?.message ||
            "Este convite é inválido ou expirou."
          }
        >
          <Button asChild variant="outline" className="mt-2">
            <Link to="/">Ir para o início</Link>
          </Button>
        </CenteredState>
      </InviteShell>
    )
  }

  // 3. Connection created (either auto for a logged carrier, or after login).
  if (connected !== null) {
    return (
      <InviteShell>
        <CenteredState
          icon={<CheckCircle2 size={28} className="text-emerald-600" />}
          title="Parceria estabelecida!"
          description={`Sua conexão com ${connected || contractorName} foi criada com sucesso.`}
        >
          <Button asChild className="mt-2 bg-blue-600 text-white hover:bg-blue-700">
            <Link to="/partner-network">
              Ver Rede de Parceiros <ArrowRight size={16} className="ml-1.5" />
            </Link>
          </Button>
        </CenteredState>
      </InviteShell>
    )
  }

  // 4. Logged-in carrier: auto-connection in flight or failed.
  if (isLoggedCarrier) {
    return (
      <InviteShell>
        {autoConnectAction.error ? (
          <CenteredState
            icon={<AlertCircle size={28} className="text-rose-500" />}
            title="Não foi possível conectar"
            description={autoConnectAction.error.message}
          >
            <Button
              className="mt-2 bg-blue-600 text-white hover:bg-blue-700"
              onClick={() =>
                autoConnectAction.run().then((result) => {
                  if (result.ok || result.error?.code === "PARCERIA_JA_EXISTE") {
                    setConnected(result.data?.contractorName ?? validation.contractorName ?? null)
                  }
                })
              }
            >
              Tentar novamente
            </Button>
          </CenteredState>
        ) : (
          <CenteredState
            icon={<Loader2 size={28} className="animate-spin text-blue-600" />}
            title="Conectando..."
            description={`Estabelecendo parceria com ${contractorName}.`}
          />
        )}
      </InviteShell>
    )
  }

  // 5. Logged-in as a contractor: invites are for carriers.
  if (isLoggedContractor) {
    return (
      <InviteShell>
        <CenteredState
          icon={<AlertCircle size={28} className="text-amber-500" />}
          title="Convite para transportadoras"
          description={`${contractorName} enviou este convite para uma transportadora. Convites não podem ser aceitos por uma conta de embarcador.`}
        >
          <Button asChild variant="outline" className="mt-2">
            <Link to={getDefaultRouteForRole(user?.role)}>Voltar ao painel</Link>
          </Button>
        </CenteredState>
      </InviteShell>
    )
  }

  // 6. Not authenticated: register or login as a carrier, right here.
  return (
    <InviteShell>
      <div className="mb-6 space-y-2 text-center">
        <h2 className="text-2xl font-bold tracking-tight text-slate-900">
          Convite de parceria
        </h2>
        <p className="text-sm text-slate-500">
          <span className="font-semibold text-slate-700">{contractorName}</span>{" "}
          convidou sua transportadora para se conectar no Sigloc.
        </p>
      </div>

      <Tabs defaultValue="register" className="w-full">
        <TabsList className="mb-6 grid w-full grid-cols-2 bg-slate-200/50 p-1">
          <TabsTrigger
            value="register"
            className="data-[state=active]:bg-white data-[state=active]:shadow-sm"
          >
            Criar Conta
          </TabsTrigger>
          <TabsTrigger
            value="login"
            className="data-[state=active]:bg-white data-[state=active]:shadow-sm"
          >
            Já tenho conta
          </TabsTrigger>
        </TabsList>

        <TabsContent value="register">
          <InviteRegisterForm token={token} onSuccess={handleSuccess} />
        </TabsContent>

        <TabsContent value="login">
          <InviteLoginForm token={token} onConnected={handleConnected} />
        </TabsContent>
      </Tabs>
    </InviteShell>
  )
}
