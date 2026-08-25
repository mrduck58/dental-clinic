using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record SaveWeekScheduleCommand(string WeekStart, SaveWeekScheduleRequest Request) : IRequest<IEnumerable<ScheduleEntryDto>>;

public class SaveWeekScheduleHandler(
    IWorkScheduleRepository repo,
    IUserRepository userRepository,
    ILeaveRequestRepository leaveRequestRepository)
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

        await EnsureNoApprovedLeaveConflictsAsync(entries, ct);

        await repo.ReplaceWeekAsync(weekDate, entries, ct);

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }

    /// <summary>
    /// Chặn lưu nếu có dòng lịch trùng đúng (người, ngày, ca) với một đơn xin nghỉ đã Approved —
    /// nếu không, Owner vẫn xếp được ca cho người vừa được duyệt nghỉ đúng ca đó, và không có gì
    /// báo lại. So khớp theo tên đã chuẩn hoá (<see cref="StaffNameMatcher"/>), cùng cách WorkSchedule
    /// tự gán EmployeeId ở trên — LeaveRequest chỉ giữ UserId, không có liên kết cứng tới WorkSchedule.
    /// </summary>
    private async Task EnsureNoApprovedLeaveConflictsAsync(List<WorkSchedule> entries, CancellationToken ct)
    {
        var approvedShiftKeys = (await leaveRequestRepository.GetAllAsync(ct))
            .Where(r => r.Status == LeaveStatus.Approved)
            .SelectMany(r => r.Shifts.Select(sh => (
                NameKey: StaffNameMatcher.Key(!string.IsNullOrWhiteSpace(r.User?.FullName) ? r.User.FullName : r.User?.Email),
                sh.Date,
                ShiftKey: NormalizeShiftId(sh.ShiftId))))
            .Where(k => k.NameKey.Length > 0)
            .ToHashSet();

        if (approvedShiftKeys.Count == 0) return;

        var conflicts = entries
            .Where(e => !e.IsHoliday)
            .Select(e => new { Entry = e, NameKey = StaffNameMatcher.Key(e.StaffName), ShiftKey = NormalizeShiftId(e.Shift) })
            .Where(x => x.NameKey.Length > 0 && approvedShiftKeys.Contains((x.NameKey, x.Entry.Date, x.ShiftKey)))
            .OrderBy(x => x.Entry.Date).ThenBy(x => x.Entry.Shift)
            .ToList();

        if (conflicts.Count == 0) return;

        var lines = conflicts.Select(x =>
            $"{x.Entry.StaffName} — {x.Entry.Date:dd/MM/yyyy} ({x.Entry.Shift}): đã được duyệt nghỉ ca này.");
        throw new ValidationException(
            "Không thể lưu lịch vì các ca sau trùng với đơn xin nghỉ đã được duyệt:\n" + string.Join("\n", lines));
    }

    private static string NormalizeShiftId(string shift) => shift.Trim().Replace(" ", "").Replace("–", "-");

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
