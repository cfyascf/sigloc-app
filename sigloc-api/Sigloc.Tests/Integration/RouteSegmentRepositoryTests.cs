using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class RouteSegmentRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    private static RouteSegment Segment(
        Guid contractorId,
        string origin = "Curitiba, PR",
        string destination = "São Paulo, SP",
        SegmentStatus status = SegmentStatus.Available,
        DateTimeOffset? createdAt = null) => new()
        {
            Id = Guid.NewGuid(),
            ContractorId = contractorId,
            OriginAddress = origin,
            DestinationAddress = destination,
            OriginCoordinate = "-49.27,-25.42",
            DestinationCoordinate = "-46.63,-23.55",
            PickupDeadline = DateTimeOffset.UtcNow.AddDays(1),
            DeliveryDeadline = DateTimeOffset.UtcNow.AddDays(2),
            Status = status,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task AddAsync_persists_and_GetByIdAsync_scopes_by_contractor()
    {
        var segment = Segment(_contractorId);
        await new RouteSegmentRepository(_db.CreateContext()).AddAsync(segment);

        var repo = new RouteSegmentRepository(_db.CreateContext());
        var found = await repo.GetByIdAsync(segment.Id, _contractorId);
        var crossTenant = await repo.GetByIdAsync(segment.Id, Guid.NewGuid());

        found.Should().NotBeNull();
        found!.OriginAddress.Should().Be("Curitiba, PR");
        crossTenant.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_includes_items_with_products()
    {
        var product = TestData.Product(_contractorId, sku: "P-INC");
        var segment = Segment(_contractorId);
        segment.Items.Add(new ProductRouteSegment
        {
            Id = Guid.NewGuid(),
            RouteSegmentId = segment.Id,
            ProductId = product.Id,
            Quantity = 3,
        });

        var ctx = _db.CreateContext();
        ctx.Products.Add(product);
        ctx.RouteSegments.Add(segment);
        await ctx.SaveChangesAsync();

        var found = await new RouteSegmentRepository(_db.CreateContext()).GetByIdAsync(segment.Id, _contractorId);

        found!.Items.Should().ContainSingle();
        found.Items[0].Product.Should().NotBeNull();
        found.Items[0].Product!.Sku.Should().Be("P-INC");
    }

    // The DbContext SaveChanges override stamps CreatedAt=utcNow on insert, overwriting
    // any seeded value, so distinct timestamps must be applied with a follow-up update
    // (the override leaves CreatedAt untouched on Modified entities).
    private async Task SeedWithCreatedAtAsync(params (RouteSegment Segment, DateTimeOffset CreatedAt)[] rows)
    {
        var ctx = _db.CreateContext();
        ctx.RouteSegments.AddRange(rows.Select(r => r.Segment));
        await ctx.SaveChangesAsync();
        foreach (var (segment, createdAt) in rows)
        {
            segment.CreatedAt = createdAt;
        }
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAsync_filters_by_origin_destination_status_and_orders_by_created_desc()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedWithCreatedAtAsync(
            (Segment(_contractorId, origin: "Curitiba, PR", destination: "São Paulo, SP", status: SegmentStatus.Available), now.AddMinutes(-30)),
            (Segment(_contractorId, origin: "Curitiba, PR", destination: "Rio de Janeiro, RJ", status: SegmentStatus.Routed), now.AddMinutes(-10)),
            (Segment(_contractorId, origin: "Belo Horizonte, MG", destination: "São Paulo, SP", status: SegmentStatus.Available), now.AddMinutes(-20)),
            (Segment(Guid.NewGuid(), origin: "Curitiba, PR", destination: "São Paulo, SP"), now.AddMinutes(-5)));

        var repo = new RouteSegmentRepository(_db.CreateContext());

        var (allItems, allTotal) = await repo.SearchAsync(_contractorId, null, null, null, 1, 10);
        allTotal.Should().Be(3);
        // Ordered by CreatedAt descending: -10, -20, -30
        allItems.Select(s => s.DestinationAddress).Should().ContainInOrder("Rio de Janeiro, RJ", "São Paulo, SP", "São Paulo, SP");

        var (originItems, originTotal) = await repo.SearchAsync(_contractorId, "curitiba", null, null, 1, 10);
        originTotal.Should().Be(2);
        originItems.Should().OnlyContain(s => s.OriginAddress.Contains("Curitiba"));

        var (destItems, destTotal) = await repo.SearchAsync(_contractorId, null, "são paulo", null, 1, 10);
        destTotal.Should().Be(2);
        destItems.Should().OnlyContain(s => s.DestinationAddress.Contains("São Paulo"));

        var (statusItems, statusTotal) = await repo.SearchAsync(_contractorId, null, null, SegmentStatus.Routed, 1, 10);
        statusTotal.Should().Be(1);
        statusItems.Should().OnlyContain(s => s.Status == SegmentStatus.Routed);
    }

    [Fact]
    public async Task SearchAsync_paginates()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedWithCreatedAtAsync(
            Enumerable.Range(0, 5)
                .Select(i => (Segment(_contractorId, destination: $"Dest{i}"), now.AddMinutes(-i)))
                .ToArray());

        var (page2, total) = await new RouteSegmentRepository(_db.CreateContext())
            .SearchAsync(_contractorId, null, null, null, 2, 2);

        total.Should().Be(5);
        page2.Should().HaveCount(2);
        // Newest first => Dest0, Dest1 | Dest2, Dest3 | Dest4
        page2.Select(s => s.DestinationAddress).Should().ContainInOrder("Dest2", "Dest3");
    }

    [Fact]
    public async Task GetProductsByIdsAsync_empty_input_returns_empty()
    {
        var result = await new RouteSegmentRepository(_db.CreateContext())
            .GetProductsByIdsAsync(_contractorId, Array.Empty<Guid>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsByIdsAsync_filters_by_contractor_and_ids()
    {
        var mine = TestData.Product(_contractorId, sku: "MINE");
        var other = TestData.Product(Guid.NewGuid(), sku: "OTHER");
        var ctx = _db.CreateContext();
        ctx.Products.AddRange(mine, other);
        await ctx.SaveChangesAsync();

        var result = await new RouteSegmentRepository(_db.CreateContext())
            .GetProductsByIdsAsync(_contractorId, new[] { mine.Id, other.Id });

        result.Should().ContainSingle();
        result[0].Sku.Should().Be("MINE");
    }

    [Fact]
    public async Task GetByIdsAsync_empty_input_returns_empty()
    {
        var result = await new RouteSegmentRepository(_db.CreateContext())
            .GetByIdsAsync(_contractorId, Array.Empty<Guid>(), tracked: false);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_filters_by_contractor_and_ids()
    {
        var a = Segment(_contractorId, destination: "A");
        var b = Segment(_contractorId, destination: "B");
        var foreign = Segment(Guid.NewGuid(), destination: "F");
        var ctx = _db.CreateContext();
        ctx.RouteSegments.AddRange(a, b, foreign);
        await ctx.SaveChangesAsync();

        var result = await new RouteSegmentRepository(_db.CreateContext())
            .GetByIdsAsync(_contractorId, new[] { a.Id, b.Id, foreign.Id }, tracked: false);

        result.Should().HaveCount(2);
        result.Select(s => s.DestinationAddress).Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public async Task UpdateAsync_replaces_items_and_persists_scalar_changes()
    {
        var p1 = TestData.Product(_contractorId, sku: "P1");
        var p2 = TestData.Product(_contractorId, sku: "P2");
        var segment = Segment(_contractorId);
        segment.Items.Add(new ProductRouteSegment
        {
            Id = Guid.NewGuid(),
            RouteSegmentId = segment.Id,
            ProductId = p1.Id,
            Quantity = 1,
        });

        var seedCtx = _db.CreateContext();
        seedCtx.Products.AddRange(p1, p2);
        seedCtx.RouteSegments.Add(segment);
        await seedCtx.SaveChangesAsync();

        var actCtx = _db.CreateContext();
        var repo = new RouteSegmentRepository(actCtx);
        var tracked = await repo.GetByIdAsync(segment.Id, _contractorId);
        tracked!.DestinationAddress = "Santos, SP";
        var replacements = new[]
        {
            new ProductRouteSegment { ProductId = p2.Id, Quantity = 7 },
        };
        await repo.UpdateAsync(tracked, replacements);

        var reloaded = await new RouteSegmentRepository(_db.CreateContext()).GetByIdAsync(segment.Id, _contractorId);
        reloaded!.DestinationAddress.Should().Be("Santos, SP");
        reloaded.Items.Should().ContainSingle();
        reloaded.Items[0].ProductId.Should().Be(p2.Id);
        reloaded.Items[0].Quantity.Should().Be(7);
    }

    [Fact]
    public async Task DeleteAsync_cascades_item_rows()
    {
        var product = TestData.Product(_contractorId, sku: "DEL");
        var segment = Segment(_contractorId);
        segment.Items.Add(new ProductRouteSegment
        {
            Id = Guid.NewGuid(),
            RouteSegmentId = segment.Id,
            ProductId = product.Id,
            Quantity = 2,
        });

        var seedCtx = _db.CreateContext();
        seedCtx.Products.Add(product);
        seedCtx.RouteSegments.Add(segment);
        await seedCtx.SaveChangesAsync();

        var delCtx = _db.CreateContext();
        var repo = new RouteSegmentRepository(delCtx);
        var toDelete = await repo.GetByIdAsync(segment.Id, _contractorId);
        await repo.DeleteAsync(toDelete!);

        var assertCtx = _db.CreateContext();
        (await new RouteSegmentRepository(assertCtx).GetByIdAsync(segment.Id, _contractorId)).Should().BeNull();
        assertCtx.ProductRouteSegments.Any(i => i.RouteSegmentId == segment.Id).Should().BeFalse();
    }
}
