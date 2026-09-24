using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations
{
    public class TravelConfiguration : IEntityTypeConfiguration<Travel> {
        public void Configure(EntityTypeBuilder<Travel> b)
            {
            
            b.ToTable("Travel");
            b.HasKey(t => t.Id);

            // Propriedades Base
            b.Property(t => t.Id).ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property(t => t.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(t => t.UpdatedAt).HasColumnType("timestamp with time zone");
            b.Property(t => t.Version).HasColumnType("integer");

            // Propriedades Específicas
            b.Property(t => t.LanceVencedorId).IsRequired().HasColumnType("uuid");
            
            b.Property(t => t.IniciadaEm).HasColumnType("timestamp with time zone");
            b.Property(t => t.FinalizadaEm).HasColumnType("timestamp with time zone");
            
            b.Property(t => t.PisoAnttFinal)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

            // Enum salvo como texto
            b.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("text");

            // ----------------------------------------------------
            // RELACIONAMENTOS (Mapeando o Spanglish Travel <-> ViagemId)
            // ----------------------------------------------------
            
            // Relação 1:1 (Travel tem 1 Monitoring)
            b.HasOne(t => t.Monitoramento)
            .WithOne(m => m.Viagem)
            .HasForeignKey<Monitoring>(m => m.ViagemId)
            .OnDelete(DeleteBehavior.Cascade); // Se deletar a viagem, deleta o monitoramento

            // Relação 1:N (Travel tem N Telemetrias)
            b.HasMany(t => t.Telemetrias)
            .WithOne(tel => tel.Viagem)
            .HasForeignKey(tel => tel.ViagemId)
            .OnDelete(DeleteBehavior.Cascade);
            }
    }
}