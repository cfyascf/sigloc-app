using System.Text.Json;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using static Sigloc.Tests.TripMonitoringServiceTests;

namespace Sigloc.Tests;

public class TripOccurrenceRiskTests
{
    [Theory]
    [InlineData(null, new string[0], "NAO_MONITORADO")]
    [InlineData(null, new[] { "WARNING" }, "WARNING")]
    [InlineData("NORMAL", new[] { "WARNING" }, "WARNING")]
    [InlineData("CRITIC", new[] { "WARNING" }, "CRITIC")]
    [InlineData("NORMAL", new[] { "WARNING", "CRITIC" }, "CRITIC")]
    [InlineData("NORMAL", new string[0], "NORMAL")]
    public void Effective_risk_is_the_highest_of_eta_and_open_occurrences(string? eta, string[] severities, string expected)
    {
        var occurrences = severities.Select(s => new TripOccurrence { Severity = s }).ToList();
        Assert.Equal(expected, TripRiskEvaluator.Effective(eta, occurrences));
    }

    [Fact]
    public void Resolved_occurrences_do_not_raise_risk()
    {
        var resolved = new TripOccurrence { Severity = "CRITIC", ResolvedAt = Now };
        Assert.Equal("NORMAL", TripRiskEvaluator.Effective("NORMAL", [resolved]));
    }

    [Theory]
    [InlineData(TripOccurrenceType.MechanicalFailure, "CRITIC")]
    [InlineData(TripOccurrenceType.SlaRisk, "CRITIC")]
    [InlineData(TripOccurrenceType.HeavyTraffic, "WARNING")]
    [InlineData(TripOccurrenceType.MissingDocumentation, "WARNING")]
    public void Severity_follows_occurrence_type(TripOccurrenceType type, string severity) =>
        Assert.Equal(severity, TripRiskEvaluator.SeverityOf(type));

    [Fact]
    public async Task Open_critical_occurrence_marks_trip_late_without_monitoring()
    {
        var h = new Harness();
        h.State.Occurrences.Add(Occurrence(h, "CRITIC"));
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal("CRITIC", result.Risk);
        Assert.Equal("ATRASADO", result.Status);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        Assert.Equal("ATRASADO", json.RootElement.GetProperty("saudeOperacao").GetProperty("statusSla").GetString());
    }

    [Fact]
    public async Task Open_warning_occurrence_is_reported_as_attention()
    {
        var h = new Harness(); h.Cached(1);
        h.State.Occurrences.Add(Occurrence(h, "WARNING"));
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal("WARNING", result.Risk);
        Assert.Equal("AGUARDANDO_COLETA", result.Status);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        Assert.Equal("ATENCAO", json.RootElement.GetProperty("saudeOperacao").GetProperty("statusSla").GetString());
    }

    [Fact]
    public async Task Next_stop_deadline_is_derived_from_itinerary_without_snapshot()
    {
        var h = new Harness();
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal(h.State.Trip.Stops[0].Actions[0].Deadline, result.NextStopDeadline);
    }

    [Fact]
    public async Task Refresh_keeps_occurrence_risk_over_on_time_eta_and_logs_one_sla_event()
    {
        var h = new Harness();
        h.State.Occurrences.Add(Occurrence(h, "CRITIC"));
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id, refresh: true);
        Assert.Equal("NORMAL", h.Store.State!.Snapshot!.Risk);
        Assert.Equal("CRITIC", result.Risk);
        Assert.Equal("ATRASADO", result.Status);
        // Effective risk was already CRITIC before the refresh, so no SLA transition is logged.
        Assert.DoesNotContain(h.Store.State.Events, e => e.Kind == "SLA");
    }

    [Fact]
    public async Task Geofence_completion_resolves_occurrences_blocked_on_that_stop()
    {
        var h = new Harness();
        var blocking = Occurrence(h, "CRITIC"); blocking.BlockingStopId = h.State.Trip.Stops[0].Id;
        var later = Occurrence(h, "WARNING"); later.BlockingStopId = h.State.Trip.Stops[1].Id;
        h.State.Occurrences.AddRange([blocking, later]);
        h.Tracking.Fix = h.Tracking.Fix with { Latitude = 0, Longitude = 0 };

        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id, refresh: true);

        var stored = h.Store.State!.Occurrences;
        Assert.Equal(Now, stored.Single(o => o.Id == blocking.Id).ResolvedAt);
        Assert.Equal("GEOFENCE_INFERRED", stored.Single(o => o.Id == blocking.Id).ResolutionSource);
        Assert.Null(stored.Single(o => o.Id == later.Id).ResolvedAt);
        Assert.Equal("WARNING", result.Risk);
        Assert.Single(h.Store.State.Events, e => e.Kind == "OCORRENCIA_ENCERRADA");
        Assert.Single(h.Store.State.Events, e => e.Kind == "SLA");
    }

    [Fact]
    public async Task Warning_is_an_accepted_list_filter()
    {
        var h = new Harness();
        await h.Service.SearchAsync(h.CompanyId, new TripQueryDto(Risk: "warning"));
        Assert.Equal("WARNING", h.Store.LastQuery!.Risk);
        await Assert.ThrowsAsync<ValidationException>(() => h.Service.SearchAsync(h.CompanyId, new TripQueryDto(Risk: "ATENCAO")));
    }

    private static TripOccurrence Occurrence(Harness h, string severity) => new()
    {
        Id = Guid.NewGuid(), TripId = h.State.Trip.Id, Severity = severity,
        Type = severity == "CRITIC" ? TripOccurrenceType.MechanicalFailure : TripOccurrenceType.HeavyTraffic,
        ReportedAt = Now.AddMinutes(-5), ReportedByUserId = Guid.NewGuid()
    };
}
