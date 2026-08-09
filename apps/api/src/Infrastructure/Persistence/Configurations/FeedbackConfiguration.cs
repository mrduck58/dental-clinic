using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Rating)
            .IsRequired();

        builder.Property(f => f.Comment)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(f => f.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => v == "Approved" ? FeedbackStatus.Featured : Enum.Parse<FeedbackStatus>(v))
            .HasMaxLength(20);

        builder.Property(f => f.ReplyText)
            .HasMaxLength(2000);

        builder.HasOne(f => f.Patient)
            .WithMany()
            .HasForeignKey(f => f.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => f.PatientId);

        builder.HasData(
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"),
                CustomerName = "Nguyễn Thị Lan",
                Rating = 5,
                Comment = "Bác sĩ rất tận tình, giải thích rõ ràng từng bước điều trị. Phòng khám sạch sẽ, trang thiết bị hiện đại. Tôi rất hài lòng!",
                Status = FeedbackStatus.Featured,
                ReplyText = "Cảm ơn chị Lan đã tin tưởng phòng khám. Chúng tôi rất vui khi được phục vụ chị!",
                RepliedAt = (DateTimeOffset?)new DateTimeOffset(2026, 5, 15, 10, 30, 0, TimeSpan.Zero),
                CreatedAt = new DateTimeOffset(2026, 5, 14, 8, 0, 0, TimeSpan.Zero),
            },
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000002"),
                CustomerName = "Trần Văn Minh",
                Rating = 4,
                Comment = "Dịch vụ tốt, nhân viên thân thiện. Chỉ hơi chờ lâu một chút nhưng nhìn chung rất ổn.",
                Status = FeedbackStatus.Pending,
                ReplyText = (string?)null,
                RepliedAt = (DateTimeOffset?)null,
                CreatedAt = new DateTimeOffset(2026, 5, 20, 9, 0, 0, TimeSpan.Zero),
            },
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000003"),
                CustomerName = "Phạm Thu Hương",
                Rating = 5,
                Comment = "Lần đầu đến phòng khám, được tư vấn miễn phí rất chi tiết. Bác sĩ chuyên nghiệp, nhẹ nhàng. Sẽ quay lại!",
                Status = FeedbackStatus.Featured,
                ReplyText = (string?)null,
                RepliedAt = (DateTimeOffset?)null,
                CreatedAt = new DateTimeOffset(2026, 5, 22, 14, 0, 0, TimeSpan.Zero),
            },
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000004"),
                CustomerName = "Lê Hoàng Nam",
                Rating = 3,
                Comment = "Chất lượng điều trị ổn nhưng thời gian đợi khá lâu, khoảng 30 phút so với lịch hẹn.",
                Status = FeedbackStatus.Pending,
                ReplyText = (string?)null,
                RepliedAt = (DateTimeOffset?)null,
                CreatedAt = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero),
            },
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000005"),
                CustomerName = "Võ Thị Mai",
                Rating = 5,
                Comment = "Phòng khám rất sạch sẽ và hiện đại. Bác sĩ giỏi, không đau chút nào khi nhổ răng. Cực kỳ hài lòng!",
                Status = FeedbackStatus.Featured,
                ReplyText = "Cảm ơn chị Mai đã chia sẻ! Phòng khám luôn cố gắng mang lại trải nghiệm tốt nhất.",
                RepliedAt = (DateTimeOffset?)new DateTimeOffset(2026, 6, 3, 9, 0, 0, TimeSpan.Zero),
                CreatedAt = new DateTimeOffset(2026, 6, 2, 15, 30, 0, TimeSpan.Zero),
            },
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000006"),
                CustomerName = "Đặng Quốc Hùng",
                Rating = 2,
                Comment = "Giá hơi cao so với các phòng khám khác. Dịch vụ tạm ổn nhưng không đặc biệt.",
                Status = FeedbackStatus.Hidden,
                ReplyText = (string?)null,
                RepliedAt = (DateTimeOffset?)null,
                CreatedAt = new DateTimeOffset(2026, 6, 5, 10, 0, 0, TimeSpan.Zero),
            },
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000007"),
                CustomerName = "Bùi Thị Thanh",
                Rating = 4,
                Comment = "Môi trường phòng khám thoáng mát, nhân viên lễ phép. Bác sĩ giải thích kỹ tình trạng răng miệng.",
                Status = FeedbackStatus.Pending,
                ReplyText = (string?)null,
                RepliedAt = (DateTimeOffset?)null,
                CreatedAt = new DateTimeOffset(2026, 6, 8, 8, 30, 0, TimeSpan.Zero),
            },
            new
            {
                Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000008"),
                CustomerName = "Ngô Tiến Dũng",
                Rating = 5,
                Comment = "Đã điều trị tại đây được 2 năm. Chất lượng luôn ổn định, bác sĩ theo dõi sát sao tình trạng bệnh nhân.",
                Status = FeedbackStatus.Featured,
                ReplyText = "Cảm ơn anh Dũng đã đồng hành cùng phòng khám! Chúng tôi rất trân trọng sự tin tưởng của anh.",
                RepliedAt = (DateTimeOffset?)new DateTimeOffset(2026, 6, 10, 11, 0, 0, TimeSpan.Zero),
                CreatedAt = new DateTimeOffset(2026, 6, 9, 16, 0, 0, TimeSpan.Zero),
            }
        );
    }
}
