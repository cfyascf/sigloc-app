import { TripListView } from "@/components/trips/trip-list-view"
import { listActiveTrips } from "@/services/trip-service"

export default function ActiveRoutes() {
  return (
    <TripListView
      title="Rotas Ativas"
      heading="Rotas em Execução"
      searchPlaceholder="Buscar rota ou transportadora..."
      loadTrips={listActiveTrips}
      getDetailPath={(trip) => `/active-route-tracking/${trip.id}`}
      getTitle={(trip) => trip.carrier || "Não informado"}
    />
  )
}
