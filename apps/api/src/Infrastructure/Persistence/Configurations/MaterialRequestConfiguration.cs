using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class MaterialRequestConfiguration : IEntityTypeConfiguration<MaterialRequest>
{
    public void Configure(EntityTypeBuilder<MaterialRequest> builder)
    {
        builder.ToTable("MaterialRequests");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.CourseName).HasMaxLength(300);
        builder.Property(m => m.PatientName).HasMaxLength(200);
        builder.Property(m => m.DentistName).HasMaxLength(200);
        builder.Property(m => m.HandledBy).HasMaxLength(200);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(m => m.Status);

        builder.HasMany(m => m.Items)
            .WithOne()
            .HasForeignKey(i => i.MaterialRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(MaterialRequest.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
