using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Category column kept for data compatibility but no longer used
        builder.Property(s => s.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(500);
    }
}
