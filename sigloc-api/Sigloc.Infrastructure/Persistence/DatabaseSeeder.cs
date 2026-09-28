using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds a realistic sample dataset for local
/// development. The seeding is idempotent: it only runs when the database has no
/// contractor yet, so restarting the app (or the container, with a persisted
/// volume) never duplicates data.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Default password for every seeded local user. Only used in Development.
    /// </summary>
    public const string DefaultPassword = "Password123!";

    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var dbContext = provider.GetRequiredService<SiglocDbContext>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var logger = provider.GetRequiredService<ILogger<SiglocDbContextSeedMarker>>();

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Set<Contractor>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seed data already present; skipping seeding.");
            return;
        }

        logger.LogInformation("Seeding development sample data...");
        await SeedAsync(dbContext, passwordHasher, cancellationToken);
        logger.LogInformation(
            "Seeding complete. Login with email '{Email}' and password '{Password}'.",
            "operator@sigloc.dev",
            DefaultPassword);
    }

    private static async Task SeedAsync(SiglocDbContext dbContext, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        // --- Contractor (shipper company) and its login-ready user ---
        var contractor = new Contractor
        {
            Id = Guid.NewGuid(),
            Cnpj = "12345678000199",
            CompanyName = "Sigloc Logística Ltda",
            TradeName = "Sigloc"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "operator@sigloc.dev",
            PasswordHash = passwordHasher.Hash(DefaultPassword),
            AuthProvider = AuthProvider.Local,
            ProfileType = ProfileType.Contratante,
            CompanyId = contractor.Id
        };

        await dbContext.Set<Contractor>().AddAsync(contractor, cancellationToken);
        await dbContext.Set<User>().AddAsync(user, cancellationToken);

        // --- Products (one general, one frozen, one dangerous) ---
        var palletized = new Product
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            Sku = "SKU-GEN-001",
            Name = "Palletized dry goods",
            Type = "General cargo",
            Category = ProductCategory.General,
            TransportEnvironment = TransportEnvironment.Dry,
            PackagingType = PackagingType.Palletized,
            Dangerous = false,
            Fragile = true,
            DefaultWeight = 800,
            DefaultVolume = 3.5
        };

        var frozen = new Product
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            Sku = "SKU-FRZ-002",
            Name = "Frozen food pallet",
            Type = "Perishable",
            Category = ProductCategory.General,
            TransportEnvironment = TransportEnvironment.Frozen,
            TempMin = -20,
            TempMax = -12,
            PackagingType = PackagingType.MasterCartons,
            Dangerous = false,
            Fragile = false,
            DefaultWeight = 1200,
            DefaultVolume = 5.0
        };

        var chemicals = new Product
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            Sku = "SKU-CHM-003",
            Name = "Industrial chemicals",
            Type = "Hazardous",
            Category = ProductCategory.LiquidBulk,
            TransportEnvironment = TransportEnvironment.Dry,
            Dangerous = true,
            Fragile = false,
            DefaultWeight = 2000,
            DefaultVolume = 2.0
        };

        await dbContext.Set<Product>().AddRangeAsync(new[] { palletized, frozen, chemicals }, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        // --- Available route segments (real Brazilian coordinates: "longitude,latitude") ---
        // Curitiba -> São Paulo
        var segmentAvailable1 = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            OriginAddress = "Curitiba, PR",
            DestinationAddress = "São Paulo, SP",
            OriginCoordinate = "-49.273252,-25.429595",
            DestinationCoordinate = "-46.633308,-23.550520",
            DistanceKm = 408.0,
            EstimatedTimeHours = 5.5,
            BudgetCeiling = 3200.00m,
            EstimatedTollCost = 180.00m,
            PickupDeadline = now.AddDays(2),
            DeliveryDeadline = now.AddDays(3),
            Status = SegmentStatus.Available,
            Items =
            {
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = palletized.Id, Quantity = 10 },
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = frozen.Id, Quantity = 5 }
            }
        };

        // São Paulo -> Campinas
        var segmentAvailable2 = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            OriginAddress = "São Paulo, SP",
            DestinationAddress = "Campinas, SP",
            OriginCoordinate = "-46.633308,-23.550520",
            DestinationCoordinate = "-47.061580,-22.905833",
            DistanceKm = 96.0,
            EstimatedTimeHours = 1.5,
            BudgetCeiling = 1400.00m,
            EstimatedTollCost = 62.00m,
            PickupDeadline = now.AddDays(2),
            DeliveryDeadline = now.AddDays(3),
            Status = SegmentStatus.Available,
            Items =
            {
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = palletized.Id, Quantity = 8 }
            }
        };

        // Rio de Janeiro -> Belo Horizonte (dangerous cargo)
        var segmentAvailable3 = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            OriginAddress = "Rio de Janeiro, RJ",
            DestinationAddress = "Belo Horizonte, MG",
            OriginCoordinate = "-43.172896,-22.906847",
            DestinationCoordinate = "-43.937772,-19.920830",
            DistanceKm = 440.0,
            EstimatedTimeHours = 6.5,
            BudgetCeiling = 3800.00m,
            EstimatedTollCost = 210.00m,
            PickupDeadline = now.AddDays(4),
            DeliveryDeadline = now.AddDays(5),
            Status = SegmentStatus.Available,
            Items =
            {
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = chemicals.Id, Quantity = 4 }
            }
        };

        await dbContext.Set<RouteSegment>().AddRangeAsync(
            new[] { segmentAvailable1, segmentAvailable2, segmentAvailable3 },
            cancellationToken);

        // --- A consolidated route already in auction, with its two routed segments ---
        var consolidatedRoute = new ConsolidatedRoute
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            Status = RouteStatus.InAuction,
            TotalDistanceKm = 520.0,
            EstimatedTimeHours = 7.5,
            ConsolidatedCeiling = 5300.00m,
            EstimatedAnttFloor = 5300.00m,
            TotalWeightKg = 21000,
            TotalVolumeM3 = 73
        };

        // Goiânia -> Brasília (routed to the consolidated route above)
        var routedSegment1 = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            RouteId = consolidatedRoute.Id,
            OriginAddress = "Goiânia, GO",
            DestinationAddress = "Brasília, DF",
            OriginCoordinate = "-49.253887,-16.686882",
            DestinationCoordinate = "-47.929820,-15.780148",
            DistanceKm = 209.0,
            EstimatedTimeHours = 3.0,
            BudgetCeiling = 2600.00m,
            EstimatedTollCost = 90.00m,
            PickupDeadline = now.AddDays(1),
            DeliveryDeadline = now.AddDays(2),
            Status = SegmentStatus.Routed,
            Items =
            {
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = palletized.Id, Quantity = 12 }
            }
        };

        // Brasília -> Anápolis (routed to the consolidated route above)
        var routedSegment2 = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            RouteId = consolidatedRoute.Id,
            OriginAddress = "Brasília, DF",
            DestinationAddress = "Anápolis, GO",
            OriginCoordinate = "-47.929820,-15.780148",
            DestinationCoordinate = "-48.952770,-16.326810",
            DistanceKm = 140.0,
            EstimatedTimeHours = 2.0,
            BudgetCeiling = 2700.00m,
            EstimatedTollCost = 70.00m,
            PickupDeadline = now.AddDays(1),
            DeliveryDeadline = now.AddDays(2),
            Status = SegmentStatus.Routed,
            Items =
            {
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = frozen.Id, Quantity = 9 }
            }
        };

        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            RouteId = consolidatedRoute.Id,
            OpenedAt = now,
            ExpiresAt = now.AddDays(2),
            AutomaticAward = true,
            Status = AuctionStatus.Open
        };

        // --- Carriers and their vehicles (bidders on the auction above) ---
        var carrierFrio = new Carrier
        {
            Id = Guid.NewGuid(),
            Cnpj = "98765432000155",
            CompanyName = "Expresso Frio Ltda",
            TradeName = "Expresso Frio",
            AverageRating = 4.7,
            OnTimeDeliveryRate = 98,
            HasActiveInsurancePolicy = true
        };

        var carrierRapido = new Carrier
        {
            Id = Guid.NewGuid(),
            Cnpj = "45678912000133",
            CompanyName = "Rápido Sul Transportes",
            TradeName = "Rápido Sul",
            AverageRating = 4.2,
            OnTimeDeliveryRate = 95,
            HasActiveInsurancePolicy = true
        };

        var vehicleFrio = new Vehicle(
            transportadoraId: carrierFrio.Id,
            plate: "ABC1D23",
            model: "Volvo FH 460",
            axleCount: 6,
            capacityWeight: 27000m,
            capacityVolume: 90m,
            bodyType: VehicleBodyType.Frigorifico,
            refrigerationLevel: RefrigerationLevel.Congelado,
            hasMopp: false,
            hasCargoSecuring: true,
            driver: "João Pereira",
            currentLocation: "Curitiba, PR",
            status: OperationalStatus.LIVRE);

        var vehicleRapido = new Vehicle(
            transportadoraId: carrierRapido.Id,
            plate: "EFG4H56",
            model: "Scania R450",
            axleCount: 6,
            capacityWeight: 26000m,
            capacityVolume: 88m,
            bodyType: VehicleBodyType.Frigorifico,
            refrigerationLevel: RefrigerationLevel.Congelado,
            hasMopp: false,
            hasCargoSecuring: true,
            driver: "Maria Santos",
            currentLocation: "São Paulo, SP",
            status: OperationalStatus.LIVRE);

        // --- Bids: Expresso Frio holds the best (lowest total) position ---
        var bidFrio = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            CarrierId = carrierFrio.Id,
            VehicleId = vehicleFrio.Id,
            NetFreightValue = 5000.00m,
            TollValue = 200.00m,
            TotalValue = 5200.00m,
            SubmittedAt = now.AddHours(-3),
            Status = BidStatus.Winning
        };

        var bidRapido = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            CarrierId = carrierRapido.Id,
            VehicleId = vehicleRapido.Id,
            NetFreightValue = 5100.00m,
            TollValue = 200.00m,
            TotalValue = 5300.00m,
            SubmittedAt = now.AddHours(-2),
            Status = BidStatus.Losing
        };

        await dbContext.Set<ConsolidatedRoute>().AddAsync(consolidatedRoute, cancellationToken);
        await dbContext.Set<RouteSegment>().AddRangeAsync(new[] { routedSegment1, routedSegment2 }, cancellationToken);
        await dbContext.Set<Auction>().AddAsync(auction, cancellationToken);
        await dbContext.Set<Carrier>().AddRangeAsync(new[] { carrierFrio, carrierRapido }, cancellationToken);
        await dbContext.Set<Vehicle>().AddRangeAsync(new[] { vehicleFrio, vehicleRapido }, cancellationToken);
        await dbContext.Set<Bid>().AddRangeAsync(new[] { bidFrio, bidRapido }, cancellationToken);

        // --- Carrier login + active partnership so the opportunity board (Mural de Fretes)
        //     is non-empty end-to-end. Expresso Frio partners with the seeded contractor. ---
        var carrierUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "carrier@sigloc.dev",
            PasswordHash = passwordHasher.Hash(DefaultPassword),
            AuthProvider = AuthProvider.Local,
            ProfileType = ProfileType.Transportador,
            CompanyId = carrierFrio.Id
        };

        var activePartnership = new PartnerConnection
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            CarrierId = carrierFrio.Id,
            Status = PartnershipStatus.Active,
            InitiatedBy = PartnershipInitiator.Contractor
        };

        await dbContext.Set<User>().AddAsync(carrierUser, cancellationToken);
        await dbContext.Set<PartnerConnection>().AddAsync(activePartnership, cancellationToken);

        // --- An in-transit trip (Viagem EM_CURSO) so the executive dashboard renders live
        //     network efficiency and SLA milestones. The route consolidates two segments to
        //     count as a Continuous Move success. ---
        var transitRoute = new ConsolidatedRoute
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            Status = RouteStatus.InTransit,
            TotalDistanceKm = 504.0,
            EstimatedTimeHours = 7.0,
            ConsolidatedCeiling = 4100.00m,
            EstimatedAnttFloor = 4000.00m,
            TotalWeightKg = 23800, // ~88% of the 27.000 kg vehicle capacity
            TotalVolumeM3 = 67     // ~74% of the 90 m³ vehicle capacity
        };

        var transitSegment1 = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            RouteId = transitRoute.Id,
            OriginAddress = "Curitiba, PR",
            DestinationAddress = "São Paulo, SP",
            OriginCoordinate = "-49.273252,-25.429595",
            DestinationCoordinate = "-46.633308,-23.550520",
            DistanceKm = 408.0,
            EstimatedTimeHours = 5.5,
            BudgetCeiling = 3200.00m,
            EstimatedTollCost = 180.00m,
            PickupDeadline = now.AddMinutes(45),   // imminent COLETA → critical SLA milestone
            DeliveryDeadline = now.AddHours(9),
            Status = SegmentStatus.Routed,
            Items =
            {
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = palletized.Id, Quantity = 14 }
            }
        };

        var transitSegment2 = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            RouteId = transitRoute.Id,
            OriginAddress = "São Paulo, SP",
            DestinationAddress = "Campinas, SP",
            OriginCoordinate = "-46.633308,-23.550520",
            DestinationCoordinate = "-47.061580,-22.905833",
            DistanceKm = 96.0,
            EstimatedTimeHours = 1.5,
            BudgetCeiling = 900.00m,
            EstimatedTollCost = 62.00m,
            PickupDeadline = now.AddHours(3),
            DeliveryDeadline = now.AddHours(11),
            Status = SegmentStatus.Routed,
            Items =
            {
                new ProductRouteSegment { Id = Guid.NewGuid(), ProductId = frozen.Id, Quantity = 6 }
            }
        };

        var transitAuction = new Auction
        {
            Id = Guid.NewGuid(),
            RouteId = transitRoute.Id,
            OpenedAt = now.AddDays(-2),
            ExpiresAt = now.AddDays(-1),
            AutomaticAward = true,
            Status = AuctionStatus.Closed
        };

        var transitBid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = transitAuction.Id,
            CarrierId = carrierFrio.Id,
            VehicleId = vehicleFrio.Id,
            NetFreightValue = 4080.00m,
            TollValue = 200.00m,
            TotalValue = 4280.00m, // above the 4.100 ceiling → over-budget cost deviation
            SubmittedAt = now.AddDays(-2).AddHours(1),
            Status = BidStatus.Winner
        };

        var transitTrip = new Trip
        {
            Id = Guid.NewGuid(),
            RouteId = transitRoute.Id,
            AuctionId = transitAuction.Id,
            CarrierId = carrierFrio.Id,
            VehicleId = vehicleFrio.Id,
            BidId = transitBid.Id,
            AgreedValue = transitBid.TotalValue,
            StartedAt = now.AddHours(-2),
            Status = TripStatus.InTransit
        };

        var transitMonitoring = new TripMonitoring
        {
            Id = Guid.NewGuid(),
            TripId = transitTrip.Id,
            LastProgressPercentage = 35,
            LastCalculatedEta = now.AddHours(1), // ETA later than the 45-min pickup deadline
            LastPingAt = now.AddMinutes(-5)
        };

        var blockedAttempt = new BlockedBidAttempt
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            AuctionId = auction.Id,
            CarrierId = carrierRapido.Id,
            Reason = BlockedReason.Volume,
            AttemptedAt = now.AddDays(-3)
        };

        await dbContext.Set<ConsolidatedRoute>().AddAsync(transitRoute, cancellationToken);
        await dbContext.Set<RouteSegment>().AddRangeAsync(new[] { transitSegment1, transitSegment2 }, cancellationToken);
        await dbContext.Set<Auction>().AddAsync(transitAuction, cancellationToken);
        await dbContext.Set<Bid>().AddAsync(transitBid, cancellationToken);
        await dbContext.Set<Trip>().AddAsync(transitTrip, cancellationToken);
        await dbContext.Set<TripMonitoring>().AddAsync(transitMonitoring, cancellationToken);
        await dbContext.Set<BlockedBidAttempt>().AddAsync(blockedAttempt, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Marker type used only to obtain a typed <see cref="ILogger"/> for the seeder.</summary>
    public sealed class SiglocDbContextSeedMarker
    {
    }
}
