using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record GetMyScheduleQuery(Guid UserId, string WeekStart) : IRequest<IEnumerable<ScheduleEntryDto>>;

/// <summary>
/// Lịch làm việc của chính người dùng đang đăng nhập trong một tuần (chỉ xem) — dùng chung cho
/// Dentist và Staff.
/// </summary>
public class GetMyScheduleHandler(
    IUserRepository userRepository,
    IWorkScheduleRepository workScheduleRepository) : IRequestHandler<GetMyScheduleQuery, IEnumerable<ScheduleEntryDto>>
{
    public async Task<IEnumerable<ScheduleEntryDto>> Handle(GetMyScheduleQuery query, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(query.WeekStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        var user = await userRepository.GetByIdAsync(query.UserId, ct);
        if (user == null)
            return Enumerable.Empty<ScheduleEntryDto>();

        var employeeId = user.Employee?.Id;
        var staffName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email;
        if (employeeId == null && string.IsNullOrWhiteSpace(staffName))
            return Enumerable.Empty<ScheduleEntryDto>();

        // Nối lịch với người dùng qua EmployeeId — khóa THẬT (xem WorkSchedule.Create). StaffName chỉ
        // còn là lưới an toàn cho dòng cũ/nhập tay chưa gán được EmployeeId: so khớp exact string trước
        // đây khiến người dùng "biến mất" khỏi lịch của chính mình khi hồ sơ ghi khác chữ (vd
        // "BS. Đào Tuấn Anh" ở bảng xếp lịch nhưng "Đào Tuấn Anh" ở tài khoản) — cùng lỗi đã sửa ở
        // GetWaitingQueueHandler, StaffNameMatcher.Key bỏ qua chức danh/khoảng trắng thừa.
        var end = start.AddDays(6);
        var candidates = await workScheduleRepository.GetByDateRangeAsync(start, end, ct);
        var entries = candidates
            .Where(s =>
                (employeeId != null && s.EmployeeId == employeeId) ||
                (s.EmployeeId == null && StaffNameMatcher.IsSamePerson(s.StaffName, staffName)))
            .OrderBy(s => s.Date)
            .ToList();

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }
}
