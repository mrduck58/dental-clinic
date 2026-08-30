using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class TreatmentPlanConfiguration : IEntityTypeConfiguration<TreatmentPlan>
{
    public void Configure(EntityTypeBuilder<TreatmentPlan> builder)
    {
        builder.ToTable("TreatmentPlans");

        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(tp => tp.Notes)
            .HasMaxLength(2000);

        builder.Property(tp => tp.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Ignore(tp => tp.TotalCost);

        builder.HasOne(tp => tp.Patient)
            .WithMany()
            .HasForeignKey(tp => tp.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tp => tp.Dentist)
            .WithMany()
            .HasForeignKey(tp => tp.DentistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(tp => tp.Items)
            .WithOne(i => i.TreatmentPlan)
            .HasForeignKey(i => i.TreatmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tp => tp.PatientId);
    }
}
