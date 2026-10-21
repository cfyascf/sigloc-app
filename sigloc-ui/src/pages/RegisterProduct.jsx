import { useMemo } from "react"
import { ArrowLeft, Loader2, Save, X } from "lucide-react"
import { Link, useNavigate } from "react-router-dom"

import AppShell from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { FormAlert } from "@/components/auth/FormAlert"
import { ProductFormFields } from "@/components/products/ProductFormFields"
import { useProductForm } from "@/hooks/use-product-form"
import { useAsyncAction } from "@/hooks/use-async-action"
import { productService } from "@/services/product-service"
import { mapProductFieldErrors } from "@/constants/products"

export default function RegisterProduct() {
  const navigate = useNavigate()
  const { form, setField, showTemperature, showPackaging } = useProductForm()

  const { run, pending, error, fieldErrors } = useAsyncAction((payload) =>
    productService.createProduct(payload)
  )

  const mappedFieldErrors = useMemo(
    () => mapProductFieldErrors(fieldErrors),
    [fieldErrors]
  )

  const handleSubmit = async (event) => {
    event.preventDefault()
    const result = await run(form)
    if (result.ok) {
      navigate("/product-management")
    }
  }

  return (
    <AppShell title="Novo Cadastro de SKU">
      <form
        onSubmit={handleSubmit}
        className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-5xl flex-col overflow-hidden"
      >
        <div className="mb-5 flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1">
          <Link to="/product-management">
            <Button
              type="button"
              variant="ghost"
              className="h-auto p-0 text-sm font-medium text-slate-500 hover:bg-transparent hover:text-slate-900"
            >
              <ArrowLeft size={16} className="mr-2" /> Voltar ao Catálogo
            </Button>
          </Link>

          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="outline"
              disabled={pending}
              className="h-9 border-slate-200 bg-white text-xs font-semibold text-slate-700"
              onClick={() => navigate("/product-management")}
            >
              <X size={14} className="mr-1.5" /> Cancelar
            </Button>
            <Button
              type="submit"
              disabled={pending}
              className="h-9 bg-blue-600 text-xs font-bold tracking-wide text-white hover:bg-blue-700"
            >
              {pending ? (
                <Loader2 size={14} className="mr-1.5 animate-spin" />
              ) : (
                <Save size={14} className="mr-1.5" />
              )}
              Salvar Novo SKU
            </Button>
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="h-full overflow-y-auto pb-6 pr-2">
            {error ? (
              <div className="mb-5">
                <FormAlert message={error.message} />
              </div>
            ) : null}

            <ProductFormFields
              form={form}
              setField={setField}
              fieldErrors={mappedFieldErrors}
              showTemperature={showTemperature}
              showPackaging={showPackaging}
              disabled={pending}
            />
          </div>
        </div>
      </form>
    </AppShell>
  )
}
