using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

/// <summary>
/// Đối chiếu một đơn xin nghỉ với lịch làm việc và lịch hẹn để trả lời câu hỏi "duyệt đơn này thì
/// mất những gì". Dùng chung cho màn hình xem trước (<see cref="GetLeaveRequestImpactHandler"/>)
/// và cho chính lúc duyệt (<see cref="ApproveLeaveRequestHandler"/>) — hai nơi phải nhìn thấy cùng
/// một tập ca, nếu không Owner sẽ duyệt một đằng còn hệ thống xóa một nẻo.
/// </summary>
public static class LeaveImpactBuilder
{
    /// <summary>
    /// Tên dùng để dò lịch làm việc. Lịch chỉ lưu tên hiển thị (WorkSchedules.StaffName) chứ không
    /// lưu khóa ngoại nhân sự, nên phải lấy đúng chuỗi mà màn hình xếp lịch đã ghi vào — cùng quy
    /// tắc với <see cref="GetLeaveRequestsHandler.ToDto"/>.
    /// </summary>
    public static string ResolveStaffName(LeaveRequest request) =>
        !string.IsNullOrWhiteSpace(request.User?.FullName)
            ? request.User.FullName
            : (request.User?.Email ?? string.Empty);

    /// <summary>
    /// Chuẩn hoá tên trước khi so khớp — nay dùng chung <see cref="StaffNameMatcher"/> với luồng
    /// đặt lịch tại quầy. Hai nơi từng có hai bản chuẩn hoá riêng, tức là cùng một cặp tên có thể
    /// khớp ở màn hình đơn nghỉ nhưng trượt ở lưới đặt lịch; giữ một bản là cách duy nhất để hai
    /// màn hình luôn nói cùng một chuyện.
    /// </summary>
    public static string NormalizeStaffName(string? raw) => StaffNameMatcher.Key(raw);

    /// <summary>Các ca sẽ bị gỡ nếu duyệt đơn: đúng những ca (ngày + mã ca) người nộp đơn đã chọn khi
    /// tạo đơn (<see cref="LeaveRequest.Shifts"/>) VÀ hiện đang thật sự được xếp cho họ trong lịch làm
    /// việc — một ca đã chọn nhưng bị Owner xếp lại/gỡ trước khi duyệt thì đơn giản là không còn gì để
    /// gỡ nữa, không báo lỗi. Trừ dấu nghỉ lễ (IsHoliday là bản ghi đánh dấu cả phòng khám đóng cửa,
    /// không thuộc về ai). Lọc theo tên ở bộ nhớ chứ không ở SQL vì phép chuẩn hoá tên trên không viết
    /// được thành truy vấn; một đơn nghỉ chỉ trải vài ngày nên số dòng đọc lên là nhỏ.</summary>
    public static async Task<IReadOnlyList<WorkSchedule>> GetAffectedShiftsAsync(
        LeaveRequest request,
        IWorkScheduleRepository workScheduleRepository,
        CancellationToken ct)
    {
        var target = NormalizeStaffName(ResolveStaffName(request));
        if (target.Length == 0) return [];

        var requestedShifts = request.Shifts
            .Select(s => (s.Date, ShiftId: s.ShiftId.Trim().Replace(" ", "").Replace("–", "-")))
            .ToHashSet();
        if (requestedShifts.Count == 0) return [];

        var shifts = await workScheduleRepository.GetByDateRangeAsync(request.StartDate, request.EndDate, ct);

        return shifts
            .Where(s => !s.IsHoliday
                && NormalizeStaffName(s.StaffName) == target
                && requestedShifts.Contains((s.Date, s.Shift.Trim().Replace(" ", "").Replace("–", "-"))))
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Shift)
            .ToList();
    }

    /// <summary>
    /// Lịch hẹn đã đặt của người nộp đơn trong khoảng nghỉ (chỉ có với bác sĩ — nhân viên hành chính
    /// không nhận lịch hẹn). Đây là thông tin CẢNH BÁO: duyệt đơn không tự hủy lịch hẹn, Owner phải
    /// tự dời hoặc đổi bác sĩ.
    /// </summary>
    public static async Task<IReadOnlyList<Appointment>> GetAffectedAppointmentsAsync(
        LeaveRequest request,
        IAppointmentRepository appointmentRepository,
        CancellationToken ct)
    {
        var dentistId = request.User?.Employee?.DentistProfile?.Id;
        if (dentistId is null) return [];

        var appointments = await appointmentRepository.GetActiveByDentistIdAsync(dentistId.Value, ct);

        return appointments
            .Where(a =>
            {
                var vnDate = DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTime(a.AppointmentDate, AppointmentStatusHelper.VietnamTz).DateTime);
                return vnDate >= request.StartDate && vnDate <= request.EndDate;
            })
            .OrderBy(a => a.AppointmentDate)
            .ToList();
    }

    /// <summary>
    /// Gom ca + lịch hẹn thành báo cáo theo từng ngày. Chỉ liệt kê ngày THỰC SỰ có ảnh hưởng —
    /// ngày nghỉ mà người đó vốn không có ca nào thì không cần Owner bận tâm.
    /// </summary>
    public static LeaveImpactDto Build(
        LeaveRequest request,
        IReadOnlyList<WorkSchedule> shifts,
        IReadOnlyList<Appointment> appointments)
    {
        var appointmentsByDate = appointments
            .GroupBy(a => DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(a.AppointmentDate, AppointmentStatusHelper.VietnamTz).DateTime))
            .ToDictionary(g => g.Key, g => g.ToList());

        var shiftsByDate = shifts.GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.ToList());

        var days = shiftsByDate.Keys
            .Union(appointmentsByDate.Keys)
            .OrderBy(d => d)
            .Select(date =>
            {
                var dayShifts = shiftsByDate.TryGetValue(date, out var s) ? s : [];
                var dayAppointments = appointmentsByDate.TryGetValue(date, out var a) ? a : [];

                return new LeaveImpactDayDto(
                    date,
                    dayShifts
                        .OrderBy(x => x.Shift)
                        .Select(x => new LeaveImpactShiftDto(x.Id, x.Shift, x.Room, x.Role, x.Type))
                        .ToList(),
                    dayAppointments.Count,
                    dayAppointments
                        .Select(x => TimeZoneInfo
                            .ConvertTime(x.AppointmentDate, AppointmentStatusHelper.VietnamTz)
                            .ToString("HH:mm"))
                        .ToList());
            })
            .ToList();

        return new LeaveImpactDto(
            request.Id,
            ResolveStaffName(request),
            request.Status.ToString(),
            request.StartDate,
            request.EndDate,
            days.Count(d => d.Shifts.Count > 0),
            shifts.Count,
            appointments.Count,
            days);
    }
}
