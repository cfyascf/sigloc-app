using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class VehicleRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _carrierId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_persists_and_GetByIdAsync_scopes_by_carrier()
    {
        var vehicle = TestData.Vehicle(_carrierId, plate: "ABC1D23");
        await new VehicleRepository(_db.CreateContext()).AddAsync(vehicle);

        var repo = new VehicleRepository(_db.CreateContext());
        var found = await repo.GetByIdAsync(vehicle.Id, _carrierId);
        var crossTenant = await repo.GetByIdAsync(vehicle.Id, Guid.NewGuid());

        found.Should().NotBeNull();
        found!.Plate.Should().Be("ABC1D23");
        crossTenant.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_returns_only_vehicles_for_carrier()
    {
        var otherCarrier = Guid.NewGuid();
        var ctx = _db.CreateContext();
        ctx.Vehicles.AddRange(
            TestData.Vehicle(_carrierId, plate: "AAA1A11"),
            TestData.Vehicle(_carrierId, plate: "BBB2B22"),
            TestData.Vehicle(otherCarrier, plate: "CCC3C33"));
        await ctx.SaveChangesAsync();

        var all = await new VehicleRepository(_db.CreateContext()).GetAllAsync(_carrierId);

        all.Should().HaveCount(2);
        all.Should().OnlyContain(v => v.TransportadoraId == _carrierId);
    }

    [Fact]
    public async Task Reads_still_work_when_a_stored_row_has_invalid_capacity()
    {
        var vehicle = TestData.Vehicle(_carrierId, plate: "BAD1B11");
        await new VehicleRepository(_db.CreateContext()).AddAsync(vehicle);

        // Simulate a row written before validation existed (or by hand), bypassing the entity.
        await _db.CreateContext().Vehicles
            .Where(v => v.Id == vehicle.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.CapacityWeight, -500m));

        var repo = new VehicleRepository(_db.CreateContext());
        var all = await repo.GetAllAsync(_carrierId);
        var found = await repo.GetByIdAsync(vehicle.Id, _carrierId);

        all.Should().ContainSingle(v => v.Id == vehicle.Id);
        found!.CapacityWeight.Should().Be(-500m);
    }

    [Fact]
    public async Task Active_trips_mark_vehicles_busy_for_free_counts_and_active_trip_ids()
    {
        var contractorId = Guid.NewGuid();
        var onTrip = TestData.Vehicle(_carrierId, plate: "TRP1A11"); onTrip.Id = Guid.NewGuid();
        var delivered = TestData.Vehicle(_carrierId, plate: "DLV1A11"); delivered.Id = Guid.NewGuid();
        var idle = TestData.Vehicle(_carrierId, plate: "IDL1A11"); idle.Id = Guid.NewGuid();

        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(contractorId));
        ctx.Carriers.Add(TestData.Carrier(_carrierId));
        ctx.Vehicles.AddRange(onTrip, delivered, idle);
        // One trip per auction (unique), so each trip gets its own route and auction.
        foreach (var (vehicleId, status) in new[] { (onTrip.Id, TripStatus.InTransit), (delivered.Id, TripStatus.Delivered) })
        {
            var route = new ConsolidatedRoute { Id = Guid.NewGuid(), ContractorId = contractorId };
            var auction = new Auction { Id = Guid.NewGuid(), RouteId = route.Id, OpenedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), Status = AuctionStatus.Closed };
            ctx.ConsolidatedRoutes.Add(route);
            ctx.Auctions.Add(auction);
            ctx.Trips.Add(new Trip
            {
                Id = Guid.NewGuid(), RouteId = route.Id, AuctionId = auction.Id, CarrierId = _carrierId,
                VehicleId = vehicleId, BidId = Guid.NewGuid(), AgreedValue = 1000, Status = status,
            });
        }
        await ctx.SaveChangesAsync();

        var repo = new VehicleRepository(_db.CreateContext());
        var free = await repo.CountFreeByCarrierIdsAsync(new[] { _carrierId });
        var activeIds = await repo.GetIdsOnActiveTripsAsync(_carrierId);

        free[_carrierId].Should().Be(2);
        activeIds.Should().BeEquivalentTo(new[] { onTrip.Id });
    }

    [Fact]
    public async Task ExistsByPlateAsync_matches_exact_plate()
    {
        var vehicle = TestData.Vehicle(_carrierId, plate: "XYZ9Z99");
        await new VehicleRepository(_db.CreateContext()).AddAsync(vehicle);

        var repo = new VehicleRepository(_db.CreateContext());
        (await repo.ExistsByPlateAsync("XYZ9Z99")).Should().BeTrue();
        (await repo.ExistsByPlateAsync("NOPE000")).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_and_DeleteAsync_persist()
    {
        var vehicle = TestData.Vehicle(_carrierId, plate: "UPD1D11");
        await new VehicleRepository(_db.CreateContext()).AddAsync(vehicle);

        var updating = new VehicleRepository(_db.CreateContext());
        var toUpdate = await updating.GetByIdAsync(vehicle.Id, _carrierId);
        toUpdate!.Update(model: "Scania R", null, null, null, null, null, null, null, driver: "Maria", null, status: OperationalStatus.EM_TRANSITO);
        await updating.UpdateAsync(toUpdate);

        var afterUpdate = await new VehicleRepository(_db.CreateContext()).GetByIdAsync(vehicle.Id, _carrierId);
        afterUpdate!.Model.Should().Be("Scania R");
        afterUpdate.Status.Should().Be(OperationalStatus.EM_TRANSITO);

        var deleting = new VehicleRepository(_db.CreateContext());
        await deleting.DeleteAsync(afterUpdate);

        var afterDelete = await new VehicleRepository(_db.CreateContext()).GetByIdAsync(vehicle.Id, _carrierId);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task CountFreeByCarrierIdsAsync_returns_empty_for_empty_input()
    {
        var result = await new VehicleRepository(_db.CreateContext())
            .CountFreeByCarrierIdsAsync(Array.Empty<Guid>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CountFreeByCarrierIdsAsync_counts_only_free_vehicles_grouped_by_carrier()
    {
        var carrierA = Guid.NewGuid();
        var carrierB = Guid.NewGuid();
        var carrierC = Guid.NewGuid();

        var ctx = _db.CreateContext();
        ctx.Vehicles.AddRange(
            TestData.Vehicle(carrierA, plate: "A1", status: OperationalStatus.LIVRE),
            TestData.Vehicle(carrierA, plate: "A2", status: OperationalStatus.LIVRE),
            TestData.Vehicle(carrierA, plate: "A3", status: OperationalStatus.EM_TRANSITO),
            TestData.Vehicle(carrierB, plate: "B1", status: OperationalStatus.LIVRE),
            TestData.Vehicle(carrierC, plate: "C1", status: OperationalStatus.EM_TRANSITO));
        await ctx.SaveChangesAsync();

        var result = await new VehicleRepository(_db.CreateContext())
            .CountFreeByCarrierIdsAsync(new[] { carrierA, carrierB, carrierC });

        result[carrierA].Should().Be(2);
        result[carrierB].Should().Be(1);
        result.Should().NotContainKey(carrierC);
    }
}
