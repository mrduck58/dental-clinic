using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.Property(e => e.Category)
            .IsRequired()
            .HasConversion(v => v.ToString(), v => Enum.Parse<ExpenseCategory>(v))
            .HasMaxLength(20);

        builder.Property(e => e.Frequency)
            .HasConversion(
                v => v == null ? null : v.ToString(),
                v => v == null ? null : Enum.Parse<RecurrenceFrequency>(v))
            .HasMaxLength(20);

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.RecurringSourceId);
    }
}
