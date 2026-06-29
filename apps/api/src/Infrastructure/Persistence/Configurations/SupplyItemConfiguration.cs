using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class SupplyItemConfiguration : IEntityTypeConfiguration<SupplyItem>
{
    public void Configure(EntityTypeBuilder<SupplyItem> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.HasMany(s => s.Transactions)
            .WithOne(t => t.SupplyItem)
            .HasForeignKey(t => t.SupplyItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
