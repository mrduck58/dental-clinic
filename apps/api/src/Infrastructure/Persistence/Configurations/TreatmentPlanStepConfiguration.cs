using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class TreatmentPlanStepConfiguration : IEntityTypeConfiguration<TreatmentPlanStep>
{
    public void Configure(EntityTypeBuilder<TreatmentPlanStep> builder)
    {
        builder.ToTable("TreatmentPlanSteps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.EstimatedSessionCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.DurationMinutes)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(s => s.Percent)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(s => s.TreatmentPlanItem)
            .WithMany(i => i.Steps)
            .HasForeignKey(s => s.TreatmentPlanItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.TreatmentProcedure)
            .WithMany()
            .HasForeignKey(s => s.TreatmentProcedureId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.TreatmentPlanItemId);
        builder.HasIndex(s => s.TreatmentProcedureId);
    }
}
