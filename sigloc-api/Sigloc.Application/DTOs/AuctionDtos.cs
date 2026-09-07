using Sigloc.Domain.Constants;

namespace Sigloc.Application.DTOs;

public record CreateAuctionDto(
    Guid RotaId,
    DateTime AbertoEm, 
    DateTime RxpiraEm, 
    bool AdjudicacaoAutomatica,
    EnumLeilao Status
);
public record UpdateAuctionDto(
    Guid RotaId,
    DateTime AbertoEm, 
    DateTime RxpiraEm, 
    bool AdjudicacaoAutomatica,
    EnumLeilao Status
);
public record AuctionResponseDto(
    Guid Id,
    Guid RotaId,
    DateTime AbertoEm, 
    DateTime RxpiraEm, 
    bool AdjudicacaoAutomatica,
    EnumLeilao Status
);
