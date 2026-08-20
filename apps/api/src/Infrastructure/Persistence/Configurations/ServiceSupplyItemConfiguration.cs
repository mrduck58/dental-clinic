using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class ServiceSupplyItemConfiguration : IEntityTypeConfiguration<ServiceSupplyItem>
{
    public void Configure(EntityTypeBuilder<ServiceSupplyItem> builder)
    {
        builder.ToTable("ServiceSupplyItems");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ServiceOptionName)
            .HasMaxLength(200);

        builder.HasOne(s => s.Service)
            .WithMany()
            .HasForeignKey(s => s.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.SupplyItem)
            .WithMany()
            .HasForeignKey(s => s.SupplyItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Postgres coi NULL là khác biệt trong unique index — nên 1 dòng "chung" (ServiceOptionName=null)
        // và 1 dòng "riêng cho option X" của cùng 1 vật tư có thể tồn tại song song, đúng ý đồ thiết kế.
        builder.HasIndex(s => new { s.ServiceId, s.SupplyItemId, s.ServiceOptionName }).IsUnique();
    }
}
