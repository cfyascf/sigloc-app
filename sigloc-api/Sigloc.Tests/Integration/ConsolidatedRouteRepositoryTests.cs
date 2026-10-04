using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class ConsolidatedRouteRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    private static ConsolidatedRoute Route(Guid contractorId) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        Status = RouteStatus.Planned,
        TotalWeightKg = 1234,
        TotalVolumeM3 = 12,
        ConsolidatedCeiling = 9000,
    };

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var route = Route(_contractorId);

        var ctx = _db.CreateContext();
        await new ConsolidatedRouteRepository(ctx).AddAsync(route);

        (await _db.CreateContext().ConsolidatedRoutes.FindAsync(route.Id)).Should().BeNull();

        await ctx.SaveChangesAsync();

        (await _db.CreateContext().ConsolidatedRoutes.FindAsync(route.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTrackedByIdAsync_returns_tracked_entity()
    {
        var route = Route(_contractorId);
        var ctx = _db.CreateContext();
        ctx.ConsolidatedRoutes.Add(route);
        await ctx.SaveChangesAsync();

        var repo = new ConsolidatedRouteRepository(_db.CreateContext());
        var found = await repo.GetTrackedByIdAsync(route.Id);

        found.Should().NotBeNull();
        found!.TotalWeightKg.Should().Be(1234);
        found.ConsolidatedCeiling.Should().Be(9000);
    }

    [Fact]
    public async Task GetTrackedByIdAsync_returns_null_for_unknown_id()
    {
        var repo = new ConsolidatedRouteRepository(_db.CreateContext());
        (await repo.GetTrackedByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetTrackedByIdAsync_tracked_update_persists_on_save()
    {
        var route = Route(_contractorId);
        var seedCtx = _db.CreateContext();
        seedCtx.ConsolidatedRoutes.Add(route);
        await seedCtx.SaveChangesAsync();

        var updCtx = _db.CreateContext();
        var repo = new ConsolidatedRouteRepository(updCtx);
        var tracked = await repo.GetTrackedByIdAsync(route.Id);
        tracked!.Status = RouteStatus.InTransit;
        await updCtx.SaveChangesAsync();

        var after = await _db.CreateContext().ConsolidatedRoutes.FindAsync(route.Id);
        after!.Status.Should().Be(RouteStatus.InTransit);
    }
}
