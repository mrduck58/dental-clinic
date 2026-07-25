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

        builder.Property(d => d.Description)
            .HasMaxLength(1000)
            .IsRequired();

        // Các trường khám răng miệng — bác sĩ nhập tự do, giới hạn độ dài vừa phải
        foreach (var prop in new[]
        {
            nameof(Diagnosis.GumCondition), nameof(Diagnosis.OralMucosaCondition),
            nameof(Diagnosis.GumBleeding), nameof(Diagnosis.PainOnChewing),
            nameof(Diagnosis.TeethCount), nameof(Diagnosis.DecayedTeeth),
            nameof(Diagnosis.WornOrBrokenTeeth), nameof(Diagnosis.LooseTeeth),
            nameof(Diagnosis.Tartar), nameof(Diagnosis.Plaque), nameof(Diagnosis.BadBreath),
            nameof(Diagnosis.TmjSymptoms), nameof(Diagnosis.Occlusion), nameof(Diagnosis.OcclusionDeviation),
        })
        {
            builder.Property(prop).HasMaxLength(500);
        }

        builder.Property(d => d.MedicalHistory).HasMaxLength(2000);
        builder.Property(d => d.AllergyHistory).HasMaxLength(2000);
        builder.Property(d => d.Conclusion).HasMaxLength(2000);

        builder.HasOne(d => d.Appointment)
            .WithMany(a => a.Diagnoses)
            .HasForeignKey(d => d.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
