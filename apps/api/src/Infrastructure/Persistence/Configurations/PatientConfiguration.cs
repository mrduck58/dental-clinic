using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        // Self-referencing family relationship configuration
        builder.HasOne(p => p.PrimaryPatient)
            .WithMany(p => p.FamilyMembers)
            .HasForeignKey(p => p.PrimaryPatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}