using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Constants;
using Sigloc.Domain.Entities;

[Table("Auction")]
public class Auction: BaseEntity
{
    public required Guid RotaId { get; set; } //TODO: Trocar por Rota
    public required DateTime AbertoEm { get; set; } 
    public required DateTime RxpiraEm { get; set; } 
    public required bool AdjudicacaoAutomatica { get; set; }
    public required EnumLeilao Status { get; set; } = EnumLeilao.ABERTO; 
}