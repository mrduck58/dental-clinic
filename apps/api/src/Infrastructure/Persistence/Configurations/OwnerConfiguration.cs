using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(o => o.User)
            .WithOne()
            .HasForeignKey<Owner>(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Owners");
    }
}
