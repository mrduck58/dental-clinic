using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class TreatmentSupplyUsageConfiguration : IEntityTypeConfiguration<TreatmentSupplyUsage>
{
    public void Configure(EntityTypeBuilder<TreatmentSupplyUsage> builder)
    {
        builder.ToTable("TreatmentSupplyUsages");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitCostAtUsage).HasPrecision(18, 2);
        builder.Property(u => u.CreatedBy).HasMaxLength(200);

        builder.HasOne(u => u.TreatmentPlan)
            .WithMany()
            .HasForeignKey(u => u.TreatmentPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.TreatmentSession)
            .WithMany(ts => ts.SupplyUsages)
            .HasForeignKey(u => u.TreatmentSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.SupplyItem)
            .WithMany()
            .HasForeignKey(u => u.SupplyItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.SupplyTransaction)
            .WithMany()
            .HasForeignKey(u => u.SupplyTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.TreatmentPlanId);
        builder.HasIndex(u => u.TreatmentSessionId);
        builder.HasIndex(u => u.StepEntryId);
    }
}
