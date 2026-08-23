using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class AppointmentPhotoConfiguration : IEntityTypeConfiguration<AppointmentPhoto>
{
    public void Configure(EntityTypeBuilder<AppointmentPhoto> builder)
    {
        builder.ToTable("AppointmentPhotos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Section).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Url).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(1000);
        builder.Property(p => p.UploadedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(p => new { p.AppointmentId, p.Section });

        builder.HasOne(p => p.Appointment)
            .WithMany(a => a.Photos)
            .HasForeignKey(p => p.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
