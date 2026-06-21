using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class DiagnosisConfiguration : IEntityTypeConfiguration<Diagnosis>
{
    public void Configure(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("Diagnoses");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DiagnosisCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(d => d.Notes)
            .HasMaxLength(2000);

        builder.HasOne(d => d.Appointment)
            .WithMany(a => a.Diagnoses)
            .HasForeignKey(d => d.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
