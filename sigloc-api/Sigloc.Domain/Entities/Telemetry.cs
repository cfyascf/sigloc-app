using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

[Table("Telemetry")]
public class Telemetry : BaseEntity
{
    public Guid ViagemId { get; set; } // FK
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    // Navegação do EF Core
    public Travel Viagem { get; set; }

    public Telemetry(Guid viagemId, double latitude, double longitude)
    {
        if (viagemId == Guid.Empty) throw new ArgumentException("ViagemId é obrigatório");
        if (latitude < -90 || latitude > 90) throw new ArgumentException("Latitude é inválida");
        if (longitude < -180 || longitude > 180) throw new ArgumentException("Longitude é inválida");

        ViagemId = viagemId;
        Latitude = latitude;
        Longitude = longitude;
        
        // A data é registrada no momento exato em que a entidade é instanciada
        Timestamp = DateTimeOffset.UtcNow; 
    }
}