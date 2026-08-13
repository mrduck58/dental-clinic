using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class ServiceOptionConfiguration : IEntityTypeConfiguration<ServiceOption>
{
    public void Configure(EntityTypeBuilder<ServiceOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Unit)
            .HasMaxLength(50)
            .HasDefaultValue("Răng");

        builder.HasOne(o => o.Service)
            .WithMany(s => s.Options)
            .HasForeignKey(o => o.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
