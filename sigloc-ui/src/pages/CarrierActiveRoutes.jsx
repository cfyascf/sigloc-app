import { TripListView } from "@/components/trips/trip-list-view"
import { listCarrierTripHistory, listCarrierTrips } from "@/services/carrier-trip-service"

export default function CarrierActiveRoutes() {
  return (
    <TripListView
      title="Rota em Trânsito"
      heading="Rotas em Trânsito"
      historyHeading="Histórico de Viagens"
      searchPlaceholder="Buscar rota ou embarcador..."
      loadTrips={listCarrierTrips}
      historyLoadTrips={listCarrierTripHistory}
      getDetailPath={(trip) => `/carrier-route-tracking/${trip.id}`}
      getTitle={(trip) => trip.shipperName || "Embarcador não informado"}
    />
  )
}
