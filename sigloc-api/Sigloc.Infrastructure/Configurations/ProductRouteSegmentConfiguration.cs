using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class ProductRouteSegmentConfiguration : IEntityTypeConfiguration<ProductRouteSegment>
{
    public void Configure(EntityTypeBuilder<ProductRouteSegment> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).IsRequired();

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.RouteSegmentId);
        builder.HasIndex(i => i.ProductId);
    }
}
