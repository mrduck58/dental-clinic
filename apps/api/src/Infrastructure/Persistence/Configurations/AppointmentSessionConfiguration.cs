using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class AppointmentSessionConfiguration : IEntityTypeConfiguration<AppointmentSession>
{
    public void Configure(EntityTypeBuilder<AppointmentSession> builder)
    {
        builder.ToTable("AppointmentSessions");

        builder.HasKey(ase => ase.Id);

        builder.Property(ase => ase.Note)
            .HasMaxLength(1000);

        builder.HasOne(ase => ase.Appointment)
            .WithMany(a => a.AppointmentSessions)
            .HasForeignKey(ase => ase.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ase => ase.TreatmentSession)
            .WithMany(ts => ts.AppointmentSessions)
            .HasForeignKey(ase => ase.TreatmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ase => new { ase.AppointmentId, ase.TreatmentSessionId }).IsUnique();
    }
}
