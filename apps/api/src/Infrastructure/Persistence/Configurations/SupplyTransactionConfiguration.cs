using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class SupplyTransactionConfiguration : IEntityTypeConfiguration<SupplyTransaction>
{
    public void Configure(EntityTypeBuilder<SupplyTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(t => t.Note)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedBy)
            .IsRequired()
            .HasMaxLength(200);

        // SetNull (không Restrict): đây chỉ là nhãn tham khảo phòng nào đã nhận vật tư — xóa phòng không nên
        // bị chặn lại chỉ vì còn lịch sử xuất kho cũ tham chiếu tới nó, cũng không nên xóa mất log giao dịch.
        builder.HasOne(t => t.Room)
            .WithMany()
            .HasForeignKey(t => t.RoomId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
