using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class RouteSegmentService : IRouteSegmentService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IRouteSegmentRepository _repository;
    private readonly IRouteGeocodingService _geocodingService;

    public RouteSegmentService(IRouteSegmentRepository repository, IRouteGeocodingService geocodingService)
    {
        _repository = repository;
        _geocodingService = geocodingService;
    }

    public async Task<RouteSegmentResponseDto> CreateAsync(Guid contractorId, RouteSegmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validated = Validate(dto);
        var products = await LoadProductsAsync(contractorId, validated.Items, cancellationToken);

        var geometry = await _geocodingService.ResolveAsync(validated.Origin, validated.Destination, cancellationToken);

        var segment = new RouteSegment
        {
            ContractorId = contractorId,
            RouteId = null,
            OriginAddress = validated.Origin,
            DestinationAddress = validated.Destination,
            OriginCoordinate = geometry.OriginCoordinate,
            DestinationCoordinate = geometry.DestinationCoordinate,
            DistanceKm = geometry.DistanceKm,
            EstimatedTimeHours = geometry.EstimatedTimeHours,
            BudgetCeiling = validated.BudgetCeiling,
            EstimatedTollCost = validated.EstimatedTollCost,
            PickupDeadline = validated.PickupDeadline,
            DeliveryDeadline = validated.DeliveryDeadline,
            Status = SegmentStatus.Available,
            Items = validated.Items
                .Select(item => new ProductRouteSegment
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                })
                .ToList()
        };

        await _repository.AddAsync(segment, cancellationToken);

        return MapToResponse(segment, products);
    }

    public async Task<RouteSegmentResponseDto> GetByIdAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default)
    {
        var segment = await _repository.GetByIdAsync(id, contractorId, cancellationToken)
            ?? throw new RouteSegmentNotFoundException(id);

        return MapToResponse(segment, ExtractProducts(segment));
    }

    public async Task<PagedRouteSegmentsDto> SearchAsync(Guid contractorId, RouteSegmentQueryDto query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? DefaultPage : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        SegmentStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!SegmentEnumMappings.TryParseStatus(query.Status, out var parsed))
            {
                throw new ValidationException(
                    new[] { new ValidationError("status", "Invalid value. Use 'AVAILABLE', 'ROUTED', 'IN_TRANSIT' or 'COMPLETED'.") },
                    "Could not list the route segments.");
            }

            status = parsed;
        }

        var (items, totalItems) = await _repository.SearchAsync(
            contractorId, query.Origin, query.Destination, status, page, pageSize, cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var mapped = items.Select(segment => MapToResponse(segment, ExtractProducts(segment))).ToList();

        return new PagedRouteSegmentsDto(mapped, page, pageSize, totalItems, totalPages);
    }

    public async Task<RouteSegmentResponseDto> UpdateAsync(Guid contractorId, Guid id, RouteSegmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        var segment = await _repository.GetByIdAsync(id, contractorId, cancellationToken)
            ?? throw new RouteSegmentNotFoundException(id);

        if (segment.Status != SegmentStatus.Available)
        {
            throw new RouteSegmentNotEditableException(id);
        }

        var validated = Validate(dto);
        var products = await LoadProductsAsync(contractorId, validated.Items, cancellationToken);

        var itineraryChanged =
            !string.Equals(segment.OriginAddress, validated.Origin, StringComparison.Ordinal) ||
            !string.Equals(segment.DestinationAddress, validated.Destination, StringComparison.Ordinal);

        if (itineraryChanged)
        {
            var geometry = await _geocodingService.ResolveAsync(validated.Origin, validated.Destination, cancellationToken);

            segment.OriginAddress = validated.Origin;
            segment.DestinationAddress = validated.Destination;
            segment.OriginCoordinate = geometry.OriginCoordinate;
            segment.DestinationCoordinate = geometry.DestinationCoordinate;
            segment.DistanceKm = geometry.DistanceKm;
            segment.EstimatedTimeHours = geometry.EstimatedTimeHours;
        }

        segment.BudgetCeiling = validated.BudgetCeiling;
        segment.EstimatedTollCost = validated.EstimatedTollCost;
        segment.PickupDeadline = validated.PickupDeadline;
        segment.DeliveryDeadline = validated.DeliveryDeadline;
        segment.Items = validated.Items
            .Select(item => new ProductRouteSegment
            {
                RouteSegmentId = segment.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            })
            .ToList();

        await _repository.UpdateAsync(segment, cancellationToken);

        return MapToResponse(segment, products);
    }

    public async Task DeleteAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default)
    {
        var segment = await _repository.GetByIdAsync(id, contractorId, cancellationToken)
            ?? throw new RouteSegmentNotFoundException(id);

        if (segment.Status != SegmentStatus.Available)
        {
            throw new RouteSegmentNotEditableException(id);
        }

        await _repository.DeleteAsync(segment, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, Product>> LoadProductsAsync(
        Guid contractorId,
        IReadOnlyList<ValidatedItem> items,
        CancellationToken cancellationToken)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToArray();
        var products = await _repository.GetProductsByIdsAsync(contractorId, productIds, cancellationToken);
        var byId = products.ToDictionary(p => p.Id);

        var missing = productIds.Where(pid => !byId.ContainsKey(pid)).ToList();
        if (missing.Count > 0)
        {
            var errors = missing
                .Select(pid => new ValidationError("items", $"Product {pid} does not exist or does not belong to this contractor."))
                .ToList();
            throw new ValidationException(errors, "Could not save the route segment.");
        }

        return byId;
    }

    private static ValidatedSegment Validate(RouteSegmentRequestDto dto)
    {
        var errors = new List<ValidationError>();

        var origin = dto.Origin?.Trim();
        if (string.IsNullOrWhiteSpace(origin))
        {
            errors.Add(new ValidationError("origin", "Required."));
        }

        var destination = dto.Destination?.Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            errors.Add(new ValidationError("destination", "Required."));
        }

        if (dto.PickupDeadline is null)
        {
            errors.Add(new ValidationError("pickupDeadline", "Required."));
        }

        if (dto.DeliveryDeadline is null)
        {
            errors.Add(new ValidationError("deliveryDeadline", "Required."));
        }

        if (dto.PickupDeadline is DateTimeOffset pickup &&
            dto.DeliveryDeadline is DateTimeOffset delivery &&
            delivery <= pickup)
        {
            errors.Add(new ValidationError("deliveryDeadline", "Must be later than pickupDeadline."));
        }

        if (dto.EstimatedTollCost is not decimal toll || toll < 0)
        {
            errors.Add(new ValidationError("estimatedTollCost", "Must be zero or greater."));
        }

        if (dto.BudgetCeiling is decimal budget && budget < 0)
        {
            errors.Add(new ValidationError("budgetCeiling", "Must be zero or greater."));
        }

        var validatedItems = new List<ValidatedItem>();
        if (dto.Items is null || dto.Items.Count == 0)
        {
            errors.Add(new ValidationError("items", "The route segment must contain at least one linked product."));
        }
        else
        {
            for (var index = 0; index < dto.Items.Count; index++)
            {
                var item = dto.Items[index];

                if (item.ProductId is not Guid productId || productId == Guid.Empty)
                {
                    errors.Add(new ValidationError($"items[{index}].productId", "Required."));
                    continue;
                }

                if (item.Quantity is not int quantity || quantity <= 0)
                {
                    errors.Add(new ValidationError($"items[{index}].quantity", "Must be greater than zero."));
                    continue;
                }

                validatedItems.Add(new ValidatedItem(productId, quantity));
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors, "Could not save the route segment.");
        }

        return new ValidatedSegment(
            origin!,
            destination!,
            dto.BudgetCeiling,
            dto.EstimatedTollCost!.Value,
            dto.PickupDeadline!.Value,
            dto.DeliveryDeadline!.Value,
            validatedItems);
    }

    private static IReadOnlyDictionary<Guid, Product> ExtractProducts(RouteSegment segment)
        => segment.Items
            .Where(i => i.Product is not null)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.First().Product!);

    private static RouteSegmentResponseDto MapToResponse(RouteSegment segment, IReadOnlyDictionary<Guid, Product> products)
    {
        var items = new List<RouteSegmentItemDto>(segment.Items.Count);
        var carriedProducts = new List<Product>(segment.Items.Count);
        double totalWeight = 0;
        double totalVolume = 0;

        foreach (var link in segment.Items)
        {
            var product = products[link.ProductId];
            carriedProducts.Add(product);

            var weightSubtotal = product.DefaultWeight * link.Quantity;
            var volumeSubtotal = product.DefaultVolume * link.Quantity;

            totalWeight += weightSubtotal;
            totalVolume += volumeSubtotal;

            items.Add(new RouteSegmentItemDto(
                product.Id,
                product.Sku,
                product.Name,
                link.Quantity,
                weightSubtotal,
                volumeSubtotal));
        }

        var requirement = ConsolidatedVehicleRequirement.From(carriedProducts);

        return new RouteSegmentResponseDto(
            segment.Id,
            segment.ContractorId,
            segment.RouteId,
            segment.OriginAddress,
            segment.DestinationAddress,
            segment.DistanceKm,
            segment.EstimatedTimeHours,
            segment.BudgetCeiling,
            segment.EstimatedTollCost,
            segment.PickupDeadline,
            segment.DeliveryDeadline,
            segment.Status.ToWire(),
            items,
            new CalculatedTotalsDto(totalWeight, totalVolume),
            new ConsolidatedVehicleRequirementDto(
                requirement.BaseBodyworkType,
                requirement.MinRefrigerationLevel,
                requirement.RequiresMopp,
                requirement.RequiresCargoFixing),
            segment.CreatedAt);
    }

    private sealed record ValidatedSegment(
        string Origin,
        string Destination,
        decimal? BudgetCeiling,
        decimal EstimatedTollCost,
        DateTimeOffset PickupDeadline,
        DateTimeOffset DeliveryDeadline,
        IReadOnlyList<ValidatedItem> Items);

    private sealed record ValidatedItem(Guid ProductId, int Quantity);
}
