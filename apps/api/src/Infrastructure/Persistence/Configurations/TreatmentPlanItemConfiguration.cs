using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class TreatmentPlanItemConfiguration : IEntityTypeConfiguration<TreatmentPlanItem>
{
    public void Configure(EntityTypeBuilder<TreatmentPlanItem> builder)
    {
        builder.ToTable("TreatmentPlanItems");

        builder.HasKey(tpi => tpi.Id);

        builder.Property(tpi => tpi.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(tpi => tpi.Teeth)
            .HasMaxLength(200);

        builder.Property(tpi => tpi.ServiceOptionName)
            .HasMaxLength(200);

        builder.Property(tpi => tpi.Notes)
            .HasMaxLength(2000);

        builder.Property(tpi => tpi.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(tpi => tpi.EstimatedDurationUnit)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Ignore(tpi => tpi.TotalCost);

        builder.HasOne(tpi => tpi.TreatmentPlan)
            .WithMany(tp => tp.Items)
            .HasForeignKey(tpi => tpi.TreatmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tpi => tpi.Service)
            .WithMany()
            .HasForeignKey(tpi => tpi.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tpi => tpi.ServiceOption)
            .WithMany()
            .HasForeignKey(tpi => tpi.ServiceOptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(tpi => tpi.Sessions)
            .WithOne(s => s.TreatmentPlanItem)
            .HasForeignKey(s => s.TreatmentPlanItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tpi => tpi.InvoiceItems)
            .WithOne(ii => ii.TreatmentPlanItem)
            .HasForeignKey(ii => ii.TreatmentPlanItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(tpi => tpi.TreatmentPlanId);
        builder.HasIndex(tpi => tpi.ServiceId);
    }
}
