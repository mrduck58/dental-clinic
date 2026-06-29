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

        builder.Property(tp => tp.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(tp => tp.EstimatedCost)
            .HasPrecision(18, 2);

        builder.Property(tp => tp.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(tp => tp.Appointment)
            .WithMany(a => a.TreatmentPlans)
            .HasForeignKey(tp => tp.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
