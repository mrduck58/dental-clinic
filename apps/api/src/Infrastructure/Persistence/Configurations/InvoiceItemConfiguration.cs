using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.AmountCollected).HasPrecision(18, 2);

        // Liệu trình mà dòng thu tiền cho — cột thường (không FK) để migration đơn giản,
        // handler tự đối chiếu theo giá trị.
        builder.Property(i => i.TreatmentPlanId);
        builder.HasIndex(i => i.TreatmentPlanId);

        // Thuộc tính tính toán, không ánh xạ xuống DB.
        builder.Ignore(i => i.LineTotal);
        builder.Ignore(i => i.LineRemaining);
    }
}
