using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Email).IsRequired().HasMaxLength(256);
        builder.Property(o => o.Code).IsRequired().HasMaxLength(6);
        builder.Property(o => o.Purpose).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(o => o.ExpiresAt).IsRequired();
        builder.Property(o => o.IsUsed).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasIndex(o => new { o.Email, o.Purpose, o.IsUsed, o.ExpiresAt });
    }
}
