using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public record ApproveLeaveRequestCommand(Guid Id) : IRequest<ApproveLeaveRequestResultDto>;

/// <summary>
/// Duyệt đơn nghỉ. Ngoài việc đổi trạng thái, còn GỠ luôn các ca đã xếp cho người này trong khoảng
/// nghỉ — người đã được duyệt nghỉ mà vẫn đứng tên trong lịch làm việc thì lễ tân vẫn gom bệnh nhân
/// vào phòng đó. Sau khi gỡ, Owner được thông báo lại là lịch đang trống và cần bổ sung người.
/// </summary>
public class ApproveLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IWorkScheduleRepository workScheduleRepository,
    IAppointmentRepository appointmentRepository,
    IUserRepository userRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IRequestHandler<ApproveLeaveRequestCommand, ApproveLeaveRequestResultDto>
{
    public async Task<ApproveLeaveRequestResultDto> Handle(ApproveLeaveRequestCommand command, CancellationToken ct)
    {
        var request = await leaveRequestRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {command.Id}");

        // Đọc ảnh hưởng TRƯỚC khi đổi trạng thái: đây đúng là tập ca mà Owner vừa nhìn thấy ở màn
        // hình chi tiết, và cũng là tập sẽ bị xóa ngay sau đây.
        var affectedShifts = await LeaveImpactBuilder.GetAffectedShiftsAsync(request, workScheduleRepository, ct);
        var affectedAppointments = await LeaveImpactBuilder.GetAffectedAppointmentsAsync(request, appointmentRepository, ct);
        var affectedDates = affectedShifts.Select(s => s.Date).Distinct().OrderBy(d => d).ToList();

        request.Approve();
        await leaveRequestRepository.UpdateAsync(request, ct);

        // Chỉ xóa lịch sau khi đơn đã lưu trạng thái Approved — nếu Approve() ném lỗi (đơn đã xử lý)
        // thì lịch làm việc phải còn nguyên.
        await workScheduleRepository.RemoveRangeAsync(affectedShifts, ct);

        var staffName = LeaveImpactBuilder.ResolveStaffName(request);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Approve,
            module: ActivityModule.Leave,
            description: affectedShifts.Count > 0
                ? $"Duyệt đơn xin nghỉ ID: {command.Id} — gỡ {affectedShifts.Count} ca làm việc của {staffName}"
                : $"Duyệt đơn xin nghỉ ID: {command.Id}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: command.Id.ToString(),
            ct: ct);

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: request.UserId,
            Type: NotificationType.Schedule,
            Priority: NotificationPriority.Medium,
            Title: "Đơn xin nghỉ được phê duyệt",
            Body: affectedShifts.Count > 0
                ? $"Đơn xin nghỉ từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy} đã được duyệt. "
                  + $"{affectedShifts.Count} ca làm việc của bạn trong những ngày này đã được gỡ khỏi lịch."
                : $"Đơn xin nghỉ từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy} đã được duyệt.",
            RelatedEntityType: "LeaveRequest",
            RelatedEntityId: request.Id.ToString()), ct);

        if (affectedShifts.Count > 0)
            await NotifyOwnersAsync(request, staffName, affectedShifts.Count, affectedDates, affectedAppointments.Count, ct);

        return new ApproveLeaveRequestResultDto(
            GetLeaveRequestsHandler.ToDto(request),
            affectedShifts.Count,
            affectedDates.Count,
            affectedAppointments.Count,
            affectedDates);
    }

    /// <summary>
    /// Báo cho toàn bộ tài khoản Owner biết lịch vừa thủng chỗ nào. Gửi cho mọi Owner chứ không chỉ
    /// người vừa bấm duyệt — phòng khám có thể có nhiều chủ và ai cũng có quyền xếp lại lịch.
    /// RelatedEntityId là thứ Hai của tuần có ca đầu tiên bị gỡ, để bấm vào thông báo là mở thẳng
    /// màn hình chỉnh lịch đúng tuần đó.
    /// </summary>
    private async Task NotifyOwnersAsync(
        LeaveRequest request,
        string staffName,
        int removedShiftCount,
        IReadOnlyList<DateOnly> affectedDates,
        int appointmentCount,
        CancellationToken ct)
    {
        var ownerIds = await userRepository.GetUserIdsByRoleAsync(nameof(UserRole.Owner), ct);
        if (ownerIds.Count == 0) return;

        var firstDate = affectedDates[0];
        var weekStart = firstDate.AddDays(-(((int)firstDate.DayOfWeek + 6) % 7));

        var body =
            $"Đã duyệt đơn nghỉ của {staffName} ({request.StartDate:dd/MM} - {request.EndDate:dd/MM}) và gỡ "
            + $"{removedShiftCount} ca làm việc trong {affectedDates.Count} ngày. Các ca này đang TRỐNG, "
            + "cần phân công người thay thế.";

        if (appointmentCount > 0)
            body += $" Lưu ý: có {appointmentCount} lịch hẹn đã đặt trong những ngày này chưa được xử lý.";

        await notificationService.CreateForMultipleUsersAsync(
            ownerIds,
            new CreateNotificationRequest(
                UserId: Guid.Empty, // template — UserId thật do CreateForMultipleUsersAsync gán cho từng Owner
                Type: NotificationType.Schedule,
                Priority: NotificationPriority.High,
                Title: "Lịch làm việc bị trống, cần bổ sung",
                Body: body,
                RelatedEntityType: "WorkSchedule",
                RelatedEntityId: weekStart.ToString("yyyy-MM-dd")),
            ct);
    }
}
