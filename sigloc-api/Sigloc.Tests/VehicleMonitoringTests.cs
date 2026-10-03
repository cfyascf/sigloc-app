using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;

namespace Sigloc.Tests;

public class VehicleMonitoringTests
{
    [Fact]
    public async Task Optional_tracking_and_contact_round_trip_and_legacy_updates_preserve_them()
    {
        var repo = new VehicleRepository(); var service = new VehicleService(repo);
        var dto = Request() with { TraccarDeviceId = 42, DriverPhone = " +55 (11) 99999-0000 " };
        var result = await service.CreateAsync(Guid.NewGuid(), dto);
        Assert.Equal(42, result.TraccarDeviceId); Assert.Equal("+55 (11) 99999-0000", result.DriverPhone);
        await service.UpdateAsync(result.TransportadoraId, result.Id, new UpdateVehicleDto("Updated", null, null, null, null, null, null, null, null, null, null));
        result = await service.GetByIdAsync(result.TransportadoraId, result.Id);
        Assert.Equal(42, result.TraccarDeviceId); Assert.NotNull(result.DriverPhone); Assert.Equal("Updated", result.Model);
        await service.UpdateAsync(result.TransportadoraId, result.Id, new UpdateVehicleDto(null, null, null, null, null, null, null, null, null, null, null, 77, ""));
        result = await service.GetByIdAsync(result.TransportadoraId, result.Id);
        Assert.Equal(77, result.TraccarDeviceId); Assert.Null(result.DriverPhone);
    }

    [Fact]
    public async Task Old_create_shape_keeps_optional_values_unknown()
    {
        var result = await new VehicleService(new VehicleRepository()).CreateAsync(Guid.NewGuid(), Request());
        Assert.Null(result.TraccarDeviceId); Assert.Null(result.DriverPhone);
    }

    [Theory]
    [InlineData(0, null)] [InlineData(-1, null)] [InlineData(1, "not-a-phone")]
    public async Task Invalid_optional_values_return_validation_error_without_writing(long deviceId, string? phone)
    {
        var repo = new VehicleRepository();
        await Assert.ThrowsAsync<ValidationException>(() => new VehicleService(repo).CreateAsync(Guid.NewGuid(), Request() with { TraccarDeviceId = deviceId, DriverPhone = phone }));
        Assert.Null(repo.Item);
    }

    private static CreateVehicleDto Request() => new("ABC1D23", "Truck", 2, 1000, 20, default, default, false, false, "Driver", "City");
    private sealed class VehicleRepository : IVehicleRepository
    {
        public Vehicle? Item { get; private set; }
        public Task<Vehicle?> GetByIdAsync(Guid id, Guid transportadoraId, CancellationToken cancellationToken = default) => Task.FromResult(Item);
        public Task<IEnumerable<Vehicle>> GetAllAsync(Guid transportadoraId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Vehicle>>(Item == null ? [] : [Item]);
        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default) { Item = vehicle; return Task.CompletedTask; }
        public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default) { Item = vehicle; return Task.CompletedTask; }
        public Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default) { Item = null; return Task.CompletedTask; }
        public Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Dictionary<Guid, int>> CountFreeByCarrierIdsAsync(IEnumerable<Guid> carrierIds, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<Guid, int>());
    }
}
