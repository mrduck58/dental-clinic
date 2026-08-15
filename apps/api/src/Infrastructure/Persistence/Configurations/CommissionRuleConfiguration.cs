using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ServiceName).HasMaxLength(200);
        builder.Property(r => r.RatePercent).HasPrecision(5, 2);
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasIndex(r => r.DentistId);
        builder.HasIndex(r => r.IsActive);
    }
}
