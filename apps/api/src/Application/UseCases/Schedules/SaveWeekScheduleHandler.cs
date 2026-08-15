using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record SaveWeekScheduleCommand(string WeekStart, SaveWeekScheduleRequest Request) : IRequest<IEnumerable<ScheduleEntryDto>>;

public class SaveWeekScheduleHandler(IWorkScheduleRepository repo, IUserRepository userRepository)
    : IRequestHandler<SaveWeekScheduleCommand, IEnumerable<ScheduleEntryDto>>
{
    public async Task<IEnumerable<ScheduleEntryDto>> Handle(SaveWeekScheduleCommand command, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(command.WeekStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var weekDate))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        // Gán nhân sự cho từng dòng NGAY LÚC LƯU. Làm ở đây chứ không phải lúc đọc vì đây là thời
        // điểm duy nhất còn biết chắc người xếp lịch đang nói tới ai; để đến lúc đọc thì chỉ còn
        // một chuỗi tên và phải đoán lại mỗi lần.
        var employeeIdByNameKey = await BuildEmployeeIndexAsync(ct);

        var entries = command.Request.Entries.Select(e =>
        {
            if (!DateOnly.TryParseExact(e.Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                throw new ArgumentException($"Invalid entry date: {e.Date}");

            // Không khớp ai thì vẫn lưu (employeeId = null): dòng nhập từ Excel có thể ghi sai tên,
            // giấu nó đi chỉ khiến người xếp lịch tưởng đã phân ca xong.
            employeeIdByNameKey.TryGetValue(StaffNameMatcher.Key(e.Name), out var employeeId);

            return WorkSchedule.Create(
                d, e.Shift, e.Type, e.Role, e.Name, e.Room, e.RoomColor, e.IsHoliday,
                employeeId == Guid.Empty ? null : employeeId);
        }).ToList();

        await repo.ReplaceWeekAsync(weekDate, entries, ct);

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }

    /// <summary>
    /// Bảng tra "tên đã chuẩn hoá → nhân sự". Tên trùng nhau sau khi chuẩn hoá bị loại khỏi bảng:
    /// hai người cùng tên thì đoán bừa một người còn tệ hơn để trống, vì dòng lịch sẽ gắn nhầm
    /// vào bác sĩ khác mà không ai nhận ra.
    /// </summary>
    private async Task<Dictionary<string, Guid>> BuildEmployeeIndexAsync(CancellationToken ct)
    {
        var users = await userRepository.GetAllAsync(ct);

        return users
            .Where(u => u.Employee != null)
            .Select(u => new { Key = StaffNameMatcher.Key(u.FullName), EmployeeId = u.Employee!.Id })
            .Where(x => x.Key.Length > 0)
            .GroupBy(x => x.Key)
            .Where(g => g.Select(x => x.EmployeeId).Distinct().Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().EmployeeId);
    }
}
