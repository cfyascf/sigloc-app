using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations
{
    public class MonitoringConfiguration : IEntityTypeConfiguration<Monitoring> {
        public void Configure(EntityTypeBuilder<Monitoring> b)
            {
            
            b.ToTable("Monitoring");
            b.HasKey(m => m.Id);

            // Propriedades Base
            b.Property(m => m.Id).ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property(m => m.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(m => m.UpdatedAt).HasColumnType("timestamp with time zone");
            b.Property(m => m.Version).HasColumnType("integer");

            b.Property(m => m.ViagemId).IsRequired().HasColumnType("uuid");

            b.Property(m => m.UltimoProgressoPorcentual)
            .IsRequired()
            .HasColumnType("numeric(5,2)"); // 5 dígitos totais, 2 após a vírgula (ex: 100.00)

            b.Property(m => m.UltimoEtaCalculado).HasColumnType("timestamp with time zone");
            b.Property(m => m.UltimaAtualizacaoPing).HasColumnType("timestamp with time zone");
        }
    }
}