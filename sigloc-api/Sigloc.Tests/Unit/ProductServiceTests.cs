using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class ProductServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ProductService _service;
    private readonly Guid _contractorId = Guid.NewGuid();

    public ProductServiceTests() => _service = new ProductService(_repository);

    private static ProductRequestDto ValidDto(
        string? sku = "SKU-1",
        string? name = "Caixa",
        string? category = "Geral",
        string? environment = "Seco",
        string? packaging = "Paletizado",
        double? tempMin = null,
        double? tempMax = null,
        double? weight = 10,
        double? volume = 1) => new(
            Sku: sku,
            Name: name,
            Type: null,
            Category: category,
            TransportEnvironment: environment,
            TempMin: tempMin,
            TempMax: tempMax,
            PackagingType: packaging,
            Dangerous: false,
            Fragile: false,
            DefaultWeight: weight,
            DefaultVolume: volume,
            HandlingRestriction: null);

    [Fact]
    public async Task CreateAsync_valid_persists_and_returns_dto()
    {
        _repository.SkuExistsAsync(_contractorId, "SKU-1", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.CreateAsync(_contractorId, ValidDto());

        result.Sku.Should().Be("SKU-1");
        result.ContractorId.Should().Be(_contractorId);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_duplicate_sku_throws()
    {
        _repository.SkuExistsAsync(_contractorId, "SKU-1", null, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _service.CreateAsync(_contractorId, ValidDto());

        await act.Should().ThrowAsync<DuplicateSkuException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "Caixa", "Geral", "Seco", "Paletizado")]      // missing sku
    [InlineData("SKU", null, "Geral", "Seco", "Paletizado")]        // missing name
    [InlineData("SKU", "Caixa", "Invalid", "Seco", "Paletizado")]   // bad category
    [InlineData("SKU", "Caixa", "Geral", "Invalid", "Paletizado")]  // bad environment
    [InlineData("SKU", "Caixa", "Geral", "Seco", "Invalid")]        // bad packaging (General requires it)
    public async Task CreateAsync_invalid_fields_throw_ValidationException(
        string? sku, string? name, string category, string environment, string packaging)
    {
        _repository.SkuExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var dto = ValidDto(sku: sku, name: name, category: category, environment: environment, packaging: packaging);
        var act = () => _service.CreateAsync(_contractorId, dto);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_non_positive_weight_throws(double weight)
    {
        _repository.SkuExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = () => _service.CreateAsync(_contractorId, ValidDto(weight: weight));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_chilled_requires_temperature_range()
    {
        _repository.SkuExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var dto = ValidDto(environment: "Resfriado", tempMin: null, tempMax: null);
        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field == "tempMin");
    }

    [Fact]
    public async Task CreateAsync_chilled_rejects_inverted_range()
    {
        _repository.SkuExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var dto = ValidDto(environment: "Resfriado", tempMin: 10, tempMax: 2);
        var act = () => _service.CreateAsync(_contractorId, dto);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetByIdAsync_not_found_throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), _contractorId, Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var act = () => _service.GetByIdAsync(_contractorId, Guid.NewGuid());

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task SearchAsync_clamps_paging_defaults()
    {
        _repository.SearchAsync(_contractorId, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Product>(), 0));

        var result = await _service.SearchAsync(_contractorId, new ProductQueryDto(null, null, Page: 0, PageSize: 0));

        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
        await _repository.Received(1).SearchAsync(_contractorId, null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_caps_page_size_at_100()
    {
        _repository.SearchAsync(_contractorId, null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<Product>(), 0));

        await _service.SearchAsync(_contractorId, new ProductQueryDto(null, null, Page: 1, PageSize: 500));

        await _repository.Received(1).SearchAsync(_contractorId, null, null, 1, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_invalid_category_throws()
    {
        var act = () => _service.SearchAsync(_contractorId, new ProductQueryDto(null, "Nope", 1, 20));

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field == "categoria");
    }

    [Fact]
    public async Task SearchAsync_computes_total_pages()
    {
        _repository.SearchAsync(_contractorId, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Product> { TestData.Product(_contractorId) }, 45));

        var result = await _service.SearchAsync(_contractorId, new ProductQueryDto(null, null, 1, 20));

        result.TotalItems.Should().Be(45);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_not_found_throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), _contractorId, Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var act = () => _service.UpdateAsync(_contractorId, Guid.NewGuid(), ValidDto());

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_duplicate_sku_on_other_product_throws()
    {
        var product = TestData.Product(_contractorId);
        _repository.GetByIdAsync(product.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(product);
        _repository.SkuExistsAsync(_contractorId, "SKU-1", product.Id, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _service.UpdateAsync(_contractorId, product.Id, ValidDto());

        await act.Should().ThrowAsync<DuplicateSkuException>();
    }

    [Fact]
    public async Task UpdateAsync_valid_persists_changes()
    {
        var product = TestData.Product(_contractorId);
        _repository.GetByIdAsync(product.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(product);
        _repository.SkuExistsAsync(_contractorId, "SKU-2", product.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.UpdateAsync(_contractorId, product.Id, ValidDto(sku: "SKU-2", name: "Novo"));

        result.Sku.Should().Be("SKU-2");
        result.Name.Should().Be("Novo");
        await _repository.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_not_found_throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), _contractorId, Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var act = () => _service.DeleteAsync(_contractorId, Guid.NewGuid());

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_linked_segments_throws_in_use()
    {
        var product = TestData.Product(_contractorId);
        _repository.GetByIdAsync(product.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(product);
        _repository.GetLinkedSegmentIdsAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { Guid.NewGuid().ToString() });

        var act = () => _service.DeleteAsync(_contractorId, product.Id);

        await act.Should().ThrowAsync<ProductInUseException>();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_valid_removes_product()
    {
        var product = TestData.Product(_contractorId);
        _repository.GetByIdAsync(product.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(product);
        _repository.GetLinkedSegmentIdsAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        await _service.DeleteAsync(_contractorId, product.Id);

        await _repository.Received(1).DeleteAsync(product, Arg.Any<CancellationToken>());
    }
}
