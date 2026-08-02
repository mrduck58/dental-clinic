using DentalClinic.API.Application.UseCases.Payrolls;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Payrolls;

[TestFixture]
public class PayrollCalculatorTests
{
    /// <summary>
    /// Nghỉ trong định mức phép thì không bị khấu trừ:
    /// thực nhận = lương cơ bản + phụ cấp.
    /// </summary>
    [Test]
    public void Compute_LeaveWithinAllowance_NoDeduction()
    {
        var user = MakeStaffUser(baseSalary: 10_000_000m, allowance: 1_000_000m, leaveAccrued: 2m);
        var leave = MakeApprovedLeave(user.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4));

        var result = PayrollCalculator.Compute(user, [leave], 2026, 8);

        result.LeaveDays.Should().Be(2);
        result.ExceededDays.Should().Be(0);
        result.Deduction.Should().Be(0);
        result.NetSalary.Should().Be(11_000_000m);
    }

    /// <summary>
    /// Vượt định mức phép bị trừ theo số ngày công vượt (lương cơ bản / 26 ngày công).
    /// </summary>
    [Test]
    public void Compute_LeaveExceedsAllowance_DeductsPerExceededDay()
    {
        var user = MakeStaffUser(baseSalary: 13_000_000m, allowance: 1_000_000m, leaveAccrued: 1m);
        var leave = MakeApprovedLeave(user.Id, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 12));

        var result = PayrollCalculator.Compute(user, [leave], 2026, 8);

        result.LeaveDays.Should().Be(3);
        result.ExceededDays.Should().Be(2);
        result.Deduction.Should().Be(1_000_000m); // 2 ngày × (13.000.000 / 26)
        result.NetSalary.Should().Be(13_000_000m);
    }

    /// <summary>
    /// Đơn nghỉ vắt qua hai tháng chỉ tính phần ngày rơi vào đúng kỳ đang xét.
    /// </summary>
    [Test]
    public void Compute_LeaveSpanningTwoMonths_CountsOnlyDaysInPeriod()
    {
        var user = MakeStaffUser(baseSalary: 10_000_000m, allowance: 0m, leaveAccrued: 0m);
        var leave = MakeApprovedLeave(user.Id, new DateOnly(2026, 7, 30), new DateOnly(2026, 8, 2));

        PayrollCalculator.Compute(user, [leave], 2026, 8).LeaveDays.Should().Be(2);
        PayrollCalculator.Compute(user, [leave], 2026, 7).LeaveDays.Should().Be(2);
    }

    /// <summary>
    /// Đơn nghỉ của nhân sự khác không được tính vào bảng lương của người đang xét.
    /// </summary>
    [Test]
    public void Compute_LeaveOfAnotherUser_IsIgnored()
    {
        var user = MakeStaffUser(baseSalary: 10_000_000m, allowance: 0m, leaveAccrued: 0m);
        var otherLeave = MakeApprovedLeave(Guid.NewGuid(), new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 9));

        var result = PayrollCalculator.Compute(user, [otherLeave], 2026, 8);

        result.LeaveDays.Should().Be(0);
        result.NetSalary.Should().Be(10_000_000m);
    }

    /// <summary>
    /// Hồ sơ chưa có lương cơ bản thì trả về 0 và đánh dấu HasSalaryConfigured = false,
    /// KHÔNG suy đoán một con số lương thay thế.
    /// </summary>
    [Test]
    public void Compute_NoSalaryInProfile_ReturnsZeroAndFlagsMissing()
    {
        var user = MakeStaffUser(baseSalary: null, allowance: null, leaveAccrued: null);

        var result = PayrollCalculator.Compute(user, [], 2026, 8);

        result.BaseSalary.Should().Be(0);
        result.NetSalary.Should().Be(0);
        result.HasSalaryConfigured.Should().BeFalse();
    }

    /// <summary>
    /// Nghỉ vượt phép quá nhiều cũng không làm thực nhận âm — khấu trừ bị chặn ở tổng thu nhập.
    /// </summary>
    [Test]
    public void Compute_ExcessiveLeave_NetSalaryNeverNegative()
    {
        var user = MakeStaffUser(baseSalary: 10_000_000m, allowance: 500_000m, leaveAccrued: 0m);
        var leave = MakeApprovedLeave(user.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        var result = PayrollCalculator.Compute(user, [leave], 2026, 8);

        result.NetSalary.Should().Be(0);
        result.Deduction.Should().Be(10_500_000m);
    }

    /// <summary>
    /// Nha sĩ lấy lương từ hồ sơ Dentist chứ không phải hồ sơ Staff.
    /// </summary>
    [Test]
    public void ReadSalaryProfile_DentistRole_ReadsFromDentistProfile()
    {
        var user = User.Create("d1", "d@test.com", "hash", "Dentist");
        var dentist = Dentist.Create(
            user.Id, "NS001", specialization: "Chỉnh nha", licenseNumber: "CCHN-001",
            baseSalary: 30_000_000m, allowance: 2_000_000m, leaveAccrued: 1.5m);
        typeof(User).GetProperty("Dentist")!.SetValue(user, dentist);

        var (baseSalary, allowance, leaveAccrued) = PayrollCalculator.ReadSalaryProfile(user);

        baseSalary.Should().Be(30_000_000m);
        allowance.Should().Be(2_000_000m);
        leaveAccrued.Should().Be(1.5m);
    }

    private static User MakeStaffUser(decimal? baseSalary, decimal? allowance, decimal? leaveAccrued)
    {
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Nguyễn Văn A");
        var staff = DentalClinic.API.Domain.Entities.Staff.Create(
            user.Id, "NV001",
            baseSalary: baseSalary, allowance: allowance, leaveAccrued: leaveAccrued);
        typeof(User).GetProperty("Staff")!.SetValue(user, staff);
        return user;
    }

    private static LeaveRequest MakeApprovedLeave(Guid userId, DateOnly from, DateOnly to)
    {
        var leave = LeaveRequest.Create(userId, LeaveType.Annual, from, to, "Test");
        leave.Approve();
        return leave;
    }
}
