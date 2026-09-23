using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

[Table("Telemetria")]
public class Telemetria : BaseEntity
{
    public Guid ViagemId { get; set; } // FK
    public string Latitude { get; set; }
    public string Longitude { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    // Navegação do EF Core
    public Travel Viagem { get; set; }

    public Telemetria(Guid viagemId, string latitude, string longitude)
    {
        if (viagemId == Guid.Empty) throw new ArgumentException("ViagemId é obrigatório");
        if (string.IsNullOrWhiteSpace(latitude)) throw new ArgumentException("Latitude é obrigatória");
        if (string.IsNullOrWhiteSpace(longitude)) throw new ArgumentException("Longitude é obrigatória");

        ViagemId = viagemId;
        Latitude = latitude;
        Longitude = longitude;
        
        // A data é registrada no momento exato em que a entidade é instanciada
        Timestamp = DateTimeOffset.UtcNow; 
    }
}