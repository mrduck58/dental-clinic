using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.ToTable("FollowUps");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Note)
            .HasMaxLength(2000);

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(f => f.Patient)
            .WithMany()
            .HasForeignKey(f => f.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Dentist)
            .WithMany()
            .HasForeignKey(f => f.DentistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.OriginAppointment)
            .WithMany(a => a.OriginatedFollowUps)
            .HasForeignKey(f => f.OriginAppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.TreatmentPlanItem)
            .WithMany(tpi => tpi.FollowUps)
            .HasForeignKey(f => f.TreatmentPlanItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(f => f.TreatmentSession)
            .WithMany(ts => ts.FollowUps)
            .HasForeignKey(f => f.TreatmentSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(f => f.Appointment)
            .WithOne(a => a.FollowUpOrder)
            .HasForeignKey<FollowUp>(f => f.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => f.PatientId);
        builder.HasIndex(f => f.DueDate);
        builder.HasIndex(f => f.Status);
    }
}
