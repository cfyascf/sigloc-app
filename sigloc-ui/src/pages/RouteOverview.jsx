import { useState } from "react"
import { Map, MapPin, MoreHorizontal, Pencil, Plus, Trash2 } from "lucide-react"
import { Link } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"

export default function RouteOverview() {
  // Mock inicial das rotas cadastradas
  const [rotas, setRotas] = useState([
    {
      id: "RT-001",
      nome: "Sul-Sudeste",
      origem: "Curitiba, PR",
      destino: "São Paulo, SP",
    },
    {
      id: "RT-002",
      nome: "Rota PR-MT",
      origem: "Sorriso, MT",
      destino: "Paranaguá, PR",
    },
    {
      id: "RT-003",
      nome: "Trajeto RJ-MG",
      origem: "Rio de Janeiro, RJ",
      destino: "Belo Horizonte, MG",
    }
  ])

  // Função simples para simular a exclusão
  const handleDelete = (idParaDeletar) => {
    // No TCC real, aqui iria uma chamada para a sua API C# (ex: axios.delete)
    setRotas(rotas.filter(rota => rota.id !== idParaDeletar))
  }

  // Função simples para simular a edição
  const handleEdit = (rota) => {
    alert(`Abrir modal de edição para a rota: ${rota.nome}`)
  }

  return (
    <AppShell 
      title="Gestão de Trechos da Frota"
      actions={
        <Button asChild className="w-full bg-slate-900 text-white hover:bg-slate-800 md:w-auto">
          <Link to="/load-management">
            <Plus size={16} className="mr-2" />
            Criar rota
          </Link>
        </Button>
      }
    >
      <div className="flex flex-col space-y-6 p-2 md:p-6">
        
       

        {/* Tabela de Rotas */}
        <Card className="border-slate-200 shadow-sm">
          <CardHeader className="bg-slate-50/50 border-b border-slate-100 pb-4">
            <CardTitle className="text-lg font-semibold flex items-center text-slate-800">
              <Map size={18} className="mr-2 text-blue-600" />
              Trechos Cadastrados
            </CardTitle>
          </CardHeader>
          
          <CardContent className="p-0">
            <Table>
              <TableHeader className="bg-slate-50">
                <TableRow>
                  <TableHead className="w-[120px] font-semibold">ID da Rota</TableHead>
                  <TableHead className="font-semibold">Nome</TableHead>
                  <TableHead className="font-semibold">Local de Saída</TableHead>
                  <TableHead className="font-semibold">Destino</TableHead>
                  <TableHead className="text-right font-semibold">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rotas.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-8 text-slate-500">
                      Nenhum trecho cadastrado no momento.
                    </TableCell>
                  </TableRow>
                ) : (
                  rotas.map((rota) => (
                    <TableRow key={rota.id} className="hover:bg-slate-50/50 transition-colors">
                      <TableCell className="font-medium text-slate-600">
                        {rota.id}
                      </TableCell>
                      
                      <TableCell className="font-semibold text-slate-800">
                        {rota.nome}
                      </TableCell>
                      
                      <TableCell>
                        <div className="flex items-center text-slate-600">
                          <MapPin size={14} className="mr-1.5 text-slate-400" />
                          {rota.origem}
                        </div>
                      </TableCell>
                      
                      <TableCell>
                        <div className="flex items-center text-slate-600">
                          <MapPin size={14} className="mr-1.5 text-blue-500" />
                          {rota.destino}
                        </div>
                      </TableCell>
                      
                      {/* Menu de 3 pontos (Ações) */}
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
                              onClick={() => handleEdit(rota)}
                            >
                              <Pencil size={14} className="mr-2" />
                              Editar
                            </DropdownMenuItem>
                            
                            <DropdownMenuItem 
                              className="cursor-pointer text-red-600 focus:bg-red-50 focus:text-red-700"
                              onClick={() => handleDelete(rota.id)}
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