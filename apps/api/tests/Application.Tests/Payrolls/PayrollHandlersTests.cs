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
    private IWorkScheduleRepository _workScheduleRepo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPayrollRepository>();
        _workScheduleRepo = Substitute.For<IWorkScheduleRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();

        _repo.GetByPeriodAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repo.GetApprovedLeavesOverlappingAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workScheduleRepo.GetByDateRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    // ── GetPayrollPeriod ─────────────────────────────────────────────────────

    /// <summary>
    /// Kỳ chưa có bản ghi nào vẫn trả về đủ danh sách nhân sự với trạng thái "NotCreated"
    /// (chưa lập kỳ lương chính thức) và các con số được ước tính từ hồ sơ lương.
    /// </summary>
    [Test]
    public async Task GetPeriod_NoSavedRecords_ReturnsComputedNotCreatedRows()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);

        var result = await new GetPayrollPeriodHandler(_repo, _workScheduleRepo)
            .Handle(new GetPayrollPeriodQuery(2026, 8, null, null, null), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("NotCreated");
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
        var record = CreatePaidRecord(user.Id, 2026, 8, 10_000_000m, 1_000_000m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new GetPayrollPeriodHandler(_repo, _workScheduleRepo)
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

        var result = await new GetPayrollPeriodHandler(_repo, _workScheduleRepo)
            .Handle(new GetPayrollPeriodQuery(2026, 8, null, null, "Dentist,Doctor"), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].UserId.Should().Be(dentistUser.Id);
    }

    // ── Pay / PayAll (yêu cầu kỳ đã Approved) ────────────────────────────────

    /// <summary>Chưa tạo kỳ lương thì không thể chi trả.</summary>
    [Test]
    public async Task Pay_NoExistingRecord_ThrowsValidationException()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns((PayrollRecord?)null);

        Func<Task> act = () => new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Kỳ mới Nháp/Đã tính (chưa được Owner duyệt) thì chưa thể chi trả.</summary>
    [Test]
    public async Task Pay_NotYetApproved_ThrowsValidationException()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        var record = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 1_000_000m, 0, 0, 1m, 0m, 0m);
        record.MarkCalculated(); // Đã tính nhưng chưa duyệt

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        Func<Task> act = () => new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Kỳ đã được duyệt thì chi trả thành công, chuyển sang trạng thái Paid.</summary>
    [Test]
    public async Task Pay_ApprovedRecord_MarksPaid()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        var record = CreateApprovedRecord(user.Id, 2026, 8, 10_000_000m, 1_000_000m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        var result = await new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        result.Status.Should().Be("Paid");
        result.PaidAt.Should().NotBeNull();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Không cho phép chi trả hai lần cho cùng một nhân sự trong cùng một kỳ.</summary>
    [Test]
    public async Task Pay_AlreadyPaid_ThrowsValidationException()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = CreatePaidRecord(user.Id, 2026, 8, 10_000_000m, 0m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        Func<Task> act = () => new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Nhân sự chưa được thiết lập lương (thực nhận = 0) không thể chi trả dù đã duyệt.</summary>
    [Test]
    public async Task Pay_NoSalaryConfigured_ThrowsValidationException()
    {
        var user = MakeStaffUser(null, null);
        var record = CreateApprovedRecord(user.Id, 2026, 8, 0m, 0m, 0m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        Func<Task> act = () => new PayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayPayrollCommand(2026, 8, user.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Chi trả toàn bộ: chỉ những bản ghi đã Approved trong kỳ mới được chi; người chưa thiết lập
    /// lương (thực nhận = 0) bị bỏ qua và liệt kê riêng chứ không làm hỏng cả đợt chi.
    /// </summary>
    [Test]
    public async Task PayAll_MixedStaff_PaysConfiguredAndReportsFailures()
    {
        var paid = MakeStaffUser(10_000_000m, 0m);
        var missing = MakeStaffUser(null, null);
        var paidRecord = CreateApprovedRecord(paid.Id, 2026, 8, 10_000_000m, 0m, 0m);
        var missingRecord = CreateApprovedRecord(missing.Id, 2026, 8, 0m, 0m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([paid, missing]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([paidRecord, missingRecord]);

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

    /// <summary>Chưa tạo/chưa duyệt kỳ lương thì bị bỏ qua khi chi trả hàng loạt, không tính là lỗi.</summary>
    [Test]
    public async Task PayAll_NoRecordForUser_IsSkippedSilently()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        // Không có bản ghi nào trong kỳ (GetByPeriodAsync trả về [] theo SetUp)

        var result = await new PayAllPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayAllPayrollCommand(2026, 8, null), CancellationToken.None);

        result.PaidCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
    }

    /// <summary>
    /// Chi trả toàn bộ bỏ qua những người đã được chi trả trước đó trong kỳ.
    /// </summary>
    [Test]
    public async Task PayAll_AlreadyPaidStaff_IsSkipped()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = CreatePaidRecord(user.Id, 2026, 8, 10_000_000m, 0m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new PayAllPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new PayAllPayrollCommand(2026, 8, null), CancellationToken.None);

        result.PaidCount.Should().Be(0);
        result.AlreadyPaidCount.Should().Be(1);
        result.Failures.Should().BeEmpty();
    }

    // ── Unpay ─────────────────────────────────────────────────────────────────

    /// <summary>Hoàn tác chi trả đưa bản ghi về trạng thái Đã duyệt (không lùi về Nháp) và xóa ngày chi trả.</summary>
    [Test]
    public async Task Unpay_PaidRecord_ResetsToApproved()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = CreatePaidRecord(user.Id, 2026, 8, 10_000_000m, 0m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        var result = await new UnpayPayrollHandler(_repo, _activityLog, _currentUser)
            .Handle(new UnpayPayrollCommand(2026, 8, user.Id), CancellationToken.None);

        record.Status.Should().Be(PayrollStatus.Approved);
        result.Status.Should().Be("Approved");
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

    // ── Tạo / Tính / Duyệt kỳ lương ───────────────────────────────────────────

    /// <summary>Tạo kỳ lương sinh bản ghi Nháp cho nhân sự chưa có bản ghi trong kỳ.</summary>
    [Test]
    public async Task CreatePeriod_NoExistingRecords_CreatesDraftForEveryUser()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);

        var result = await new CreatePayrollPeriodHandler(_repo, _workScheduleRepo, _activityLog, _currentUser)
            .Handle(new CreatePayrollPeriodCommand(2026, 8), CancellationToken.None);

        result.AffectedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        await _repo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<PayrollRecord>>(r => r.Count() == 1 && r.First().Status == PayrollStatus.Draft),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Tạo kỳ lương bỏ qua nhân sự đã có bản ghi (ở bất kỳ trạng thái nào) trong kỳ.</summary>
    [Test]
    public async Task CreatePeriod_UserAlreadyHasRecord_IsSkipped()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var existing = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([existing]);

        var result = await new CreatePayrollPeriodHandler(_repo, _workScheduleRepo, _activityLog, _currentUser)
            .Handle(new CreatePayrollPeriodCommand(2026, 8), CancellationToken.None);

        result.AffectedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
    }

    /// <summary>Tính lương chốt số liệu và chuyển các bản ghi Nháp sang Đã tính, giữ nguyên Thưởng đã nhập.</summary>
    [Test]
    public async Task CalculatePeriod_DraftRecords_MarksCalculatedAndKeepsBonus()
    {
        var user = MakeStaffUser(10_000_000m, 1_000_000m);
        var record = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 1_000_000m, 0, 0, 1m, 0m, 0m);
        record.SetBonus(500_000m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new CalculatePayrollPeriodHandler(_repo, _workScheduleRepo, _activityLog, _currentUser)
            .Handle(new CalculatePayrollPeriodCommand(2026, 8), CancellationToken.None);

        result.AffectedCount.Should().Be(1);
        record.Status.Should().Be(PayrollStatus.Calculated);
        record.NetSalary.Should().Be(11_500_000m); // 10tr + 1tr phụ cấp + 500k thưởng
    }

    /// <summary>Tính lương bỏ qua các bản ghi không ở trạng thái Nháp (đã tính/duyệt/trả trước đó).</summary>
    [Test]
    public async Task CalculatePeriod_NonDraftRecord_IsSkipped()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = CreateApprovedRecord(user.Id, 2026, 8, 10_000_000m, 0m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new CalculatePayrollPeriodHandler(_repo, _workScheduleRepo, _activityLog, _currentUser)
            .Handle(new CalculatePayrollPeriodCommand(2026, 8), CancellationToken.None);

        result.AffectedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        record.Status.Should().Be(PayrollStatus.Approved);
    }

    /// <summary>Duyệt kỳ lương chuyển các bản ghi Đã tính sang Đã duyệt.</summary>
    [Test]
    public async Task ApprovePeriod_CalculatedRecords_MarksApproved()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m);
        record.MarkCalculated();

        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new ApprovePayrollPeriodHandler(_repo, _activityLog, _currentUser)
            .Handle(new ApprovePayrollPeriodCommand(2026, 8), CancellationToken.None);

        result.AffectedCount.Should().Be(1);
        record.Status.Should().Be(PayrollStatus.Approved);
    }

    /// <summary>Duyệt kỳ lương bỏ qua bản ghi còn ở trạng thái Nháp (chưa tính).</summary>
    [Test]
    public async Task ApprovePeriod_DraftRecord_IsSkipped()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m);

        _repo.GetByPeriodAsync(2026, 8, Arg.Any<CancellationToken>()).Returns([record]);

        var result = await new ApprovePayrollPeriodHandler(_repo, _activityLog, _currentUser)
            .Handle(new ApprovePayrollPeriodCommand(2026, 8), CancellationToken.None);

        result.AffectedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        record.Status.Should().Be(PayrollStatus.Draft);
    }

    /// <summary>Sửa Thưởng khi kỳ đang Nháp cập nhật lại thực nhận ngay lập tức.</summary>
    [Test]
    public async Task SetBonus_DraftRecord_UpdatesNetSalary()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m);

        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);
        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);

        var result = await new SetPayrollBonusHandler(_repo, _activityLog, _currentUser)
            .Handle(new SetPayrollBonusCommand(2026, 8, user.Id, 2_000_000m), CancellationToken.None);

        result.Bonus.Should().Be(2_000_000m);
        result.NetSalary.Should().Be(12_000_000m);
    }

    /// <summary>Không thể sửa Thưởng sau khi kỳ đã được tính (Calculated trở lên).</summary>
    [Test]
    public async Task SetBonus_CalculatedRecord_ThrowsValidationException()
    {
        var user = MakeStaffUser(10_000_000m, 0m);
        var record = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m);
        record.MarkCalculated();

        _repo.GetByUserAndPeriodAsync(user.Id, 2026, 8, Arg.Any<CancellationToken>()).Returns(record);

        Func<Task> act = () => new SetPayrollBonusHandler(_repo, _activityLog, _currentUser)
            .Handle(new SetPayrollBonusCommand(2026, 8, user.Id, 2_000_000m), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
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
        var prev = CreatePaidRecord(user.Id, 2026, 7, 9_000_000m, 0m, 0m);

        _repo.GetPayableUsersAsync(Arg.Any<CancellationToken>()).Returns([user]);
        _repo.GetByPeriodAsync(2026, 7, Arg.Any<CancellationToken>()).Returns([prev]);

        var result = await new GetPayrollPeriodHandler(_repo, _workScheduleRepo)
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

        await new GetPayrollPeriodHandler(_repo, _workScheduleRepo)
            .Handle(new GetPayrollPeriodQuery(2026, 1, null, null, null), CancellationToken.None);

        await _repo.Received(1).GetByPeriodAsync(2025, 12, Arg.Any<CancellationToken>());
    }

    /// <summary>Tháng ngoài khoảng 1–12 bị từ chối ngay ở tầng use case.</summary>
    [Test]
    public async Task GetPeriod_InvalidMonth_ThrowsValidationException()
    {
        Func<Task> act = () => new GetPayrollPeriodHandler(_repo, _workScheduleRepo)
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

    private static PayrollRecord CreateApprovedRecord(
        Guid userId, int year, int month, decimal baseSalary, decimal allowance, decimal deduction,
        int leaveDays = 0, decimal allowedLeaveDays = 0m, decimal exceededDays = 0m)
    {
        var record = PayrollRecord.CreateDraft(userId, year, month, baseSalary, allowance, 0, leaveDays, allowedLeaveDays, exceededDays, deduction);
        record.MarkCalculated();
        record.MarkApproved();
        return record;
    }

    private static PayrollRecord CreatePaidRecord(
        Guid userId, int year, int month, decimal baseSalary, decimal allowance, decimal deduction,
        int leaveDays = 0, decimal allowedLeaveDays = 0m, decimal exceededDays = 0m)
    {
        var record = CreateApprovedRecord(userId, year, month, baseSalary, allowance, deduction, leaveDays, allowedLeaveDays, exceededDays);
        record.MarkPaid(null);
        return record;
    }
}
