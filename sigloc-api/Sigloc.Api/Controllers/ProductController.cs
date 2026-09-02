using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using System.Security.Claims;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    private Guid GetContractorId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Create([FromBody] ProductRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(GetContractorId(), dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByIdAsync(GetContractorId(), id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "busca")] string? search,
        [FromQuery(Name = "categoria")] string? category,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "tamanhoPagina")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ProductQueryDto(search, category, page, pageSize);
        var result = await _productService.SearchAsync(GetContractorId(), query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(GetContractorId(), id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(GetContractorId(), id, cancellationToken);
        return NoContent();
    }
}
