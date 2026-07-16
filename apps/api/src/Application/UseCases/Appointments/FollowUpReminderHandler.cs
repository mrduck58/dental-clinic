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
        // Liệu trình đang thực hiện + buổi hẹn nơi liệu trình được lập (gốc chuỗi điều trị)
        var activePlans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Where(tp => tp.Status == TreatmentPlanStatus.InProgress && tp.AppointmentId != null)
            .Select(tp => new { tp.PatientId, AppointmentId = tp.AppointmentId!.Value, ServiceName = tp.Service.Name })
            .ToListAsync(ct);

        if (activePlans.Count == 0) return new List<FollowUpDueDto>();

        var patientIds = activePlans.Select(p => p.PatientId).Distinct().ToList();

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Service)
            .Where(a => patientIds.Contains(a.PatientId))
            .ToListAsync(ct);

        var parentById = appointments.ToDictionary(a => a.Id, a => a.FollowUpFromAppointmentId);
        var rootsByPatient = activePlans
            .GroupBy(p => p.PatientId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.AppointmentId).ToHashSet());

        // Chuỗi tái khám của một buổi hẹn: chính nó + các buổi gốc phía trên (chặn vòng lặp)
        List<Guid> WalkUp(Guid id)
        {
            var chain = new List<Guid>();
            Guid? cursor = id;
            while (cursor is Guid c && !chain.Contains(c))
            {
                chain.Add(c);
                cursor = parentById.TryGetValue(c, out var next) ? next : null;
            }
            return chain;
        }

        var result = new List<FollowUpDueDto>();
        foreach (var g in appointments.GroupBy(a => a.PatientId))
        {
            if (!rootsByPatient.TryGetValue(g.Key, out var roots)) continue;

            // Ứng viên: các buổi đã kết thúc THUỘC CHUỖI điều trị của một liệu trình đang thực hiện.
            var candidates = g
                .Where(a => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment)
                .Select(a => new { Appointment = a, Chain = WalkUp(a.Id) })
                .Where(x => x.Chain.Any(roots.Contains))
                .ToList();

            // Mỗi chuỗi điều trị (nhóm theo buổi gốc trên cùng) một dòng riêng —
            // bệnh nhân có 2 chuỗi dở dang song song sẽ hiện cả 2.
            foreach (var chainGroup in candidates.GroupBy(x => x.Chain[^1]))
            {
                // Buổi gốc = buổi CUỐI chuỗi (sâu nhất) — không dựa vào ngày giờ, vì buổi tái khám
                // check-in tại quầy có thể mang giờ thực sớm hơn giờ hẹn của buổi trước.
                var original = chainGroup
                    .OrderByDescending(x => x.Chain.Count)
                    .ThenByDescending(x => x.Appointment.AppointmentDate)
                    .First().Appointment;

                // Đã check-in tái khám cho buổi gốc này (buổi con chưa kết thúc) → ẩn để tránh trùng
                if (g.Any(f => f.FollowUpFromAppointmentId == original.Id && f.Status != AppointmentStatus.Cancelled)) continue;

                // Chỉ liệt kê các dịch vụ đang thực hiện thuộc đúng chuỗi này
                var chainSet = WalkUp(original.Id).ToHashSet();
                var planNames = activePlans
                    .Where(p => p.PatientId == g.Key && chainSet.Contains(p.AppointmentId))
                    .Select(p => p.ServiceName)
                    .Distinct()
                    .ToList();

                result.Add(new FollowUpDueDto
                {
                    OriginalAppointmentId = original.Id,
                    PatientId = original.PatientId,
                    PatientName = original.Patient.FullName,
                    PatientPhone = original.Patient.PhoneNumber ?? original.Patient.User?.PhoneNumber,
                    Gender = original.Patient.Gender,
                    DentistName = original.Dentist.FullName,
                    ServiceName = original.Service?.Name,
                    OriginalAppointmentDate = original.AppointmentDate,
                    FollowUpDate = original.FollowUpDate,
                    FollowUpNote = original.FollowUpNote,
                    ActivePlans = planNames
                });
            }
        }

        return result.OrderBy(x => x.FollowUpDate ?? DateOnly.MaxValue).ToList();
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

        // Chuỗi tái khám của buổi gốc (đi ngược FollowUpFromAppointmentId)
        var chain = new List<Guid>();
        Guid? cursor = original.Id;
        while (cursor is Guid c && !chain.Contains(c))
        {
            chain.Add(c);
            cursor = await dbContext.Appointments
                .Where(a => a.Id == c)
                .Select(a => a.FollowUpFromAppointmentId)
                .FirstOrDefaultAsync(ct);
        }

        // Chuỗi này phải còn liệu trình đang thực hiện
        var hasActivePlan = await dbContext.TreatmentPlans.AnyAsync(tp =>
            tp.AppointmentId != null && chain.Contains(tp.AppointmentId.Value) &&
            tp.Status == TreatmentPlanStatus.InProgress, ct);
        if (!hasActivePlan)
            throw new ValidationException("Chuỗi điều trị này không còn liệu trình đang thực hiện.");

        // Chặn check-in tái khám lặp cho cùng một buổi gốc (buổi hủy không tính).
        // Các lịch hẹn/lượt khám khác của bệnh nhân là lần khám riêng — không ảnh hưởng.
        var alreadyCheckedIn = await dbContext.Appointments.AnyAsync(f =>
            f.FollowUpFromAppointmentId == originalAppointmentId && f.Status != AppointmentStatus.Cancelled, ct);
        if (alreadyCheckedIn)
            throw new ConflictException("Buổi hẹn này đã được check-in tái khám.");

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
