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

        builder.Property(s => s.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);
    }
}
