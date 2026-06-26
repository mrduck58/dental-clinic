using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.MedicineName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Dosage)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.Unit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Usage)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.Notes)
            .HasMaxLength(500);
    }
}
