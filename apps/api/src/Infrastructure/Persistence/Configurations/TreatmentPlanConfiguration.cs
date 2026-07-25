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

        builder.Property(tp => tp.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(tp => tp.Teeth)
            .HasMaxLength(200);

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

        builder.HasOne(tp => tp.Service)
            .WithMany()
            .HasForeignKey(tp => tp.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Liệu trình là dữ liệu tài chính — giữ lại khi buổi hẹn bị xóa
        builder.HasOne(tp => tp.Appointment)
            .WithMany(a => a.TreatmentPlans)
            .HasForeignKey(tp => tp.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(tp => tp.Invoices)
            .WithOne()
            .HasForeignKey(i => i.TreatmentPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(tp => tp.PatientId);
    }
}
