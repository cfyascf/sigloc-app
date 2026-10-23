import { useState } from "react"
import { Loader2 } from "lucide-react"

import { FormAlert } from "@/components/auth/FormAlert"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { TRIP_NOTE_MAX_LENGTH, TRIP_OCCURRENCE_TYPES } from "@/constants/trip-occurrences"
import { tripStatusLabel, tripStatusStyle } from "@/lib/trip-formatters"

/** Reports an occurrence on the trip. `onSubmit({ type, note })` runs the request; `error`/`fieldErrors` come from it. */
export function ReportOccurrenceDialog({ open, onOpenChange, pending, error, fieldErrors, onSubmit }) {
  return (
    <Dialog open={open} onOpenChange={(next) => !pending && onOpenChange(next)}>
      <DialogContent className="sm:max-w-[520px]">
        <ReportOccurrenceForm pending={pending} error={error} fieldErrors={fieldErrors} onSubmit={onSubmit} onCancel={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}

function ReportOccurrenceForm({ pending, error, fieldErrors, onSubmit, onCancel }) {
  const [type, setType] = useState("")
  const [note, setNote] = useState("")
  const selected = TRIP_OCCURRENCE_TYPES.find((option) => option.value === type)

  const handleSubmit = (event) => {
    event.preventDefault()
    if (type) onSubmit({ type, note })
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <DialogHeader>
        <DialogTitle>Reportar Ocorrência</DialogTitle>
        <DialogDescription>O embarcador vê a ocorrência e o risco da viagem é atualizado imediatamente.</DialogDescription>
      </DialogHeader>

      <div className="space-y-1.5">
        <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Tipo de ocorrência</p>
        <Select value={type} onValueChange={setType} disabled={pending}>
          <SelectTrigger aria-label="Tipo de ocorrência" className="h-10 w-full border-slate-200 text-sm">
            <SelectValue placeholder="Selecione o tipo" />
          </SelectTrigger>
          <SelectContent>
            {TRIP_OCCURRENCE_TYPES.map((option) => (
              <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        {selected && (
          <p className="flex items-center gap-2 text-xs text-slate-500">
            Severidade:
            <span className={`rounded px-1.5 py-0.5 text-[10px] font-bold uppercase ${tripStatusStyle(selected.severity)}`}>{tripStatusLabel(selected.severity)}</span>
          </p>
        )}
        {fieldErrors?.tipo && <p className="text-xs font-medium text-rose-600">{fieldErrors.tipo}</p>}
      </div>

      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <label htmlFor="occurrence-note" className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Observação (opcional)</label>
          <span className="font-mono text-[10px] text-slate-400">{note.length}/{TRIP_NOTE_MAX_LENGTH}</span>
        </div>
        <Textarea id="occurrence-note" value={note} maxLength={TRIP_NOTE_MAX_LENGTH} onChange={(event) => setNote(event.target.value)} disabled={pending} placeholder="Descreva o que aconteceu" className="border-slate-200 text-sm" />
        {fieldErrors?.observacao && <p className="text-xs font-medium text-rose-600">{fieldErrors.observacao}</p>}
      </div>

      <FormAlert message={error?.message} />

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel} disabled={pending}>Cancelar</Button>
        <Button type="submit" className="bg-blue-600 text-white hover:bg-blue-700" disabled={pending || !type}>
          {pending && <Loader2 className="animate-spin" />}
          {pending ? "Enviando..." : "Reportar Ocorrência"}
        </Button>
      </DialogFooter>
    </form>
  )
}
