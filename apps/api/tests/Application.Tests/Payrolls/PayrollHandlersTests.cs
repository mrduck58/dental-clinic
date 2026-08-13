using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Application.UseCases.Payrolls;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Payrolls;

[TestFixture]
public class PayrollHandlersTests
{
    private IPayrollRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPayrollRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();

        _repo.GetByPeriodAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repo.GetApprovedLeavesOverlappingAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    /// <summary>
    /// Kỳ chưa có bản ghi nào vẫn trả về đủ danh sách nhân sự với trạng thái "Pending"
    /// và các con số được tính từ hồ sơ lương.
    /// </summary>
    [Test]
    public async Task GetPeriod_NoSavedRecords_ReturnsComputedPendingRows()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);

        var result = await new GetPayrollPeriodHandler(_repo)
            .Handle(new GetPayrollPeriodQuery(2026, 8, null, null, null), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Pending");
        result.Items[0].NetSalary.Should().Be(11_000_000m);
        result.Summary.TotalNet.Should().Be(11_000_000m);
        result.Summary.PendingCount.Should().Be(1);
    }

    /// <summary>
    /// Bảng lương đã chi trả phải giữ nguyên con số đã chốt, kể cả khi lương trong hồ sơ
    /// nhân sự sau đó được điều chỉnh.
    /// </summary>
    [Test]
    public async Task GetPeriod_PaidRecord_ReturnsSnapshotNotRecomputedValue()
    {
        var user = MakeStaffUser(99_000_000m, 0m);
        var record = PayrollRecord.Create(user.Id, 2026, 8, 10_000_000m, 1_000_000m, 0, 1m, 0m, 0m, 11_000_000m);
        record.MarkPaid(null);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new GetPayrollPeriodHandler(_repo)
            .Handle(new GetPayrollPeriodQuery(2026, 8, null, null, null), CancellationToken.None);

        result.Items[0].Status.Should().Be("Paid");
        result.Items[0].NetSalary.Should().Be(11_000_000m);
        result.Summary.TotalPaid.Should().Be(11_000_000m);
    }

    /// <summary>
    /// Bộ lọc theo vai trò chỉ trả về nhân sự thuộc các vai trò được yêu cầu.
    /// </summary>
    [Test]
    public async Task GetPeriod_RoleFilter_ReturnsOnlyMatchingRoles()
    {
        var staff = MakeStaffUser(10_000_000m, 0m);
        var dentistUser = User.Create("d1", "d@test.com", "hash", UserRole.Dentist);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([staff, dentistUser]);

        var result = await new GetPayrollPeriodHandler(_repo)
            .Handle(new GetPayrollPeriodQuery(2026, 8, null, null, "Dentist,Doctor"), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].UserId.Should().Be(dentistUser.Id);
    }

    /// <summary>
    /// Chi trả lần đầu trong kỳ phải tạo bản ghi mới ở trạng thái Paid.
    /// </summary>
    [Test]
    public async Task Pay_NoExistingRecord_CreatesPaidRecord()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns((PayrollRecord?)null);

        var result = await new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        await _repo.Received(1).AddAsync(
            Arg.Is<PayrollRecord>(r => r.Status == PayrollStatus.Paid && r.NetSalary == 11_000_000m),
            Arg.Any<CancellationToken>());
        result.Status.Should().Be("Paid");
        result.PaidAt.Should().NotBeNull();
    }

    /// <summary>
    /// Không cho phép chi trả hai lần cho cùng một nhân sự trong cùng một kỳ.
    /// </summary>
    [Test]
    public async Task Pay_AlreadyPaid_ThrowsValidationException()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = PayrollRecord.Create(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0m, 0m, 0m, 10_000_000m);
        record.MarkPaid(null);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        Func<Task> act = () => new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Nhân sự chưa được thiết lập lương (thực nhận = 0) không thể chi trả.
    /// </summary>
    [Test]
    public async Task Pay_NoSalaryConfigured_ThrowsValidationException()
    {
        var user = MakeStaffUser(null, null);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);

        Func<Task> act = () => new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<PayrollRecord>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Chi trả toàn bộ: người chưa có lương bị bỏ qua chứ không làm hỏng cả đợt chi,
    /// và phải trả về đích danh người lỗi kèm lý do để hiển thị riêng.
    /// </summary>
    [Test]
    public async Task PayAll_MixedStaff_PaysConfiguredAndReportsFailures()
    {
        var paid = MakeStaffUser(10_000_000m, 0m);
        var missing = MakeStaffUser(null, null);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([paid, missing]);

        var result = await new PayAllPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayAllPayrollCommand(2026, 8, null), CancellationToken.None);

        result.PaidCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.TotalPaid.Should().Be(10_000_000m);
        result.AlreadyPaidCount.Should().Be(0);
        result.Failures.Should().ContainSingle()
            .Which.Should().Match<PayrollFailureDto>(f =>
                f.UserId == missing.Id && f.Reason.Contains("chưa được thiết lập lương"));
    }

    /// <summary>
    /// Chi trả toàn bộ bỏ qua những người đã được chi trả trước đó trong kỳ.
    /// </summary>
    [Test]
    public async Task PayAll_AlreadyPaidStaff_IsSkipped()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = PayrollRecord.Create(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0m, 0m, 0m, 10_000_000m);
        record.MarkPaid(null);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new PayAllPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayAllPayrollCommand(2026, 8, null), CancellationToken.None);

        result.PaidCount.Should().Be(0);
        result.AlreadyPaidCount.Should().Be(1);
        result.Failures.Should().BeEmpty();
    }

    /// <summary>
    /// Hoàn tác chi trả đưa bản ghi về trạng thái chờ duyệt và xóa ngày chi trả.
    /// </summary>
    [Test]
    public async Task Unpay_PaidRecord_ResetsToPending()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = PayrollRecord.Create(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0m, 0m, 0m, 10_000_000m);
        record.MarkPaid(null);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        var result = await new UnpayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new UnpayPayrollCommand(2026, 8, user.Id), CancellationToken.None);

        record.Status.Should().Be(PayrollStatus.Pending);
        result.Status.Should().Be("Pending");
        result.PaidAt.Should().BeNull();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Hoàn tác khi kỳ đó chưa từng được chi trả phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Unpay_NoRecord_ThrowsNotFoundException()
    {
        _repo.GetByUserAndPeriodAsync(Arg.Any<Guid>(), 2026, 8, Arg.Any<CancellationToken>())
            .Returns((PayrollRecord?)null);

        Func<Task> act = () => new UnpayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new UnpayPayrollCommand(2026, 8, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── So sánh với kỳ liền trước ─────────────────────────────────────────────

    /// <summary>
    /// Thực nhận kỳ trước lấy từ bản ghi ĐÃ CHI TRẢ của tháng trước, không tính lại,
    /// để phần so sánh khớp đúng số tiền đã chi thật.
    /// </summary>
    [Test]
    public async Task GetPeriod_PreviousMonthPaid_UsesThatSnapshotForComparison()
    {
        var user = MakeStaffUser(11_000_000m, 0m);
        var prev = PayrollRecord.Create(user.Id, 2026, 7, 9_000_000m, 0m, 0, 0m, 0m, 0m, 9_000_000m);
        prev.MarkPaid(null);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 7, Arg.Any<CancellationToken>()).Returns([prev]);

        var result = await new GetPayrollPeriodHandler(_repo)
            .Handle(new GetPayrollPeriodQuery(2026, 8, null, null, null), CancellationToken.None);

        result.Items[0].NetSalary.Should().Be(11_000_000m);
        result.Items[0].PreviousNetSalary.Should().Be(9_000_000m);
        result.Summary.PreviousTotalNet.Should().Be(9_000_000m);
    }

    /// <summary>Kỳ tháng 1 phải so với tháng 12 của năm liền trước.</summary>
    [Test]
    public async Task GetPeriod_January_ComparesAgainstDecemberOfPreviousYear()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);

        await new GetPayrollPeriodHandler(_repo)
            .Handle(new GetPayrollPeriodQuery(2026, 1, null, null, null), CancellationToken.None);

        await _repo.Received(1).GetByPeriodAsync(2025, 12, Arg.Any<CancellationToken>());
    }

    // ── Báo cáo năm ───────────────────────────────────────────────────────────

    /// <summary>Báo cáo năm luôn đủ 12 kỳ, kể cả tháng chưa có dữ liệu chi trả.</summary>
    [Test]
    public async Task GetYearly_ReturnsTwelveMonthsWithTotals()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        var paidAugust = PayrollRecord.Create(user.Id, 2026, 8, 10_000_000m, 1_000_000m, 0, 1m, 0m, 0m, 11_000_000m);
        paidAugust.MarkPaid(null);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByYearAsync(2026, Arg.Any<CancellationToken>()).Returns([paidAugust]);

        var result = await new GetPayrollYearlyHandler(_repo)
            .Handle(new GetPayrollYearlyQuery(2026), CancellationToken.None);

        result.Months.Should().HaveCount(12);
        result.Months.Select(m => m.Month).Should().BeInAscendingOrder();
        result.TotalNet.Should().Be(11_000_000m * 12);
        // Chỉ tháng 8 đã chi trả
        result.TotalPaid.Should().Be(11_000_000m);
        result.Months.Single(m => m.Month == 8).PaidCount.Should().Be(1);
        result.Months.Single(m => m.Month == 7).PaidCount.Should().Be(0);
        result.AverageMonthlyNet.Should().Be(11_000_000m);
    }

    /// <summary>Tháng có tổng quỹ cao nhất được nêu đích danh trong báo cáo.</summary>
    [Test]
    public async Task GetYearly_PeakMonth_IsTheHighestTotalNet()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        // Tháng 3 đã chốt ở mức cao hơn mức tính lại của các tháng khác
        var march = PayrollRecord.Create(user.Id, 2026, 3, 50_000_000m, 0m, 0, 0m, 0m, 0m, 50_000_000m);
        march.MarkPaid(null);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByYearAsync(2026, Arg.Any<CancellationToken>()).Returns([march]);

        var result = await new GetPayrollYearlyHandler(_repo)
            .Handle(new GetPayrollYearlyQuery(2026), CancellationToken.None);

        result.PeakMonth.Should().Be(3);
    }

    /// <summary>Tháng ngoài khoảng 1–12 bị từ chối ngay ở tầng use case.</summary>
    [Test]
    public async Task GetPeriod_InvalidMonth_ThrowsValidationException()
    {
        Func<Task> act = () => new GetPayrollPeriodHandler(_repo)
            .Handle(new GetPayrollPeriodQuery(2026, 13, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static User MakeStaffUser(decimal? baseSalary, decimal? allowance)
    {
        var user = User.Create($"emp{Guid.NewGuid():N}"[..10], $"{Guid.NewGuid():N}@test.com", "hash", UserRole.Staff, null, "Nguyễn Văn A");
        var employee = Employee.Create(user.Id, "NV001", baseSalary: baseSalary, allowance: allowance, leaveAccrued: 1m);
        user.AttachEmployee(employee);
        return user;
    }
}
