using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record TransferQueuePatientResponse(Guid AppointmentId, Guid DentistId, string DentistName, string RoomName);

/// <summary>
/// Chuyển một bệnh nhân đang chờ sang hàng đợi của phòng khác.
/// Phòng không phải thuộc tính của lịch hẹn — nó được suy ra từ bác sĩ phụ trách,
/// nên "chuyển phòng" thực chất là giao bệnh nhân cho bác sĩ đang trực ở phòng đích.
/// </summary>
public class TransferQueuePatientHandler(
    AppDbContext dbContext,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser)
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<TransferQueuePatientResponse> HandleAsync(
        Guid appointmentId, string roomName, Guid? dentistId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            throw new ValidationException("Thiếu tên phòng đích.");

        // Không Include(Dentist): nav đã nạp mà FK đổi thì EF ưu tiên nav và nuốt mất thay đổi.
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");

        if (appointment.Status != AppointmentStatus.CheckedIn)
            throw new ConflictException("Chỉ chuyển được bệnh nhân đang chờ khám.");

        var nowVietnam = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone);
        var today = DateOnly.FromDateTime(nowVietnam.DateTime);

        // Lễ tân chỉ định rõ bác sĩ (giao ca): giao đúng người đó nếu họ đang trực HOẶC sắp vào ca
        // tại phòng đích. Không chỉ định: tự giao cho bác sĩ đang trực (giữ hành vi cũ).
        var target = dentistId.HasValue
            ? await FindAssignableDentistAsync(roomName, dentistId.Value, today, nowVietnam, ct)
                ?? throw new ConflictException($"Bác sĩ được chọn không có ca hiện tại hoặc sắp tới tại {roomName}.")
            : await FindDentistOnShiftAsync(roomName, today, nowVietnam, ct)
                ?? throw new ConflictException($"Không có bác sĩ nào đang trong ca trực tại {roomName}.");

        if (appointment.DentistId == target.Id)
            return new TransferQueuePatientResponse(appointment.Id, target.Id, target.FullName, roomName);

        appointment.ReassignDentist(target.Id);
        // Chuyển phòng thì bệnh nhân xuống CUỐI hàng đợi phòng mới VÀ nhận số thứ tự mới ở cuối:
        // đặt cả vị trí hiển thị lẫn mốc đánh số = hiện tại (lớn hơn mọi mốc trước đó trong phòng).
        // CheckedInAt giữ nguyên nên thời gian chờ không reset.
        var nowTicks = DateTimeOffset.UtcNow.UtcTicks;
        appointment.SetQueueOrder(nowTicks);
        appointment.SetQueueEntryOrder(nowTicks);
        await dbContext.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Chuyển bệnh nhân của lịch hẹn {appointmentId} sang {roomName} (BS. phụ trách: {target.FullName})",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: target.UserId,
            Type: NotificationType.Appointment,
            Priority: NotificationPriority.High,
            Title: "Bệnh nhân được chuyển sang hàng đợi của bạn",
            Body: $"Một bệnh nhân đã được chuyển sang {roomName}. Vui lòng kiểm tra hàng đợi.",
            RelatedEntityType: "Appointment",
            RelatedEntityId: appointmentId.ToString()), ct);

        return new TransferQueuePatientResponse(appointment.Id, target.Id, target.FullName, roomName);
    }

    /// <summary>
    /// Bác sĩ đang trực phòng <paramref name="roomName"/> tại thời điểm này: có ca bao trùm
    /// giờ hiện tại và tài khoản còn Active. Nhiều người cùng ca thì lấy theo thứ tự tên.
    /// </summary>
    private async Task<Dentist?> FindDentistOnShiftAsync(
        string roomName, DateOnly today, DateTimeOffset nowVietnam, CancellationToken ct)
    {
        var validShiftCodes = WorkShifts.AllValidCodes;
        var roomSchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == today && ws.Type == "dentist" && !ws.IsHoliday &&
                         ws.Room == roomName && validShiftCodes.Contains(ws.Shift))
            .ToListAsync(ct);

        var onShiftNames = roomSchedules
            .Where(ws => WorkShifts.IsWorkingAt([ws.Shift], nowVietnam.Hour, nowVietnam.Minute))
            .Select(ws => ws.StaffName)
            .ToHashSet();

        if (onShiftNames.Count == 0) return null;

        var dentists = await dbContext.Dentists.Include(d => d.User).ToListAsync(ct);

        return dentists
            .Where(d => onShiftNames.Contains(d.FullName) &&
                        string.Equals(d.EmploymentStatus, User.DefaultEmploymentStatus,
                                      StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.FullName, StringComparer.CurrentCulture)
            .FirstOrDefault();
    }

    /// <summary>
    /// Bác sĩ <paramref name="dentistId"/> do lễ tân chọn có được phép nhận bệnh nhân tại phòng
    /// <paramref name="roomName"/> lúc này không: phải có ca trong ngày tại phòng đó, ca đang bao
    /// trùm hiện tại HOẶC sắp bắt đầu trong cửa sổ giao ca, và tài khoản còn Active. Chặn việc giao
    /// cho bác sĩ ca đã tan hoặc còn quá xa để khớp đúng những gì hàng đợi hiển thị cho lễ tân.
    /// </summary>
    private async Task<Dentist?> FindAssignableDentistAsync(
        string roomName, Guid dentistId, DateOnly today, DateTimeOffset nowVietnam, CancellationToken ct)
    {
        var validShiftCodes = WorkShifts.AllValidCodes;
        var nowMinutes = nowVietnam.Hour * 60 + nowVietnam.Minute;

        var roomSchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == today && ws.Type == "dentist" && !ws.IsHoliday &&
                         ws.Room == roomName && validShiftCodes.Contains(ws.Shift))
            .ToListAsync(ct);

        var assignableNames = roomSchedules
            .Where(ws => WorkShifts.IsWorkingAt([ws.Shift], nowVietnam.Hour, nowVietnam.Minute) ||
                         WorkShifts.StartsWithinMinutes(ws.Shift, nowMinutes, WorkShifts.ShiftHandoverWindowMinutes))
            .Select(ws => ws.StaffName)
            .ToHashSet();

        if (assignableNames.Count == 0) return null;

        var dentists = await dbContext.Dentists.Include(d => d.User).ToListAsync(ct);

        return dentists.FirstOrDefault(d =>
            d.Id == dentistId &&
            assignableNames.Contains(d.FullName) &&
            string.Equals(d.EmploymentStatus, User.DefaultEmploymentStatus,
                          StringComparison.OrdinalIgnoreCase));
    }
}
