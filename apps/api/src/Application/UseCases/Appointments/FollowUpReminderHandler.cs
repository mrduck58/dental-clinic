using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record SetFollowUpReminderRequest(DateOnly FollowUpDate, string? Note);

public class FollowUpReminderDto
{
    public Guid AppointmentId { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
}

/// <summary>Một bệnh nhân đang trong diện chờ tái khám (còn liệu trình đang thực hiện sau khi kết thúc điều trị).</summary>
public class FollowUpDueDto
{
    public Guid OriginalAppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public DateTimeOffset OriginalAppointmentDate { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
    public List<string> ActivePlans { get; set; } = new(); // Các liệu trình đang thực hiện
}

/// <summary>
/// Nhắc tái khám: bác sĩ chỉ hẹn ngày khám lại (không đặt lịch mới).
/// Khi bác sĩ kết thúc điều trị, hệ thống gửi thông báo cho bệnh nhân (xem UpdateAppointmentStatusHandler).
/// </summary>
public class FollowUpReminderHandler(AppDbContext dbContext)
{
    public async Task<FollowUpReminderDto> SetAsync(Guid appointmentId, SetFollowUpReminderRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.InProgress)
            throw new ValidationException("Chỉ có thể hẹn tái khám khi buổi hẹn đang trong trạng thái đang khám.");

        if (request.FollowUpDate <= DateOnly.FromDateTime(DateTime.Today))
            throw new ValidationException("Ngày tái khám phải sau ngày hôm nay.");

        appointment.SetFollowUpReminder(request.FollowUpDate, string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim());
        await dbContext.SaveChangesAsync(ct);

        return ToDto(appointmentId, appointment.FollowUpDate, appointment.FollowUpNote);
    }

    public async Task<FollowUpReminderDto> ClearAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        appointment.SetFollowUpReminder(null, null);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(appointmentId, null, null);
    }

    /// <summary>
    /// Danh sách bệnh nhân đang chờ tái khám: bệnh nhân còn liệu trình "Đang thực hiện"
    /// sau khi đã kết thúc buổi điều trị trước. Bệnh nhân không cần đặt lịch lại —
    /// staff check-in trực tiếp từ danh sách này.
    /// Lịch đặt mới (Pending/Confirmed) là lần khám riêng, KHÔNG ảnh hưởng diện chờ tái khám;
    /// chỉ loại bệnh nhân đang có mặt trong phòng khám (đã check-in / đang khám) để tránh trùng.
    /// </summary>
    public async Task<List<FollowUpDueDto>> GetDueAsync(CancellationToken ct = default)
    {
        // Bệnh nhân còn liệu trình đang thực hiện + tên các dịch vụ đó
        var activePlans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Where(tp => tp.Status == TreatmentPlanStatus.InProgress)
            .Select(tp => new { tp.PatientId, ServiceName = tp.Service.Name })
            .ToListAsync(ct);

        if (activePlans.Count == 0) return new List<FollowUpDueDto>();

        var patientIds = activePlans.Select(p => p.PatientId).Distinct().ToList();
        var planNamesByPatient = activePlans
            .GroupBy(p => p.PatientId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ServiceName).Distinct().ToList());

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .Where(a => patientIds.Contains(a.PatientId))
            .ToListAsync(ct);

        return appointments
            .GroupBy(a => a.PatientId)
            // Đang trong phòng khám (đã check-in / đang khám) → không hiện để tránh check-in trùng
            .Where(g => !g.Any(a => a.Status == AppointmentStatus.CheckedIn || a.Status == AppointmentStatus.InProgress))
            // Buổi gốc = buổi điều trị gần nhất đã kết thúc
            .Select(g => g
                .Where(a => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment)
                .OrderByDescending(a => a.AppointmentDate)
                .FirstOrDefault())
            .Where(a => a != null)
            .Select(a => a!)
            .OrderBy(a => a.FollowUpDate ?? DateOnly.MaxValue)
            .Select(a => new FollowUpDueDto
            {
                OriginalAppointmentId = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
                Gender = a.Patient.Gender,
                DentistName = a.Dentist.FullName,
                ServiceName = a.Service?.Name,
                OriginalAppointmentDate = a.AppointmentDate,
                FollowUpDate = a.FollowUpDate,
                FollowUpNote = a.FollowUpNote,
                ActivePlans = planNamesByPatient.GetValueOrDefault(a.PatientId) ?? new List<string>()
            })
            .ToList();
    }

    /// <summary>
    /// Staff check-in bệnh nhân đến tái khám: tạo buổi hẹn mới đã check-in ngay,
    /// gắn về buổi gốc — bác sĩ sẽ thấy cờ tái khám và liệu trình cũ của bệnh nhân.
    /// </summary>
    public async Task<Guid> CheckInAsync(Guid originalAppointmentId, CancellationToken ct = default)
    {
        var original = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == originalAppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy buổi hẹn gốc.");

        var hasActivePlan = await dbContext.TreatmentPlans.AnyAsync(tp =>
            tp.PatientId == original.PatientId && tp.Status == TreatmentPlanStatus.InProgress, ct);
        if (!hasActivePlan)
            throw new ValidationException("Bệnh nhân không còn liệu trình đang thực hiện.");

        // Chỉ chặn khi bệnh nhân đang có mặt trong phòng khám (đã check-in / đang khám) — tránh trùng lượt.
        // Lịch đặt mới (Pending/Confirmed) là lần khám riêng, không ảnh hưởng việc check-in tái khám.
        var inClinic = await dbContext.Appointments.AnyAsync(o =>
            o.PatientId == original.PatientId &&
            (o.Status == AppointmentStatus.CheckedIn || o.Status == AppointmentStatus.InProgress), ct);
        if (inClinic)
            throw new ConflictException("Bệnh nhân đang có lượt khám trong phòng khám (đã check-in hoặc đang khám).");

        var followUpVisit = Appointment.CheckInFollowUp(
            original.Id,
            original.PatientId,
            original.DentistId,
            original.ServiceId,
            string.IsNullOrWhiteSpace(original.FollowUpNote) ? "Tái khám theo hẹn" : $"Tái khám: {original.FollowUpNote}");

        dbContext.Appointments.Add(followUpVisit);
        await dbContext.SaveChangesAsync(ct);

        return followUpVisit.Id;
    }

    private static FollowUpReminderDto ToDto(Guid appointmentId, DateOnly? date, string? note) => new()
    {
        AppointmentId = appointmentId,
        FollowUpDate = date,
        FollowUpNote = note
    };
}
