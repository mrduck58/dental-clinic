using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record GetMyScheduleQuery(Guid UserId, string WeekStart) : IRequest<IEnumerable<ScheduleEntryDto>>;

/// <summary>
/// Lịch làm việc của chính người dùng đang đăng nhập trong một tuần (chỉ xem) — dùng chung cho
/// Dentist và Staff, vì WorkSchedule vốn chỉ gắn nhân sự bằng tên hiển thị (StaffName), không phân
/// biệt theo vai trò. Lấy tên theo cùng quy tắc với <see cref="DentalClinic.API.Application.UseCases.LeaveRequests.LeaveImpactBuilder.ResolveStaffName"/>.
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
        var staffName = !string.IsNullOrWhiteSpace(user?.FullName) ? user!.FullName : user?.Email;
        if (string.IsNullOrWhiteSpace(staffName))
            return Enumerable.Empty<ScheduleEntryDto>();

        var end = start.AddDays(7);
        var entries = await workScheduleRepository.GetByStaffNameAndDateRangeAsync(staffName, start, end, ct);

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }
}
