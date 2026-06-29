using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NUnit.Framework;

namespace DentalClinic.API.Domain.Tests.ActivityLogs;

[TestFixture]
public class ActivityLogTests
{
    // ── Factory method: field mapping ─────────────────────────────────────────

    /// <summary>
    /// Create phải ánh xạ đúng tất cả các trường từ tham số vào entity,
    /// đảm bảo không có trường nào bị bỏ sót khi ghi log.
    /// </summary>
    [Test]
    public void Create_AllFields_MappedCorrectly()
    {
        var userId = Guid.NewGuid();

        var log = ActivityLog.Create(
            userId:      userId,
            userName:    "admin@test.com",
            userRole:    "Admin",
            action:      "create",
            module:      "account",
            description: "Tạo tài khoản mới",
            status:      "success",
            ipAddress:   "192.168.1.1",
            targetId:    "abc-123");

        log.UserId.Should().Be(userId);
        log.UserName.Should().Be("admin@test.com");
        log.UserRole.Should().Be("Admin");
        log.Action.Should().Be("create");
        log.Module.Should().Be("account");
        log.Description.Should().Be("Tạo tài khoản mới");
        log.Status.Should().Be("success");
        log.IpAddress.Should().Be("192.168.1.1");
        log.TargetId.Should().Be("abc-123");
    }

    /// <summary>
    /// UserId nullable — log đăng nhập thất bại không có userId (chưa xác định user).
    /// </summary>
    [Test]
    public void Create_NullUserId_IsAllowed()
    {
        var log = ActivityLog.Create(null, "unknown@test.com", "Unknown",
            "login", "account", "Đăng nhập thất bại", "failed");

        log.UserId.Should().BeNull();
    }

    /// <summary>
    /// IpAddress và TargetId là optional — phải null khi không truyền.
    /// </summary>
    [Test]
    public void Create_OptionalFieldsOmitted_AreNull()
    {
        var log = ActivityLog.Create(Guid.NewGuid(), "user", "Staff",
            "edit", "medicine", "Sửa thuốc", "success");

        log.IpAddress.Should().BeNull();
        log.TargetId.Should().BeNull();
    }

    /// <summary>
    /// CreatedAt phải được thiết lập gần thời điểm gọi Create (trong vòng 5 giây),
    /// để timestamp log phản ánh đúng thời điểm hành động xảy ra.
    /// </summary>
    [Test]
    public void Create_CreatedAt_IsCloseToNow()
    {
        var before = DateTimeOffset.UtcNow;

        var log = ActivityLog.Create(Guid.NewGuid(), "user", "Staff",
            "delete", "post", "Xóa bài viết", "success");

        log.CreatedAt.Should().BeCloseTo(before, precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Id mặc định là 0 (chưa được database gán) — entity mới tạo không có Id.
    /// Database sẽ tự gán Id khi SaveChanges.
    /// </summary>
    [Test]
    public void Create_Id_DefaultsToZero()
    {
        var log = ActivityLog.Create(Guid.NewGuid(), "user", "Admin",
            "login", "account", "Đăng nhập", "success");

        log.Id.Should().Be(0);
    }

    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>
    /// ActivityAction phải định nghĩa đủ các hành động cần thiết.
    /// </summary>
    [Test]
    public void ActivityAction_Constants_AreCorrect()
    {
        ActivityAction.Login.Should().Be("login");
        ActivityAction.Create.Should().Be("create");
        ActivityAction.Edit.Should().Be("edit");
        ActivityAction.Delete.Should().Be("delete");
        ActivityAction.Approve.Should().Be("approve");
        ActivityAction.Reject.Should().Be("reject");
        ActivityAction.Cancel.Should().Be("cancel");
        ActivityAction.Payment.Should().Be("payment");
        ActivityAction.Export.Should().Be("export");
    }

    /// <summary>
    /// ActivityModule phải định nghĩa đủ các phân hệ trong hệ thống.
    /// </summary>
    [Test]
    public void ActivityModule_Constants_AreCorrect()
    {
        ActivityModule.Account.Should().Be("account");
        ActivityModule.Appointment.Should().Be("appointment");
        ActivityModule.Service.Should().Be("service");
        ActivityModule.Medicine.Should().Be("medicine");
        ActivityModule.Inventory.Should().Be("inventory");
        ActivityModule.Leave.Should().Be("leave");
        ActivityModule.Feedback.Should().Be("feedback");
        ActivityModule.Promotion.Should().Be("promotion");
        ActivityModule.Post.Should().Be("post");
    }

    /// <summary>
    /// ActivityStatus phải định nghĩa đủ 3 trạng thái: thành công, thất bại, cảnh báo.
    /// </summary>
    [Test]
    public void ActivityStatus_Constants_AreCorrect()
    {
        ActivityStatus.Success.Should().Be("success");
        ActivityStatus.Failed.Should().Be("failed");
        ActivityStatus.Warning.Should().Be("warning");
    }
}
