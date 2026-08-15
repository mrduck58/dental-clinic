using DentalClinic.API.Application.DTOs.Notifications;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Notifications;

public record GetNotificationsQuery(
    Guid UserId,
    string? Type = null,
    string? Priority = null,
    bool? IsRead = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 10,
    string? SortDir = null) : IRequest<NotificationPagedDto>;

public class GetNotificationsHandler(
    INotificationRepository repository,
    IPatientRepository? patientRepository = null,
    IAppointmentRepository? appointmentRepository = null) : IRequestHandler<GetNotificationsQuery, NotificationPagedDto>
{
    public async Task<NotificationPagedDto> Handle(GetNotificationsQuery query, CancellationToken ct)
    {
        if (patientRepository != null && appointmentRepository != null)
        {
            await EnsureAppointmentRemindersCreatedAsync(query.UserId, ct);
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page     = Math.Max(query.Page, 1);

        var (items, total) = await repository.GetPagedAsync(
            query.UserId,
            query.Type,
            query.Priority,
            query.IsRead,
            query.Search,
            page,
            pageSize,
            query.SortDir,
            ct);

        var unreadCount = await repository.GetUnreadCountAsync(query.UserId, ct);

        var dtos = items.Select(n => new NotificationDto(
            n.Id,
            n.Type,
            n.Priority,
            n.Title,
            n.Body,
            n.IsRead,
            n.ReadAt,
            n.RelatedEntityType,
            n.RelatedEntityId,
            n.CreatedAt)).ToList();

        return new NotificationPagedDto(
            dtos,
            total,
            page,
            pageSize,
            (int)Math.Ceiling((double)total / pageSize),
            unreadCount);
    }

    private async Task EnsureAppointmentRemindersCreatedAsync(Guid userId, CancellationToken ct)
    {
        if (patientRepository == null || appointmentRepository == null) return;
        try
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var vnNow = nowUtc.AddHours(7);
            var todayDate = DateOnly.FromDateTime(vnNow.DateTime);

            var patient = await patientRepository.GetByUserIdAsync(userId, ct);
            var patientId = patient?.Id;

            var upcomingApps = await appointmentRepository.GetActiveByPatientOrUserAsync(patientId, userId, ct);

            if (upcomingApps.Count == 0) return;

            var existingNotificationTitles = await repository.GetAppointmentReminderKeysAsync(userId, ct);

            var existingKeys = existingNotificationTitles
                .Select(n => $"{n.Title}|{n.RelatedEntityId}")
                .ToHashSet();

            var newNotifications = new List<Notification>();

            foreach (var app in upcomingApps)
            {
                var appVnTime = app.AppointmentDate.UtcDateTime.AddHours(7);
                var appDate = DateOnly.FromDateTime(appVnTime);
                var dentistName = app.Dentist?.FullName ?? "Nha sĩ";
                var appEntityId = app.Id.ToString();

                // 1. Nhắc nhở: Đến ngày booking
                if (todayDate == appDate)
                {
                    var dayTitle = "Lịch khám hôm nay";
                    var dayKey = $"{dayTitle}|{appEntityId}";
                    if (!existingKeys.Contains(dayKey))
                    {
                        newNotifications.Add(Notification.Create(
                            userId,
                            NotificationType.Appointment,
                            NotificationPriority.High,
                            dayTitle,
                            $"Hôm nay ({appVnTime:dd/MM/yyyy}) bạn có lịch hẹn khám với Bác sĩ {dentistName} lúc {appVnTime:HH:mm}. Vui lòng chuẩn bị đến đúng giờ.",
                            "Appointment",
                            appEntityId));
                        existingKeys.Add(dayKey);
                    }
                }

                // 2. Nhắc nhở: Trước booking 1 tiếng
                var minutesDiff = (appVnTime - vnNow).TotalMinutes;
                if (minutesDiff >= -30 && minutesDiff <= 90)
                {
                    var hourTitle = "Sắp đến giờ khám (trước 1 tiếng)";
                    var hourKey = $"{hourTitle}|{appEntityId}";
                    if (!existingKeys.Contains(hourKey))
                    {
                        newNotifications.Add(Notification.Create(
                            userId,
                            NotificationType.Appointment,
                            NotificationPriority.High,
                            hourTitle,
                            $"Lịch khám với Bác sĩ {dentistName} của bạn diễn ra lúc {appVnTime:HH:mm} (còn khoảng 1 tiếng nữa). Vui lòng di chuyển đến phòng khám.",
                            "Appointment",
                            appEntityId));
                        existingKeys.Add(hourKey);
                    }
                }
            }

            if (newNotifications.Count > 0)
            {
                await repository.AddRangeAsync(newNotifications, ct);
            }
        }
        catch
        {
            // Bỏ qua lỗi sinh thông báo tự động
        }
    }
}
