using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.FollowUpFromAppointment)
            .WithMany(a => a.FollowUpAppointments)
            .HasForeignKey(a => a.FollowUpFromAppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(a => a.AppointmentType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.AppointmentType.GeneralExam);

        builder.Property(a => a.DurationMinutes)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(a => a.CancellationReason).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.CancellationNote).HasMaxLength(500);

        builder.Property(a => a.RescheduledCount).IsRequired().HasDefaultValue(0);

        builder.Property(a => a.Origin)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.AppointmentOrigin.Online);

        builder.HasIndex(a => a.CancellationReason);
        builder.HasIndex(a => a.AppointmentType);
    }
}
