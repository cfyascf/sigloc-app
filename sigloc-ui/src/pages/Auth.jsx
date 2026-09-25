import { Shield } from "lucide-react"
import { useLocation } from "react-router-dom"

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { LoginForm } from "@/components/auth/LoginForm"
import { RegisterForm } from "@/components/auth/RegisterForm"

export default function Auth() {
  const location = useLocation()
  const activeTab = location.pathname === "/register" ? "register" : "login"

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans">
      {/* Lado Esquerdo: Branding / Apresentação */}
      <div className="hidden w-1/2 flex-col justify-between bg-slate-900 p-12 text-white lg:flex">
        <div>
          <div className="mb-8 flex h-12 w-12 items-center justify-center rounded-lg bg-blue-600">
            <Shield size={28} className="text-white" />
          </div>
          <h1 className="mb-4 text-4xl font-bold tracking-tight">
            Sig<span className="text-blue-500">loc</span>
          </h1>
          <p className="max-w-md text-lg text-slate-400">
            A plataforma definitiva de consolidação de cargas e prevenção de
            overbooking. Conectando operadores logísticos a transportadoras com
            eficiência e segurança.
          </p>
        </div>

        <div className="space-y-4 text-sm text-slate-500">
          <p>&copy; 2026 Sigloc Systems.</p>
          <p>TCC Engineering Project</p>
        </div>
      </div>

      {/* Lado Direito: Formulários */}
      <div className="flex w-full flex-col justify-center px-8 sm:px-16 lg:w-1/2 xl:px-32">
        <div className="mx-auto w-full max-w-md py-12">
          <Tabs defaultValue={activeTab} className="w-full">
            <TabsList className="mb-8 grid w-full grid-cols-2 bg-slate-200/50 p-1">
              <TabsTrigger
                value="login"
                className="data-[state=active]:bg-white data-[state=active]:shadow-sm"
              >
                Entrar
              </TabsTrigger>
              <TabsTrigger
                value="register"
                className="data-[state=active]:bg-white data-[state=active]:shadow-sm"
              >
                Criar Conta
              </TabsTrigger>
            </TabsList>

            <TabsContent value="login">
              <LoginForm />
            </TabsContent>

            <TabsContent value="register">
              <RegisterForm />
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </div>
  )
}
