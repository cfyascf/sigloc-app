using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class VehicleServiceTests
{
    private readonly IVehicleRepository _repository = Substitute.For<IVehicleRepository>();
    private readonly VehicleService _service;
    private readonly Guid _carrierId = Guid.NewGuid();

    public VehicleServiceTests() => _service = new VehicleService(_repository);

    private static CreateVehicleDto NewVehicleDto(
        string plate = "ABC1D23",
        long? traccarDeviceId = null,
        string? driverPhone = null) => new(
            Plate: plate,
            Model: "Volvo FH",
            AxleCount: 3,
            CapacityWeight: 20000m,
            CapacityVolume: 80m,
            BodyType: VehicleBodyType.Bau,
            RefrigerationLevel: RefrigerationLevel.Nenhuma,
            HasMopp: false,
            HasCargoSecuring: true,
            Driver: "João",
            CurrentLocation: "Curitiba, PR",
            TraccarDeviceId: traccarDeviceId,
            DriverPhone: driverPhone,
            Documents: TestData.VehicleDocuments());

    [Fact]
    public async Task CreateAsync_normalizes_plate_and_persists()
    {
        _repository.ExistsByPlateAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.CreateAsync(_carrierId, NewVehicleDto(plate: "abc-1d23"));

        result.Plate.Should().Be("ABC1D23");
        result.TransportadoraId.Should().Be(_carrierId);
        await _repository.Received(1).AddAsync(
            Arg.Is<Vehicle>(v => v.Plate == "ABC1D23" && v.TransportadoraId == _carrierId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_duplicate_plate_throws_DuplicatePlate()
    {
        _repository.ExistsByPlateAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _service.CreateAsync(_carrierId, NewVehicleDto());

        (await act.Should().ThrowAsync<DuplicatePlateException>()).Which.Plate.Should().Be("ABC1D23");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("AB1234")]
    [InlineData("ABCD123")]
    [InlineData("1234ABC")]
    public async Task CreateAsync_invalid_plate_format_throws_ArgumentException(string plate)
    {
        _repository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _service.CreateAsync(_carrierId, NewVehicleDto(plate: plate));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("ABC1234")]
    public async Task CreateAsync_accepts_mercosul_and_legacy_plates(string plate)
    {
        _repository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.CreateAsync(_carrierId, NewVehicleDto(plate: plate));

        result.Plate.Should().Be(plate);
    }

    [Fact]
    public async Task CreateAsync_non_positive_device_id_throws_ValidationException()
    {
        _repository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _service.CreateAsync(_carrierId, NewVehicleDto(traccarDeviceId: 0));

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field == "traccarDeviceId");
    }

    [Fact]
    public async Task CreateAsync_invalid_phone_throws_ValidationException()
    {
        _repository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _service.CreateAsync(_carrierId, NewVehicleDto(driverPhone: "letters!!"));

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field == "driverPhone");
    }

    [Fact]
    public async Task CreateAsync_out_of_range_values_report_each_field_and_do_not_save()
    {
        _repository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var dto = NewVehicleDto() with
        {
            Model = new string('M', 101),
            AxleCount = 99,
            CapacityWeight = 999_999_999m,
            CapacityVolume = 99_999m,
        };

        var act = () => _service.CreateAsync(_carrierId, dto);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Select(e => e.Field).Should().BeEquivalentTo(
            new[] { "model", "axleCount", "capacityWeight", "capacityVolume" });
        await _repository.DidNotReceive().AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_accepts_values_at_the_limits()
    {
        _repository.ExistsByPlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var dto = NewVehicleDto() with { AxleCount = 12, CapacityWeight = 100_000m, CapacityVolume = 500m };

        await _service.CreateAsync(_carrierId, dto);

        await _repository.Received(1).AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_not_found_throws_KeyNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), _carrierId, Arg.Any<CancellationToken>())
            .Returns((Vehicle?)null);

        var act = () => _service.GetByIdAsync(_carrierId, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_returns_mapped_vehicle()
    {
        var vehicle = TestData.Vehicle(_carrierId);
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await _service.GetByIdAsync(_carrierId, vehicle.Id);

        result.Id.Should().Be(vehicle.Id);
        result.Plate.Should().Be(vehicle.Plate);
    }

    [Fact]
    public async Task GetAllAsync_maps_all_vehicles()
    {
        _repository.GetAllAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new[] { TestData.Vehicle(_carrierId, "AAA1A11"), TestData.Vehicle(_carrierId, "BBB2B22") });

        var result = await _service.GetAllAsync(_carrierId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_flags_vehicles_on_active_trips_without_changing_status()
    {
        var busy = TestData.Vehicle(_carrierId, "AAA1A11"); busy.Id = Guid.NewGuid();
        var idle = TestData.Vehicle(_carrierId, "BBB2B22"); idle.Id = Guid.NewGuid();
        _repository.GetAllAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(new[] { busy, idle });
        _repository.GetIdsOnActiveTripsAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { busy.Id });

        var result = (await _service.GetAllAsync(_carrierId)).ToList();

        result.Single(v => v.Id == busy.Id).OnActiveTrip.Should().BeTrue();
        result.Single(v => v.Id == busy.Id).Status.Should().Be(OperationalStatus.LIVRE);
        result.Single(v => v.Id == idle.Id).OnActiveTrip.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_not_found_throws_KeyNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), _carrierId, Arg.Any<CancellationToken>())
            .Returns((Vehicle?)null);

        var dto = new UpdateVehicleDto(null, null, null, null, null, null, null, null, null, null, null);
        var act = () => _service.UpdateAsync(_carrierId, Guid.NewGuid(), dto);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_partial_update_keeps_unchanged_fields()
    {
        var vehicle = TestData.Vehicle(_carrierId);
        var originalModel = vehicle.Model;
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var dto = new UpdateVehicleDto(
            Model: null, AxleCount: 5, CapacityWeight: null, CapacityVolume: null,
            BodyType: null, RefrigerationLevel: null, HasMopp: null, HasCargoSecuring: null,
            Driver: null, CurrentLocation: null, Status: OperationalStatus.MANUTENCAO);

        await _service.UpdateAsync(_carrierId, vehicle.Id, dto);

        vehicle.AxleCount.Should().Be(5);
        vehicle.Model.Should().Be(originalModel);
        vehicle.Status.Should().Be(OperationalStatus.MANUTENCAO);
        await _repository.Received(1).UpdateAsync(vehicle, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, null, null, "axleCount")]
    [InlineData(null, -500, null, "capacityWeight")]
    [InlineData(null, null, 0, "capacityVolume")]
    [InlineData(13, null, null, "axleCount")]
    [InlineData(null, 100_001, null, "capacityWeight")]
    [InlineData(null, null, 501, "capacityVolume")]
    public async Task UpdateAsync_out_of_range_capacity_throws_ValidationException_and_does_not_save(
        int? axles, int? weight, int? volume, string field)
    {
        var vehicle = TestData.Vehicle(_carrierId);
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var dto = new UpdateVehicleDto(
            Model: null, AxleCount: axles, CapacityWeight: weight, CapacityVolume: volume,
            BodyType: null, RefrigerationLevel: null, HasMopp: null, HasCargoSecuring: null,
            Driver: null, CurrentLocation: null, Status: null);
        var act = () => _service.UpdateAsync(_carrierId, vehicle.Id, dto);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle(e => e.Field == field);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(VehicleBodyType.Sider, RefrigerationLevel.Congelado, true, "refrigerationLevel")]
    [InlineData(VehicleBodyType.Frigorifico, RefrigerationLevel.Nenhuma, true, "refrigerationLevel")]
    [InlineData(VehicleBodyType.Tanque, RefrigerationLevel.Congelado, true, "refrigerationLevel")]
    [InlineData(VehicleBodyType.Prancha, RefrigerationLevel.Nenhuma, false, "hasCargoSecuring")]
    [InlineData((VehicleBodyType)99, RefrigerationLevel.Nenhuma, true, "bodyType")]
    public async Task CreateAsync_rejects_equipment_the_body_type_does_not_allow(
        VehicleBodyType bodyType, RefrigerationLevel refrigeration, bool hasCargoSecuring, string field)
    {
        var dto = NewVehicleDto() with { BodyType = bodyType, RefrigerationLevel = refrigeration, HasCargoSecuring = hasCargoSecuring };

        var act = () => _service.CreateAsync(_carrierId, dto);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle(e => e.Field == field);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(VehicleBodyType.Tanque, RefrigerationLevel.Resfriado)]
    [InlineData(VehicleBodyType.PortaConteiner, RefrigerationLevel.Congelado)]
    [InlineData(VehicleBodyType.Gaiola, RefrigerationLevel.Nenhuma)]
    public async Task CreateAsync_accepts_new_body_types(VehicleBodyType bodyType, RefrigerationLevel refrigeration)
    {
        var result = await _service.CreateAsync(_carrierId, NewVehicleDto() with { BodyType = bodyType, RefrigerationLevel = refrigeration });

        result.BodyType.Should().Be(bodyType);
    }

    [Fact]
    public async Task UpdateAsync_changing_the_body_checks_the_kept_refrigeration()
    {
        var vehicle = TestData.Vehicle(_carrierId, bodyType: VehicleBodyType.Frigorifico, refrigerationLevel: RefrigerationLevel.Congelado);
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);
        var dto = new UpdateVehicleDto(
            Model: null, AxleCount: null, CapacityWeight: null, CapacityVolume: null,
            BodyType: VehicleBodyType.Sider, RefrigerationLevel: null, HasMopp: null, HasCargoSecuring: null,
            Driver: null, CurrentLocation: null, Status: null);

        var act = () => _service.UpdateAsync(_carrierId, vehicle.Id, dto);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle(e => e.Field == "refrigerationLevel");
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("composition")]
    [InlineData("rntrcSource")]
    public async Task CreateAsync_requires_the_vehicle_documents(string field)
    {
        var documents = field == "composition"
            ? TestData.VehicleDocuments() with { Composition = null }
            : TestData.VehicleDocuments() with { RntrcSource = null };

        var act = () => _service.CreateAsync(_carrierId, NewVehicleDto() with { Documents = documents });

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().Contain(e => e.Field == field);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(RntrcCategory.TAC, "11222333000181", "rntrcHolderDocument")]
    [InlineData(RntrcCategory.ETC, "52998224725", "rntrcHolderDocument")]
    [InlineData(RntrcCategory.TAC, "52998224725", null)]
    [InlineData(RntrcCategory.CTC, "11222333000181", null)]
    public async Task CreateAsync_checks_the_third_party_rntrc_holder(RntrcCategory category, string document, string? field)
    {
        var dto = NewVehicleDto() with
        {
            Documents = TestData.VehicleDocuments() with { RntrcCategory = category, RntrcHolderDocument = document },
        };

        var act = () => _service.CreateAsync(_carrierId, dto);

        if (field is null) await act.Should().NotThrowAsync();
        else (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle(e => e.Field == field);
    }

    [Fact]
    public async Task CreateAsync_under_the_carrier_rntrc_needs_it_on_the_profile()
    {
        var carriers = Substitute.For<ICarrierRepository>();
        var service = new VehicleService(_repository, carriers);
        var dto = NewVehicleDto() with { Documents = TestData.VehicleDocuments() with { RntrcSource = RntrcSource.Carrier } };
        carriers.GetByIdAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(TestData.Carrier(id: _carrierId));

        var act = () => service.CreateAsync(_carrierId, dto);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors
            .Should().ContainSingle(e => e.Field == "rntrcSource" && e.Reason.Contains("Perfil"));

        var withRntrc = TestData.Carrier(id: _carrierId);
        withRntrc.RntrcNumber = "87654321";
        withRntrc.RntrcCategory = RntrcCategory.ETC;
        withRntrc.RntrcValidUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);
        carriers.GetByIdAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(withRntrc);

        var result = await service.CreateAsync(_carrierId, dto);

        result.Documents!.RntrcNumber.Should().BeNull("the number lives on the carrier");
        result.Compliance.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_a_combination_needs_a_driver_with_cnh_e()
    {
        var dto = NewVehicleDto() with
        {
            Documents = TestData.VehicleDocuments() with { Composition = VehicleComposition.Articulated, DriverCnhCategory = CnhCategory.C },
        };

        var act = () => _service.CreateAsync(_carrierId, dto);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors
            .Should().ContainSingle(e => e.Field == "driverCnhCategory" && e.Reason.Contains("CNH categoria E"));
    }

    [Fact]
    public async Task GetAllAsync_reports_the_missing_documents_of_an_older_vehicle_as_warnings()
    {
        _repository.GetAllAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(new List<Vehicle> { TestData.Vehicle(_carrierId) });
        _repository.GetIdsOnActiveTripsAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(new HashSet<Guid>());

        var vehicle = (await _service.GetAllAsync(_carrierId)).Single();

        vehicle.Compliance.Should().NotBeEmpty().And.OnlyContain(i => i.Severity == "WARNING");
    }

    [Fact]
    public async Task UpdateAsync_sending_documents_fills_them_in_on_an_older_vehicle()
    {
        var vehicle = TestData.Vehicle(_carrierId);
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);
        var dto = new UpdateVehicleDto(
            Model: null, AxleCount: null, CapacityWeight: null, CapacityVolume: null,
            BodyType: null, RefrigerationLevel: null, HasMopp: null, HasCargoSecuring: null,
            Driver: null, CurrentLocation: null, Status: null,
            Documents: new VehicleDocumentsDto(Composition: VehicleComposition.Rigid, DriverCnhCategory: CnhCategory.E));

        await _service.UpdateAsync(_carrierId, vehicle.Id, dto);

        vehicle.Composition.Should().Be(VehicleComposition.Rigid);
        vehicle.DriverCnhCategory.Should().Be(CnhCategory.E);
        vehicle.RntrcSource.Should().BeNull("missing documents stay optional on edit");
    }

    [Fact]
    public async Task UpdateAsync_status_only_change_keeps_an_older_vehicle_editable()
    {
        // Saved before refrigeration was tied to the body type.
        var vehicle = TestData.Vehicle(_carrierId, bodyType: VehicleBodyType.Sider, refrigerationLevel: RefrigerationLevel.Congelado);
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);
        var dto = new UpdateVehicleDto(
            Model: null, AxleCount: null, CapacityWeight: null, CapacityVolume: null,
            BodyType: null, RefrigerationLevel: null, HasMopp: null, HasCargoSecuring: null,
            Driver: null, CurrentLocation: null, Status: OperationalStatus.MANUTENCAO);

        await _service.UpdateAsync(_carrierId, vehicle.Id, dto);

        vehicle.Status.Should().Be(OperationalStatus.MANUTENCAO);
    }

    [Fact]
    public async Task UpdateAsync_rejects_setting_in_transit_by_hand()
    {
        var vehicle = TestData.Vehicle(_carrierId);
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);
        var dto = new UpdateVehicleDto(
            Model: null, AxleCount: null, CapacityWeight: null, CapacityVolume: null,
            BodyType: null, RefrigerationLevel: null, HasMopp: null, HasCargoSecuring: null,
            Driver: null, CurrentLocation: null, Status: OperationalStatus.EM_TRANSITO);

        var act = () => _service.UpdateAsync(_carrierId, vehicle.Id, dto);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle(e => e.Field == "status");
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_not_found_throws_KeyNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), _carrierId, Arg.Any<CancellationToken>())
            .Returns((Vehicle?)null);

        var act = () => _service.DeleteAsync(_carrierId, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_removes_existing_vehicle()
    {
        var vehicle = TestData.Vehicle(_carrierId);
        _repository.GetByIdAsync(vehicle.Id, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        await _service.DeleteAsync(_carrierId, vehicle.Id);

        await _repository.Received(1).DeleteAsync(vehicle, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public async Task IsPlateAvailableAsync_blank_is_unavailable(string plate, bool expected)
    {
        var result = await _service.IsPlateAvailableAsync(plate);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task IsPlateAvailableAsync_returns_true_when_not_existing()
    {
        _repository.ExistsByPlateAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsPlateAvailableAsync("abc-1d23");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPlateAvailableAsync_returns_false_when_existing()
    {
        _repository.ExistsByPlateAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsPlateAvailableAsync("ABC1D23");

        result.Should().BeFalse();
    }
}
