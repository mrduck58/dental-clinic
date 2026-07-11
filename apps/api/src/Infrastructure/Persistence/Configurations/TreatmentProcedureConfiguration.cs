using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class TreatmentProcedureConfiguration : IEntityTypeConfiguration<TreatmentProcedure>
{
    public void Configure(EntityTypeBuilder<TreatmentProcedure> builder)
    {
        builder.ToTable("TreatmentProcedures");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.HasOne(p => p.Service)
            .WithMany()
            .HasForeignKey(p => p.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ServiceId, p.StepNumber }).IsUnique();
    }
}
