using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Repositories;

namespace Sigloc.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SIGLOC_TEST_CONNECTION")))
            Skip = "Set SIGLOC_TEST_CONNECTION explicitly to an isolated PostgreSQL test database. No services are started.";
    }
}

public class PostgresMonitoringTests
{
    [PostgresFact]
    public async Task Migrations_preserve_legacy_snapshot_and_enforce_unique_monitoring()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var h = await fixture.SeedAsync();
        await using var db = fixture.Factory.CreateDbContext();
        db.TripMonitorings.Add(new TripMonitoring { Id = Guid.NewGuid(), TripId = h.State.Trip.Id,
            LastCalculatedEta = TripMonitoringServiceTests.Now.AddHours(2), LastPingAt = TripMonitoringServiceTests.Now });
        await db.SaveChangesAsync();
        var migrations = db.Database.GetMigrations().ToList();
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();
        var legacy = await db.TripMonitorings.SingleAsync();
        Assert.Null(legacy.LastSuccessfulCalculationAt);
        Assert.Equal(TripMonitoringServiceTests.Now.AddHours(2), legacy.LastCalculatedEta);
        db.TripMonitorings.Add(new TripMonitoring { Id = Guid.NewGuid(), TripId = h.State.Trip.Id, LastPingAt = TripMonitoringServiceTests.Now });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task Tenant_scoped_left_join_paging_has_no_monitoring_side_effects()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var h = await fixture.SeedAsync();
        var store = new TripMonitoringStore(fixture.Factory);
        var result = await store.SearchAsync(h.CompanyId, new TripQueryDto(1, 1), default);
        Assert.Single(result.Trips); Assert.Equal(1, result.TotalItems);
        Assert.Null(result.Trips[0].ProgressPercentage);
        Assert.Empty((await store.SearchAsync(Guid.NewGuid(), new TripQueryDto(), default)).Trips);
        Assert.Empty((await store.SearchAsync(h.CompanyId, new TripQueryDto(2, 1), default)).Trips);
        Assert.Single((await store.SearchAsync(h.CompanyId, new TripQueryDto(Search: "ABC1D23"), default)).Trips);
        Assert.Null(await store.ReadAsync(Guid.NewGuid(), h.State.Trip.Id, default));
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Empty(await db.TripMonitorings.ToListAsync()); Assert.Empty(await db.TripTelemetries.ToListAsync());
    }

    [PostgresFact]
    public async Task Independent_instances_serialize_refresh_and_commit_one_observation()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var h = await fixture.SeedAsync();
        var first = new TripMonitoringService(new TripMonitoringStore(fixture.Factory), h.Tracking, h.Routing, h.Clock, new());
        var second = new TripMonitoringService(new TripMonitoringStore(fixture.Factory), h.Tracking, h.Routing, h.Clock, new());
        var results = await Task.WhenAll(first.GetDetailAsync(h.CompanyId, h.State.Trip.Id), second.GetDetailAsync(h.CompanyId, h.State.Trip.Id));
        Assert.Single(results.Where(x => x.IsCacheRenewed));
        Assert.Equal(1, h.Tracking.Calls); Assert.Equal(1, h.Routing.Calls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Single(await db.TripTelemetries.ToListAsync()); Assert.Single(await db.TripMonitorings.ToListAsync());
    }

    [PostgresFact]
    public async Task Failed_provider_rolls_back_and_releases_row_lock_for_next_request()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var h = await fixture.SeedAsync(); h.Routing.Fail = true;
        h.Tracking.Fix = h.Tracking.Fix with { Latitude = 0, Longitude = 0 };
        var service = new TripMonitoringService(new TripMonitoringStore(fixture.Factory), h.Tracking, h.Routing, h.Clock, new());
        await Assert.ThrowsAsync<Sigloc.Application.Exceptions.TrackingUnavailableException>(() => service.GetDetailAsync(h.CompanyId, h.State.Trip.Id));
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.Empty(await db.TripMonitorings.ToListAsync());
            Assert.All(await db.TripStops.ToListAsync(), s => Assert.False(s.IsCompleted));
            Assert.Equal(TripStatus.AwaitingPickup, (await db.Trips.SingleAsync()).Status);
        }
        h.Routing.Fail = false;
        Assert.True((await service.GetDetailAsync(h.CompanyId, h.State.Trip.Id)).IsCacheRenewed);
    }

    private sealed class DatabaseFixture(string connectionString, string schema) : IAsyncDisposable
    {
        public Factory Factory { get; } = new(new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema }.ConnectionString);
        public static async Task<DatabaseFixture> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("SIGLOC_TEST_CONNECTION")
                ?? throw new InvalidOperationException("Explicit test connection is required.");
            var schema = "monitoring_test_" + Guid.NewGuid().ToString("N");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
            await command.ExecuteNonQueryAsync();
            var fixture = new DatabaseFixture(connectionString, schema);
            try
            {
                await using var db = fixture.Factory.CreateDbContext();
                await db.Database.MigrateAsync();
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }
        public async Task<TripMonitoringServiceTests.Harness> SeedAsync()
        {
            var h = new TripMonitoringServiceTests.Harness();
            var s = h.State;
            s.Carrier.Id = s.Vehicle.TransportadoraId; s.Trip.CarrierId = s.Carrier.Id;
            var auction = new Auction { Id = Guid.NewGuid(), RouteId = s.Route.Id,
                OpenedAt = TripMonitoringServiceTests.Now.AddHours(-2), ExpiresAt = TripMonitoringServiceTests.Now.AddHours(-1), Status = AuctionStatus.Closed };
            var bid = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = s.Carrier.Id,
                VehicleId = s.Vehicle.Id, SubmittedAt = TripMonitoringServiceTests.Now.AddHours(-1), Status = BidStatus.Winner };
            s.Trip.AuctionId = auction.Id; s.Trip.BidId = bid.Id;
            foreach (var segment in s.Segments) segment.ContractorId = h.CompanyId;
            await using var db = Factory.CreateDbContext();
            db.Add(new Contractor { Id = h.CompanyId, Cnpj = "12345678000199", CompanyName = "Test contractor" });
            db.Add(s.Carrier); db.Add(s.Vehicle); db.Add(s.Route); db.AddRange(s.Segments); db.Add(auction); db.Add(bid); db.Add(s.Trip);
            await db.SaveChangesAsync();
            return h;
        }
        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
    private sealed class Factory(string connectionString) : IDbContextFactory<SiglocDbContext>
    {
        public SiglocDbContext CreateDbContext() => new(new DbContextOptionsBuilder<SiglocDbContext>()
            .UseNpgsql(connectionString, options => options.EnableRetryOnFailure()).Options);
        public Task<SiglocDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
