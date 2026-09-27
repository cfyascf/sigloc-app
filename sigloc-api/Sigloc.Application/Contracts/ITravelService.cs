using Sigloc.Application.DTOs;

namespace Sigloc.Application.Services.Interfaces;

public interface ITravelService
{
    // Equivalente ao GET /api/viagens/ativas
    Task<IEnumerable<TravelMacroResponseDto>> ObterViagensAtivasAsync();
    
    // Equivalente ao GET /api/viagens/{id}/detalhes
    Task<TravelDetalheResponseDto> ProcessarDetalhesViagemAsync(Guid travelId);
}