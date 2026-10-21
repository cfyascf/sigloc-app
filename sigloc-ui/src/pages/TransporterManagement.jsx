import { useState } from "react"
import { MoreHorizontal, Pencil, Plus, Trash2, UserSquare2, Phone, Star, IdCard, CheckCircle2, Truck } from "lucide-react"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Badge } from "@/components/ui/badge"

export default function TransporterManagement() {
  // Mock inicial dos transportadores
  const [transportadores, setTransportadores] = useState([
    {
      id: "TRANSP-101",
      nome: "Lopes Logística LTDA",
      cnpj: "00.000.000/0001-91"
    },
    {
      id: "TRANSP-102",
      nome: "Fedex",
      cnpj: "33.000.167/0001-01"
    },
    {
      id: "TRANSP-103",
      nome: "DHL",
      cnpj: "07.526.557/0001-00"
    }
  ])

  const handleDelete = (idParaDeletar) => {
    setTransportadores(transportadores.filter(transportador => transportador.id !== idParaDeletar))
  }

  const handleEdit = (transportador) => {
    alert(`Abrir modal de edição para o transportador: ${transportador.nome}`)
  }

  // Função para estilizar o status dinamicamente
  const getStatusBadge = (status) => {
    switch (status) {
      case "Livre":
        return <Badge className="bg-emerald-50 text-emerald-600 border-emerald-200 hover:bg-emerald-100">Livre</Badge>
      case "Em Viagem":
        return <Badge className="bg-blue-50 text-blue-600 border-blue-200 hover:bg-blue-100">Em Viagem</Badge>
      case "Inativo":
        return <Badge variant="secondary" className="bg-slate-100 text-slate-500">Inativo</Badge>
      default:
        return <Badge>{status}</Badge>
    }
  }

  return (
    <AppShell 
      title="Gestão de transportadores"
      actions={
        <Button className="w-full bg-slate-900 text-white hover:bg-slate-800 md:w-auto">
          <Plus size={16} className="mr-2" /> 
          Novo Transportador
        </Button>
      }
    >
      <div className="flex flex-col space-y-6 p-2 md:p-6">
        
        <Card className="border-slate-200 shadow-sm">
          <CardHeader className="bg-slate-50/50 border-b border-slate-100 pb-4">
            <CardTitle className="text-lg font-semibold flex items-center text-slate-800">
              <UserSquare2 size={18} className="mr-2 text-blue-600" />
              Transportadores Cadastrados
            </CardTitle>
          </CardHeader>
          
          <CardContent className="p-0">
            <Table>
              <TableHeader className="bg-slate-50">
                <TableRow>
                  <TableHead className="w-[250px] font-semibold">Transportador</TableHead>
                  <TableHead className="font-semibold">Documentação</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {transportadores.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8 text-slate-500">
                      Nenhum transportador cadastrado no momento.
                    </TableCell>
                  </TableRow>
                ) : (
                  transportadores.map((transportador) => (
                    <TableRow key={transportador.id} className="hover:bg-slate-50/50 transition-colors">
                      
                      {/* Nome e CNPJ */}
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="font-semibold text-slate-800">{transportador.nome}</span>
                        </div>
                      </TableCell>
                       {/* Nome e CNPJ */}
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="text-xs text-slate-500 mt-0.5 font-medium">CNPJ: {transportador.cnpj}</span>
                        </div>
                      </TableCell>
                      
                      {/* Menu de Ações */}
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" className="h-8 w-8 p-0 hover:bg-slate-200">
                              <span className="sr-only">Abrir menu</span>
                              <MoreHorizontal size={16} className="text-slate-600" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end" className="w-[160px]">
                            
                            <DropdownMenuItem 
                              className="cursor-pointer text-slate-700"
                              onClick={() => handleEdit(transportador)}
                            >
                              <Pencil size={14} className="mr-2" />
                              Editar
                            </DropdownMenuItem>
                            
                            <DropdownMenuItem 
                              className="cursor-pointer text-red-600 focus:bg-red-50 focus:text-red-700"
                              onClick={() => handleDelete(transportador.id)}
                            >
                              <Trash2 size={14} className="mr-2" />
                              Excluir
                            </DropdownMenuItem>
                            
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>

                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

      </div>
    </AppShell>
  )
}