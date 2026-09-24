using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations
{
    public class TelemetryConfiguration : IEntityTypeConfiguration<Telemetry> {
        public void Configure(EntityTypeBuilder<Telemetry> b)
            {
            
            b.ToTable("Telemetry");
            b.HasKey(t => t.Id);

            // Propriedades Base
            b.Property(t => t.Id).ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property(t => t.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(t => t.UpdatedAt).HasColumnType("timestamp with time zone");
            b.Property(t => t.Version).HasColumnType("integer");

            b.Property(t => t.ViagemId).IsRequired().HasColumnType("uuid");

            // Double no C# mapeia muito bem para "double precision" no Postgres
            b.Property(t => t.Latitude).IsRequired().HasColumnType("double precision");
            b.Property(t => t.Longitude).IsRequired().HasColumnType("double precision");
            
            b.Property(t => t.Timestamp).IsRequired().HasColumnType("timestamp with time zone");
        }  
    }
}