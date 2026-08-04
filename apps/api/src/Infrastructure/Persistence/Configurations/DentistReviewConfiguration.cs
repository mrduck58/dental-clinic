using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class DentistReviewConfiguration : IEntityTypeConfiguration<DentistReview>
{
    public void Configure(EntityTypeBuilder<DentistReview> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating).IsRequired();

        builder.Property(r => r.Comment)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.TagsCsv)
            .HasMaxLength(500);

        builder.Ignore(r => r.Tags);

        builder.HasOne(r => r.Dentist)
            .WithMany()
            .HasForeignKey(r => r.DentistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Patient)
            .WithMany()
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Appointment)
            .WithMany()
            .HasForeignKey(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.DentistId, r.PatientId });
        builder.HasIndex(r => r.AppointmentId);
    }
}
