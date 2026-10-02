using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Entities;
using Sigloc.Infrastructure;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Monitoring;

namespace Sigloc.Tests;

public class MonitoringSchemaTests
{
    [Fact]
    public void Offline_model_has_unique_snapshot_order_observation_and_event_identities()
    {
        using var db = new SiglocDbContext(new DbContextOptionsBuilder<SiglocDbContext>()
            .UseNpgsql("Host=localhost;Database=unused_test;Username=unused").Options);
        var model = db.Model;
        Assert.Contains(model.FindEntityType(typeof(TripMonitoring))!.GetIndexes(), i => i.IsUnique && i.Properties.Single().Name == "TripId");
        Assert.Contains(model.FindEntityType(typeof(TripStop))!.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "TripId", "Sequence" }));
        Assert.Contains(model.FindEntityType(typeof(TripTelemetry))!.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "TripId", "DeviceId", "ObservationId" }));
        Assert.Contains(model.FindEntityType(typeof(TripMonitoringEvent))!.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "TripId", "RefreshId", "Kind" }));
        Assert.True(model.FindEntityType(typeof(TripMonitoring))!.FindProperty("LastCalculatedEta")!.IsNullable);
        Assert.True(model.FindEntityType(typeof(TripMonitoring))!.FindProperty("LastSuccessfulCalculationAt")!.IsNullable);
        var migrations = db.Database.GetMigrations().ToList();
        var monitoringMigration = migrations.Single(m => m.EndsWith("AddOnDemandTripMonitoring", StringComparison.Ordinal));
        var priorMigration = migrations[migrations.IndexOf(monitoringMigration) - 1];
        var sql = db.GetService<IMigrator>().GenerateScript(priorMigration, monitoringMigration);
        Assert.Contains("CREATE TABLE \"TripStop\"", sql);
        Assert.Contains("CREATE TABLE \"TripTelemetry\"", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("DELETE FROM", sql);
        Assert.DoesNotContain("UPDATE \"TripMonitoring\"", sql);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Provider_selection_requires_explicit_mock_configuration(bool mock)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused_test;Username=unused",
            ["Monitoring:MockMode"] = mock.ToString()
        }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<ITripTrackingProvider>();
        var router = scope.ServiceProvider.GetRequiredService<ITripRoutingProvider>();
        Assert.Equal(mock ? typeof(MockTripTrackingProvider) : typeof(TraccarTripTrackingProvider), tracker.GetType());
        Assert.Equal(mock ? typeof(MockTripRoutingProvider) : typeof(OpenRouteServiceTripRoutingProvider), router.GetType());
    }
}
