import { useState } from "react"
import { FileUp, Loader2 } from "lucide-react"

import { FormAlert } from "@/components/auth/FormAlert"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import { PROOF_ACCEPT, PROOF_MAX_BYTES, TRIP_NOTE_MAX_LENGTH } from "@/constants/trip-occurrences"
import { formatFileSize, formatTripDate, stopActionLabel } from "@/lib/trip-formatters"

const ALLOWED_EXTENSIONS = /\.(jpe?g|png|pdf)$/i

// Fast feedback only: the API detects the real type from the file content.
function checkProof(file) {
  if (!ALLOWED_EXTENSIONS.test(file.name)) return "Envie um arquivo JPG, PNG ou PDF."
  if (file.size > PROOF_MAX_BYTES) return "O comprovante deve ter no máximo 10 MB."
  if (file.size === 0) return "O arquivo selecionado está vazio."
  return null
}

/**
 * Confirms the carrier's current stop. `action` is the normalized `acaoAtual`; deliveries require a
 * proof of delivery. `onSubmit({ file, note })` runs the request; `error`/`fieldErrors` come from it.
 */
export function CompleteStopDialog({ open, onOpenChange, action, stop, pending, error, fieldErrors, onSubmit }) {
  return (
    <Dialog open={open} onOpenChange={(next) => !pending && onOpenChange(next)}>
      <DialogContent className="sm:max-w-[520px]">
        {/* Remounted on every open, so the form never keeps a previous stop's file. */}
        {action && (
          <CompleteStopForm action={action} stop={stop} pending={pending} error={error} fieldErrors={fieldErrors} onSubmit={onSubmit} onCancel={() => onOpenChange(false)} />
        )}
      </DialogContent>
    </Dialog>
  )
}

function CompleteStopForm({ action, stop, pending, error, fieldErrors, onSubmit, onCancel }) {
  const [file, setFile] = useState(null)
  const [fileError, setFileError] = useState(null)
  const [note, setNote] = useState("")
  const label = stopActionLabel(action.type)
  const missingProof = action.requiresProof && !file

  const handleFile = (event) => {
    const selected = event.target.files?.[0] ?? null
    const problem = selected ? checkProof(selected) : null
    setFileError(problem)
    setFile(problem ? null : selected)
    if (problem) event.target.value = ""
  }

  const handleSubmit = (event) => {
    event.preventDefault()
    if (missingProof) {
      setFileError("Anexe o comprovante de entrega (canhoto) para finalizar.")
      return
    }
    onSubmit({ file, note })
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <DialogHeader>
        <DialogTitle>{label}</DialogTitle>
        <DialogDescription>
          {stop?.city ?? "Parada atual"} · prazo <span className="font-mono">{formatTripDate(action.deadline)}</span>
        </DialogDescription>
      </DialogHeader>

      {action.requiresProof && (
        <div className="space-y-1.5">
          <label htmlFor="proof-file" className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Comprovante de entrega (canhoto)</label>
          <label htmlFor="proof-file" className="flex cursor-pointer items-center gap-3 rounded-lg border border-dashed border-slate-300 bg-slate-50 p-3 transition-colors hover:bg-slate-100">
            <FileUp size={18} className="shrink-0 text-blue-600" />
            <span className="min-w-0 flex-1 text-sm">
              {file ? (
                <>
                  <span className="block truncate font-medium text-slate-800">{file.name}</span>
                  <span className="font-mono text-xs text-slate-500">{formatFileSize(file.size)}</span>
                </>
              ) : (
                <span className="text-slate-500">Selecione JPG, PNG ou PDF (até 10 MB)</span>
              )}
            </span>
          </label>
          <input id="proof-file" type="file" accept={PROOF_ACCEPT} onChange={handleFile} disabled={pending} className="sr-only" />
          {(fileError || fieldErrors?.comprovante) && <p className="text-xs font-medium text-rose-600">{fileError || fieldErrors.comprovante}</p>}
        </div>
      )}

      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <label htmlFor="stop-note" className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Observação (opcional)</label>
          <span className="font-mono text-[10px] text-slate-400">{note.length}/{TRIP_NOTE_MAX_LENGTH}</span>
        </div>
        <Textarea id="stop-note" value={note} maxLength={TRIP_NOTE_MAX_LENGTH} onChange={(event) => setNote(event.target.value)} disabled={pending} placeholder="Ex.: recebido por João, doca 3" className="border-slate-200 text-sm" />
        {fieldErrors?.observacao && <p className="text-xs font-medium text-rose-600">{fieldErrors.observacao}</p>}
      </div>

      <FormAlert message={error?.message} />

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel} disabled={pending}>Cancelar</Button>
        <Button type="submit" className="bg-blue-600 text-white hover:bg-blue-700" disabled={pending || missingProof}>
          {pending && <Loader2 className="animate-spin" />}
          {pending ? "Enviando..." : label}
        </Button>
      </DialogFooter>
    </form>
  )
}
