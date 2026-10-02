using System.Text.Json.Serialization;
namespace Sigloc.Application.DTOs;

public record TripQueryDto(int Page = 1, int PageSize = 20, string? Search = null, string? Status = null, string? Risk = null);
public record PagedTripsDto(
    [property: JsonPropertyName("viagens")] IReadOnlyList<ActiveTripDto> Trips,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalItems")] int TotalItems,
    [property: JsonPropertyName("totalPages")] int TotalPages);
public record ActiveTripDto(
    [property: JsonPropertyName("viagemId")] Guid Id,
    [property: JsonPropertyName("rotaId")] Guid RouteId,
    [property: JsonPropertyName("codigoReferencia")] string Reference,
    [property: JsonIgnore] string Status,
    [property: JsonPropertyName("indicadorRisco")] string Risk,
    [property: JsonIgnore] string Carrier,
    [property: JsonIgnore] string Plate,
    [property: JsonPropertyName("origem")] string? Origin,
    [property: JsonPropertyName("destino")] string? Destination,
    [property: JsonIgnore] double? ProgressPercentage,
    [property: JsonIgnore] DateTimeOffset? NewEta,
    [property: JsonPropertyName("ultimaAtualizacaoCalculada")] DateTimeOffset? LastUpdated,
    [property: JsonIgnore] DateTimeOffset? LastPingAt = null)
{
    [JsonPropertyName("statusGeral")] public string GeneralStatus => Status == "EM_TRANSITO" ? "EM_CURSO" : Status;
    [JsonPropertyName("transportadora")] public TripCarrierDto CarrierInfo => new(Carrier);
    [JsonPropertyName("veiculo")] public TripVehicleDto VehicleInfo => new(Plate);
    [JsonPropertyName("monitoramentoCache")] public TripCacheDto? Cache => ProgressPercentage == null && LastPingAt == null ? null : new(ProgressPercentage, NewEta, LastPingAt);
}
public record TripCarrierDto([property: JsonPropertyName("nomeFantasia")] string TradeName);
public record TripVehicleDto([property: JsonPropertyName("placa")] string Plate);
public record TripCacheDto(
    [property: JsonPropertyName("progressoPercentual")] double? ProgressPercentage,
    [property: JsonPropertyName("ultimoEtaCalculado")] DateTimeOffset? LastCalculatedEta,
    [property: JsonPropertyName("ultimaAtualizacaoPing")] DateTimeOffset? LastPingAt);
public record TripDetailDto(
    [property: JsonIgnore] Guid Id,
    [property: JsonIgnore] Guid RouteId,
    [property: JsonIgnore] string Reference,
    [property: JsonIgnore] string Status,
    [property: JsonIgnore] string Risk,
    [property: JsonIgnore] string Carrier,
    [property: JsonIgnore] string Plate,
    [property: JsonIgnore] string? Driver,
    [property: JsonIgnore] string? DriverPhone,
    [property: JsonIgnore] double? ProgressPercentage,
    [property: JsonIgnore] double TotalDistanceKm,
    [property: JsonIgnore] double? TraveledDistanceKm,
    [property: JsonPropertyName("distanciaRestanteKm")] double? RemainingDistanceKm,
    [property: JsonIgnore] double? UtilizationPercentage,
    [property: JsonIgnore] DateTimeOffset? NewEta,
    [property: JsonPropertyName("prazoProximaParada")] DateTimeOffset? NextStopDeadline,
    [property: JsonIgnore] double? Latitude,
    [property: JsonIgnore] double? Longitude,
    [property: JsonIgnore] DateTimeOffset? LastPositionAt,
    [property: JsonPropertyName("ultimaAtualizacaoCalculada")] DateTimeOffset? LastUpdated,
    [property: JsonPropertyName("isCacheRenovado")] bool IsCacheRenewed,
    [property: JsonPropertyName("cacheDesatualizado")] bool IsStale,
    [property: JsonPropertyName("aviso")] string? Warning,
    [property: JsonPropertyName("paradas")] IReadOnlyList<TripStopDto> Stops,
    [property: JsonPropertyName("eventos")] IReadOnlyList<TripEventDto> Events)
{
    [JsonPropertyName("viagemId")] public Guid TripId => Id;
    [JsonPropertyName("rotaId")] public Guid ConsolidatedRouteId => RouteId;
    [JsonPropertyName("codigoReferencia")] public string ReferenceCode => Reference;
    [JsonPropertyName("statusGeral")] public string GeneralStatus => Status == "EM_TRANSITO" ? "EM_CURSO" : Status;
    [JsonPropertyName("indicadorRisco")] public string RiskIndicator => Risk;
    [JsonPropertyName("motorista")] public TripDriverDto DriverInfo => new(Driver, DriverPhone, Plate, Carrier);
    [JsonPropertyName("saudeOperacao")] public TripOperationalHealthDto OperationalHealth => new(
        ProgressPercentage, TraveledDistanceKm, TotalDistanceKm, UtilizationPercentage, NewEta,
        Risk == "CRITIC" ? "ATRASADO" : "NO_PRAZO", IsCacheRenewed);
    [JsonPropertyName("contextoGeografico")] public TripGeographicContextDto GeographicContext => new(
        Latitude == null || Longitude == null ? null : new TripCoordinateDto(Latitude.Value, Longitude.Value), LastPositionAt);
    [JsonPropertyName("linhaDoTempo")] public IReadOnlyList<TripTimelineNodeDto> Timeline => BuildTimeline(Stops);
    [JsonPropertyName("logEventos")] public IReadOnlyList<TripEventLogDto> EventLog => Events
        .Select(e => new TripEventLogDto(e.OccurredAt.ToString("HH:mm"), e.Description, e.Kind, e.OccurredAt)).ToList();

    private static IReadOnlyList<TripTimelineNodeDto> BuildTimeline(IReadOnlyList<TripStopDto> stops)
    {
        var ordered = stops.OrderBy(s => s.Sequence).ToList();
        var groups = new List<List<TripStopDto>>();
        foreach (var stop in ordered)
        {
            if (groups.Count == 0 || !string.Equals(groups[^1][0].City, stop.City, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(groups[^1][0].State, stop.State, StringComparison.OrdinalIgnoreCase))
                groups.Add(new List<TripStopDto>());
            groups[^1].Add(stop);
        }

        return groups.Select((group, index) => new TripTimelineNodeDto(
            $"{group[0].City}, {group[0].State}",
            index == 0 ? "Origem" : index == groups.Count - 1 ? "Destino final" : "Parada intermediária",
            group.SelectMany(s => s.Actions).Select(a => $"{a.Kind} · {a.Product ?? "Carga"}").Distinct().ToList(),
            group.All(s => s.Status == "CONCLUIDA") ? "CONCLUIDO" : group.Any(s => s.Status == "EM_TRANSITO") ? "EM_TRANSITO" : "PENDENTE",
            group.Select(s => s.CompletedAt).FirstOrDefault(x => x != null),
            group.SelectMany(s => s.Actions).Where(a => a.CompletedAt == null).Select(a => (DateTimeOffset?)a.Deadline).OrderBy(x => x).FirstOrDefault())).ToList();
    }
}
public record TripDriverDto([property: JsonPropertyName("nome")] string? Nome, [property: JsonPropertyName("telefone")] string? Telefone, [property: JsonPropertyName("placa")] string Placa, [property: JsonPropertyName("transportadora")] string Transportadora);
public record TripOperationalHealthDto([property: JsonPropertyName("progressoPercentual")] double? ProgressoPercentual, [property: JsonPropertyName("distanciaPercorridaKm")] double? DistanciaPercorridaKm, [property: JsonPropertyName("distanciaTotalKm")] double DistanciaTotalKm, [property: JsonPropertyName("utilizacaoCargaPercentual")] double? UtilizacaoCargaPercentual, [property: JsonPropertyName("novoEta")] DateTimeOffset? NovoEta, [property: JsonPropertyName("statusSla")] string StatusSla, [property: JsonPropertyName("isCacheRenovado")] bool IsCacheRenovado);
public record TripCoordinateDto([property: JsonPropertyName("lat")] double Lat, [property: JsonPropertyName("lng")] double Lng);
public record TripGeographicContextDto([property: JsonPropertyName("ultimaCoordenada")] TripCoordinateDto? UltimaCoordenada, [property: JsonPropertyName("ultimoPing")] DateTimeOffset? UltimoPing);
public record TripTimelineNodeDto([property: JsonPropertyName("cidade")] string Cidade, [property: JsonPropertyName("tipoNode")] string TipoNode, [property: JsonPropertyName("acoes")] IReadOnlyList<string> Acoes, [property: JsonPropertyName("status")] string Status, [property: JsonPropertyName("dataHoraRealizada")] DateTimeOffset? DataHoraRealizada, [property: JsonPropertyName("deadlineSla")] DateTimeOffset? DeadlineSla);
public record TripEventLogDto([property: JsonPropertyName("hora")] string Hora, [property: JsonPropertyName("descricao")] string Descricao, [property: JsonPropertyName("tipo")] string Tipo, [property: JsonPropertyName("ocorridoEm")] DateTimeOffset OcorridoEm);
public record TripStopDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("ordem")] int Sequence,
    [property: JsonPropertyName("cidade")] string City,
    [property: JsonPropertyName("estado")] string State,
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("concluidaEm")] DateTimeOffset? CompletedAt,
    [property: JsonPropertyName("fonteConclusao")] string? CompletionSource,
    [property: JsonPropertyName("acoes")] IReadOnlyList<TripActionDto> Actions);
public record TripActionDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("trechoId")] Guid SegmentId,
    [property: JsonPropertyName("produto")] string? Product,
    [property: JsonPropertyName("tipo")] string Kind,
    [property: JsonPropertyName("prazo")] DateTimeOffset Deadline,
    [property: JsonPropertyName("concluidaEm")] DateTimeOffset? CompletedAt,
    [property: JsonPropertyName("fonteConclusao")] string? CompletionSource);
public record TripEventDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("tipo")] string Kind,
    [property: JsonPropertyName("descricao")] string Description,
    [property: JsonPropertyName("ocorridoEm")] DateTimeOffset OccurredAt);
