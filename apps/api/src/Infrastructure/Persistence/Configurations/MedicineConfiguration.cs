using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.GenericName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Manufacturer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Description)
            .HasMaxLength(2000);
    }
}
