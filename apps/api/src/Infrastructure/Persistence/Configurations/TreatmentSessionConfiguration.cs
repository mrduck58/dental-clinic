using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class TreatmentSessionConfiguration : IEntityTypeConfiguration<TreatmentSession>
{
    public void Configure(EntityTypeBuilder<TreatmentSession> builder)
    {
        builder.ToTable("TreatmentSessions");

        builder.HasKey(ts => ts.Id);

        builder.Property(ts => ts.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ts => ts.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ts => ts.Note)
            .HasMaxLength(2000);

        builder.HasOne(ts => ts.TreatmentPlanItem)
            .WithMany(tpi => tpi.Sessions)
            .HasForeignKey(ts => ts.TreatmentPlanItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ts => ts.TreatmentProcedure)
            .WithMany()
            .HasForeignKey(ts => ts.TreatmentProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ts => ts.Dentist)
            .WithMany()
            .HasForeignKey(ts => ts.DentistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(ts => ts.AppointmentSessions)
            .WithOne(asess => asess.TreatmentSession)
            .HasForeignKey(asess => asess.TreatmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ts => ts.SupplyUsages)
            .WithOne(su => su.TreatmentSession)
            .HasForeignKey(su => su.TreatmentSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(ts => ts.TreatmentPlanItemId);
    }
}
