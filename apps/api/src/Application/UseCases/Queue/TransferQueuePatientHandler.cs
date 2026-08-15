using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Queue;

public record TransferQueuePatientResponse(Guid AppointmentId, Guid DentistId, string DentistName, string RoomName);

public record TransferQueuePatientCommand(Guid AppointmentId, string RoomName, Guid? DentistId = null)
    : IRequest<TransferQueuePatientResponse>;

/// <summary>
/// Chuyển một bệnh nhân đang chờ sang hàng đợi của phòng khác.
/// Phòng không phải thuộc tính của lịch hẹn — nó được suy ra từ bác sĩ phụ trách,
/// nên "chuyển phòng" thực chất là giao bệnh nhân cho bác sĩ đang trực ở phòng đích.
/// </summary>
public class TransferQueuePatientHandler(
    IAppointmentRepository appointmentRepository,
    IWorkScheduleRepository workScheduleRepository,
    IDentistRepository dentistRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IRequestHandler<TransferQueuePatientCommand, TransferQueuePatientResponse>
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<TransferQueuePatientResponse> Handle(TransferQueuePatientCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;
        var roomName = command.RoomName;
        var dentistId = command.DentistId;

        if (string.IsNullOrWhiteSpace(roomName))
            throw new ValidationException("Thiếu tên phòng đích.");

        // Không Include(Dentist): nav đã nạp mà FK đổi thì EF ưu tiên nav và nuốt mất thay đổi.
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
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
        await appointmentRepository.UpdateAsync(appointment, ct);

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
            UserId: target.Employee.UserId,
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
    private async Task<DentistProfile?> FindDentistOnShiftAsync(
        string roomName, DateOnly today, DateTimeOffset nowVietnam, CancellationToken ct)
    {
        var roomSchedules = await workScheduleRepository.GetDentistSchedulesForDateAsync(today, roomName, ct);

        var onShiftSchedules = roomSchedules
            .Where(ws => WorkShifts.IsWorkingAt([ws.Shift], nowVietnam.Hour, nowVietnam.Minute))
            .ToList();

        if (onShiftSchedules.Count == 0) return null;

        var dentists = await dentistRepository.GetAllWithUserAsync(ct);

        return dentists
            .Where(d => IsEmployed(d.Employee.EmploymentStatus) &&
                        onShiftSchedules.Any(ws => MatchesDentist(ws, d)))
            .OrderBy(d => d.FullName, StringComparer.CurrentCulture)
            .FirstOrDefault();
    }

    /// <summary>
    /// Bác sĩ <paramref name="dentistId"/> do lễ tân chọn có được phép nhận bệnh nhân tại phòng
    /// <paramref name="roomName"/> lúc này không: phải có ca trong ngày tại phòng đó, ca đang bao
    /// trùm hiện tại HOẶC sắp bắt đầu trong cửa sổ giao ca, và tài khoản còn Active. Chặn việc giao
    /// cho bác sĩ ca đã tan hoặc còn quá xa để khớp đúng những gì hàng đợi hiển thị cho lễ tân.
    /// </summary>
    private async Task<DentistProfile?> FindAssignableDentistAsync(
        string roomName, Guid dentistId, DateOnly today, DateTimeOffset nowVietnam, CancellationToken ct)
    {
        var nowMinutes = nowVietnam.Hour * 60 + nowVietnam.Minute;

        var roomSchedules = await workScheduleRepository.GetDentistSchedulesForDateAsync(today, roomName, ct);

        var assignableSchedules = roomSchedules
            .Where(ws => WorkShifts.IsWorkingAt([ws.Shift], nowVietnam.Hour, nowVietnam.Minute) ||
                         WorkShifts.StartsWithinMinutes(ws.Shift, nowMinutes, WorkShifts.ShiftHandoverWindowMinutes))
            .ToList();

        if (assignableSchedules.Count == 0) return null;

        var dentists = await dentistRepository.GetAllWithUserAsync(ct);

        return dentists.FirstOrDefault(d =>
            d.Id == dentistId &&
            IsEmployed(d.Employee.EmploymentStatus) &&
            assignableSchedules.Any(ws => MatchesDentist(ws, d)));
    }

    private static bool MatchesDentist(WorkSchedule ws, DentistProfile dentist)
    {
        if (ws.EmployeeId.HasValue)
            return ws.EmployeeId.Value == dentist.EmployeeId;

        return StaffNameMatcher.IsSamePerson(ws.StaffName, dentist.FullName);
    }

    /// <summary>Bác sĩ còn đang làm việc: EmploymentStatus phải là "Active".</summary>
    private static bool IsEmployed(string employmentStatus) =>
        string.Equals(employmentStatus, Employee.DefaultEmploymentStatus, StringComparison.OrdinalIgnoreCase);
}
