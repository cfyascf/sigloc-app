using System.Globalization;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Entities;

namespace Sigloc.Application.Services;

/// <summary>Type codes, texts and in-app links of every notification the platform sends.</summary>
public static class NotificationCatalog
{
    // Carrier notifications.
    public const string AuctionOpenedType = "LEILAO_ABERTO";
    public const string BidOutbidType = "LANCE_SUPERADO";
    public const string AuctionWonType = "LEILAO_VENCIDO";
    public const string AuctionLostType = "LEILAO_ENCERRADO";
    public const string AuctionCancelledType = "LEILAO_CANCELADO";
    // Contractor notifications.
    public const string BidReceivedType = "NOVO_LANCE";
    public const string PartnerJoinedType = "PARCEIRO_CONECTADO";
    public const string PickupConfirmedType = "COLETA_CONFIRMADA";
    public const string DeliveryConfirmedType = "ENTREGA_CONFIRMADA";
    public const string TripDeliveredType = "VIAGEM_ENTREGUE";
    public const string OccurrenceType = "OCORRENCIA";
    public const string TripAtRiskType = "VIAGEM_EM_RISCO";
    // Either side.
    public const string PartnershipEndedType = "PARCERIA_ENCERRADA";

    private const string PartnerNetworkLink = "/partner-network";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>"Origin → final destination" of a route, from its segments in itinerary order.</summary>
    public static string RouteLabel(IEnumerable<RouteSegment> segments)
    {
        var ordered = segments.OrderBy(s => s.RouteSequence ?? int.MaxValue).ThenBy(s => s.PickupDeadline).ToList();
        return ordered.Count == 0 ? "rota consolidada" : $"{ordered[0].OriginAddress.Trim()} → {ordered[^1].DestinationAddress.Trim()}";
    }

    public static string CompanyName(string companyName, string? tradeName) =>
        string.IsNullOrWhiteSpace(tradeName) ? companyName : tradeName;

    public static NotificationMessage AuctionOpened(Guid carrierId, string contractorName, string route, Guid auctionId) =>
        new(carrierId, AuctionOpenedType, "Novo frete disponível",
            $"{contractorName} abriu um leilão para {route}.", $"/bid-analysis/{auctionId}");

    public static NotificationMessage BidOutbid(Guid carrierId, string route, Guid auctionId) =>
        new(carrierId, BidOutbidType, "Seu lance foi superado",
            $"Outra transportadora ofereceu um valor menor no leilão {route}.", $"/bid-analysis/{auctionId}");

    public static NotificationMessage AuctionWon(Guid carrierId, string route, Guid tripId) =>
        new(carrierId, AuctionWonType, "Você venceu o leilão",
            $"Seu lance para {route} foi escolhido. A viagem {TripDetailMapper.Reference(tripId)} aguarda a coleta.",
            $"/carrier-route-tracking/{tripId}");

    public static NotificationMessage AuctionLost(Guid carrierId, string route) =>
        new(carrierId, AuctionLostType, "Leilão encerrado",
            $"O leilão {route} foi fechado com outra transportadora.", "/freights-offers-overview");

    public static NotificationMessage AuctionCancelled(Guid carrierId, string route) =>
        new(carrierId, AuctionCancelledType, "Leilão cancelado",
            $"O embarcador cancelou o leilão {route}. Seu lance foi descartado.", "/freights-offers-overview");

    public static NotificationMessage BidReceived(Guid contractorId, string carrierName, string route, decimal totalValue, bool updated, Guid auctionId) =>
        new(contractorId, BidReceivedType, updated ? "Lance atualizado" : "Novo lance recebido",
            $"{carrierName} {(updated ? "atualizou o lance para" : "ofereceu")} {totalValue.ToString("C", PtBr)} no leilão {route}.",
            $"/auction-bids/{auctionId}");

    public static NotificationMessage PartnerJoined(Guid contractorId, string carrierName) =>
        new(contractorId, PartnerJoinedType, "Nova transportadora parceira",
            $"{carrierName} aceitou seu convite e já pode dar lances nos seus leilões.", PartnerNetworkLink);

    public static NotificationMessage PartnershipEnded(Guid recipientId, string endedBy) =>
        new(recipientId, PartnershipEndedType, "Parceria encerrada",
            $"{endedBy} encerrou a parceria com sua empresa.", PartnerNetworkLink);

    public static NotificationMessage StopConfirmed(Guid contractorId, Guid tripId, string city, bool delivery, bool tripDelivered)
    {
        var reference = TripDetailMapper.Reference(tripId);
        var link = $"/active-route-tracking/{tripId}";
        if (tripDelivered)
            return new(contractorId, TripDeliveredType, $"Viagem {reference} entregue",
                $"A última entrega ({city}) foi confirmada com comprovante. A viagem está concluída.", link);
        return delivery
            ? new(contractorId, DeliveryConfirmedType, $"Entrega confirmada · {reference}",
                $"O motorista confirmou a entrega em {city} e anexou o comprovante.", link)
            : new(contractorId, PickupConfirmedType, $"Coleta confirmada · {reference}",
                $"O motorista confirmou a coleta em {city}.", link);
    }

    public static NotificationMessage OccurrenceReported(Guid contractorId, Guid tripId, string occurrence, bool critical) =>
        new(contractorId, OccurrenceType, $"Ocorrência na viagem {TripDetailMapper.Reference(tripId)}",
            $"O motorista reportou: {occurrence}." + (critical ? " A ocorrência é crítica e ameaça o prazo." : ""),
            $"/active-route-tracking/{tripId}");

    public static NotificationMessage TripAtRisk(Guid contractorId, Guid tripId) =>
        new(contractorId, TripAtRiskType, $"Viagem {TripDetailMapper.Reference(tripId)} em risco de atraso",
            "A previsão de chegada calculada excede o prazo da próxima parada.", $"/active-route-tracking/{tripId}");
}
