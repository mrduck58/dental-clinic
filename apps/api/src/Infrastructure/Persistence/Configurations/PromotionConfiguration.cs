using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.DiscountType).IsRequired().HasMaxLength(20);
        builder.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ServiceIdsJson).HasColumnType("text").HasDefaultValue("[]");
        builder.HasIndex(p => p.Code).IsUnique();
    }
}
