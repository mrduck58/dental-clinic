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
        var user = MakeStaffUser(baseSalary: 10_000_000m, allowance: 1_000_000m, leaveAccrued: 12m);
        var leave = MakeApprovedLeave(user.Id, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4));

        var result = PayrollCalculator.Compute(user, [leave], 2026, 8);

        result.LeaveShifts.Should().Be(12); // 2 ngày × 6 ca/ngày
        result.ExceededShifts.Should().Be(0);
        result.Deduction.Should().Be(0);
        result.NetSalary.Should().Be(11_000_000m);
    }

    /// <summary>
    /// Vượt định mức phép bị trừ theo số ca công vượt (lương cơ bản / 156 ca công).
    /// </summary>
    [Test]
    public void Compute_LeaveExceedsAllowance_DeductsPerExceededDay()
    {
        var user = MakeStaffUser(baseSalary: 13_000_000m, allowance: 1_000_000m, leaveAccrued: 6m);
        var leave = MakeApprovedLeave(user.Id, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 12));

        var result = PayrollCalculator.Compute(user, [leave], 2026, 8);

        result.LeaveShifts.Should().Be(18); // 3 ngày × 6 ca/ngày
        result.ExceededShifts.Should().Be(12);
        result.Deduction.Should().Be(1_000_000m); // 12 ca × (13.000.000 / 156)
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

        PayrollCalculator.Compute(user, [leave], 2026, 8).LeaveShifts.Should().Be(12);
        PayrollCalculator.Compute(user, [leave], 2026, 7).LeaveShifts.Should().Be(12);
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

        result.LeaveShifts.Should().Be(0);
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
        var user = User.Create("d1", "d@test.com", "hash", UserRole.Dentist);
        var employee = Employee.Create(
            user.Id, "NS001",
            baseSalary: 30_000_000m, allowance: 2_000_000m, leaveAccrued: 1.5m);
        user.AttachEmployee(employee);
        DentistProfile.Create(employee.Id, "Chỉnh nha", "CCHN-001");

        var (baseSalary, allowance, leaveAccrued) = PayrollCalculator.ReadSalaryProfile(user);

        baseSalary.Should().Be(30_000_000m);
        allowance.Should().Be(2_000_000m);
        leaveAccrued.Should().Be(1.5m);
    }

    private static User MakeStaffUser(decimal? baseSalary, decimal? allowance, decimal? leaveAccrued)
    {
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nguyễn Văn A");
        var employee = Employee.Create(
            user.Id, "NV001",
            baseSalary: baseSalary, allowance: allowance, leaveAccrued: leaveAccrued);
        user.AttachEmployee(employee);
        return user;
    }

    private static LeaveRequest MakeApprovedLeave(Guid userId, DateOnly from, DateOnly to)
    {
        var leave = LeaveRequest.Create(userId, LeaveType.Annual, from, to, "Test");
        leave.Approve();
        return leave;
    }
}
