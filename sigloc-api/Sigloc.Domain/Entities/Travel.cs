using Sigloc.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

[Table("Travel")]
public class Travel : BaseEntity
{
    public Guid RotaId { get; set; } // FK
    public Guid LanceVencedorId { get; set; } // FK
    public DateTimeOffset? IniciadaEm { get; set; }
    public DateTimeOffset? FinalizadaEm { get; set; }
    public TravelStatus Status { get; set; }
    public decimal PisoAnttFinal { get; set; }
    //public Monitoramento Monitoramento { get;  set; }
    //public IReadOnlyCollection<Telemetria> Telemetrias { get;  set; }



    // Construtor de Travel
    public Travel(Guid rotaId, Guid lanceVencedorId, decimal pisoAnttFinal)
    {
        if (rotaId == Guid.Empty) throw new ArgumentException("RotaId é obrigatório");
        if (lanceVencedorId == Guid.Empty) throw new ArgumentException("LanceVencedorId é obrigatório");
        if (pisoAnttFinal < 0) throw new ArgumentException("O Piso ANTT não pode ser negativo");

        RotaId = rotaId;
        LanceVencedorId = lanceVencedorId;
        PisoAnttFinal = pisoAnttFinal;
        Status = TravelStatus.AGUARDANDO_COLETA;
    }
}