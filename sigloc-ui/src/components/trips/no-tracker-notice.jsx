import { SatelliteDish } from "lucide-react"
import { Link } from "react-router-dom"

/**
 * Shown on the tracking screens when the trip's vehicle has no Traccar device registered: the GPS
 * position can't be tracked, so the "Localizar Motorista" / "Transmitir Posição GPS" buttons are
 * disabled. The carrier gets a link to register the device; the contractor is told to ask for it.
 */
export function NoTrackerNotice({ plate, isCarrier = false, className = "" }) {
  return (
    <div role="status" className={`flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900 ${className}`}>
      <SatelliteDish size={18} className="mt-0.5 shrink-0 text-amber-600" />
      <div className="space-y-1">
        <p className="font-semibold">Rastreamento GPS indisponível</p>
        <p className="text-xs leading-relaxed">
          O veículo{plate ? ` ${plate}` : ""} não tem um dispositivo Traccar cadastrado. Por isso não é possível rastrear a
          localização por GPS nem calcular o ETA desta viagem.
        </p>
        {isCarrier ? (
          <p className="text-xs leading-relaxed">
            Cadastre o ID do dispositivo em{" "}
            <Link to="/fleet-management" className="font-semibold underline">Gestão de Frota</Link> para ativar o rastreamento.
          </p>
        ) : (
          <p className="text-xs leading-relaxed">Peça à transportadora para cadastrar o rastreador do veículo.</p>
        )}
      </div>
    </div>
  )
}

/** The API error code returned when a GPS refresh is asked for a vehicle without a tracker. */
export const NO_TRACKER_ERROR = "VEICULO_SEM_RASTREADOR"
