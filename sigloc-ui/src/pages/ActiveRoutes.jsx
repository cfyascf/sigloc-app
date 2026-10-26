import { TripListView } from "@/components/trips/trip-list-view"
import { listActiveTrips, listTripHistory } from "@/services/trip-service"

export default function ActiveRoutes() {
  return (
    <TripListView
      title="Rotas Ativas"
      heading="Rotas em Execução"
      historyHeading="Histórico de Rotas"
      searchPlaceholder="Buscar rota ou transportadora..."
      loadTrips={listActiveTrips}
      historyLoadTrips={listTripHistory}
      getDetailPath={(trip) => `/active-route-tracking/${trip.id}`}
      getTitle={(trip) => trip.carrier || "Não informado"}
    />
  )
}
