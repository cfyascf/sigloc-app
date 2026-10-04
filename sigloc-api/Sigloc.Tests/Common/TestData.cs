using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Tests.Common;

/// <summary>
/// Central factory for valid domain entities used across unit and integration tests.
/// Every factory returns an object that satisfies the entity invariants and the EF
/// model constraints, so tests only override the fields they actually care about.
/// </summary>
public static class TestData
{
    public static Product Product(
        Guid? contractorId = null,
        string sku = "SKU-001",
        string name = "Caixa Padrão",
        ProductCategory category = ProductCategory.General,
        double weight = 10,
        double volume = 1) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId ?? Guid.NewGuid(),
        Sku = sku,
        Name = name,
        Category = category,
        TransportEnvironment = TransportEnvironment.Dry,
        DefaultWeight = weight,
        DefaultVolume = volume,
    };

    public static Vehicle Vehicle(
        Guid? carrierId = null,
        string plate = "ABC1D23",
        OperationalStatus status = OperationalStatus.LIVRE) => new(
            transportadoraId: carrierId ?? Guid.NewGuid(),
            plate: plate,
            model: "Volvo FH",
            axleCount: 3,
            capacityWeight: 20000m,
            capacityVolume: 80m,
            bodyType: VehicleBodyType.Bau,
            refrigerationLevel: RefrigerationLevel.Nenhuma,
            hasMopp: false,
            hasCargoSecuring: true,
            driver: "João",
            currentLocation: "Curitiba, PR",
            status: status);

    public static Contractor Contractor(
        Guid? id = null,
        string cnpj = "11222333000181",
        string companyName = "Contratante Ltda") => new()
    {
        Id = id ?? Guid.NewGuid(),
        Cnpj = cnpj,
        CompanyName = companyName,
    };

    public static Carrier Carrier(
        Guid? id = null,
        string cnpj = "99888777000166",
        string companyName = "Transportadora Ltda",
        double? averageRating = 4.5) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Cnpj = cnpj,
        CompanyName = companyName,
        AverageRating = averageRating,
        HasActiveInsurancePolicy = true,
        OnTimeDeliveryRate = 95,
    };

    public static User User(
        Guid? id = null,
        string email = "user@example.com",
        string? passwordHash = "hash",
        AuthProvider provider = AuthProvider.Local,
        ProfileType profile = ProfileType.Contratante,
        Guid? companyId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = email,
        PasswordHash = passwordHash,
        AuthProvider = provider,
        ProfileType = profile,
        CompanyId = companyId,
    };
}
