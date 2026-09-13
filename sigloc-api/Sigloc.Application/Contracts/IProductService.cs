using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IProductService
{
    Task<ProductResponseDto> CreateAsync(Guid contractorId, ProductRequestDto dto, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> GetByIdAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);
    Task<PagedProductsDto> SearchAsync(Guid contractorId, ProductQueryDto query, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> UpdateAsync(Guid contractorId, Guid id, ProductRequestDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);
}