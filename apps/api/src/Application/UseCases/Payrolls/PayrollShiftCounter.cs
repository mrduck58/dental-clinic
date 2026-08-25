using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>
/// Đếm số ca đã được phân lịch (WorkSchedule) cho từng nhân sự trong một khoảng ngày — dùng làm
/// "RequiredShifts" khi tính lương (xem <see cref="PayrollCalculator"/>). Bỏ qua các ngày nghỉ lễ
/// (IsHoliday) và các dòng chưa gắn đúng nhân sự (EmployeeId null — ví dụ nhập từ Excel không khớp tên).
/// </summary>
public static class PayrollShiftCounter
{
    public static async Task<Dictionary<Guid, int>> CountByEmployeeAsync(
        IWorkScheduleRepository workScheduleRepository, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var schedules = await workScheduleRepository.GetByDateRangeAsync(from, to, ct);
        return schedules
            .Where(s => !s.IsHoliday && s.EmployeeId.HasValue)
            .GroupBy(s => s.EmployeeId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
