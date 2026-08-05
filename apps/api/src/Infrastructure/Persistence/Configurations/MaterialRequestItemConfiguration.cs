using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class MaterialRequestItemConfiguration : IEntityTypeConfiguration<MaterialRequestItem>
{
    public void Configure(EntityTypeBuilder<MaterialRequestItem> builder)
    {
        builder.ToTable("MaterialRequestItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ItemName).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Unit).HasMaxLength(50).IsRequired();

        builder.HasIndex(i => i.MaterialRequestId);
    }
}
