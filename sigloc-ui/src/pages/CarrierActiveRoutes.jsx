import { TripListView } from "@/components/trips/trip-list-view"
import { listCarrierTrips } from "@/services/carrier-trip-service"

export default function CarrierActiveRoutes() {
  return (
    <TripListView
      title="Rota em Trânsito"
      heading="Rotas em Trânsito"
      searchPlaceholder="Buscar rota ou embarcador..."
      loadTrips={listCarrierTrips}
      getDetailPath={(trip) => `/carrier-route-tracking/${trip.id}`}
      getTitle={(trip) => trip.shipperName || "Embarcador não informado"}
    />
  )
}
