using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record GetMyScheduleQuery(Guid UserId, string WeekStart) : IRequest<IEnumerable<ScheduleEntryDto>>;

/// <summary>
/// Lịch làm việc của chính nha sĩ đang đăng nhập trong một tuần (chỉ xem).
/// Lọc WorkSchedules theo tên nha sĩ (StaffName) — khớp cách DentistDashboard xác định ca.
/// </summary>
public class GetMyScheduleHandler(
    IDentistRepository dentistRepository,
    IWorkScheduleRepository workScheduleRepository) : IRequestHandler<GetMyScheduleQuery, IEnumerable<ScheduleEntryDto>>
{
    public async Task<IEnumerable<ScheduleEntryDto>> Handle(GetMyScheduleQuery query, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(query.WeekStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        var dentist = await dentistRepository.GetByUserIdWithUserAsync(query.UserId, ct);
        if (dentist == null)
            return Enumerable.Empty<ScheduleEntryDto>();

        var end = start.AddDays(7);
        var entries = await workScheduleRepository.GetByStaffNameAndDateRangeAsync(dentist.FullName, start, end, ct);

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }
}
