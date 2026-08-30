using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class StepProgressEntryConfiguration : IEntityTypeConfiguration<StepProgressEntry>
{
    public void Configure(EntityTypeBuilder<StepProgressEntry> builder)
    {
        builder.ToTable("StepProgressEntries");

        builder.HasKey(spe => spe.Id);

        builder.Property(spe => spe.Note)
            .HasMaxLength(1000);

        builder.HasOne(spe => spe.TreatmentSession)
            .WithMany(ts => ts.StepProgressEntries)
            .HasForeignKey(spe => spe.TreatmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(spe => spe.TreatmentSessionId);
    }
}
