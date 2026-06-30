using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class TreatmentCourseConfiguration : IEntityTypeConfiguration<TreatmentCourse>
{
    public void Configure(EntityTypeBuilder<TreatmentCourse> builder)
    {
        builder.ToTable("TreatmentCourses");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.TotalCost).HasPrecision(18, 2);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Dentist)
            .WithMany()
            .HasForeignKey(c => c.DentistId)
            .OnDelete(DeleteBehavior.Restrict);

        // Buổi hẹn thuộc liệu trình — xóa liệu trình thì gỡ liên kết (không xóa buổi hẹn)
        builder.HasMany(c => c.Appointments)
            .WithOne(a => a.Course)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Các đợt thu của liệu trình
        builder.HasMany(c => c.Invoices)
            .WithOne()
            .HasForeignKey(i => i.CourseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
