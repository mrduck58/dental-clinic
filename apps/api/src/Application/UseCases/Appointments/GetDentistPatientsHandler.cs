using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record DentistPatientDto(
    Guid AppointmentId,
    string AppointmentCode,
    string PatientName,
    int Age,
    string Gender,
    string? Phone,
    DateTimeOffset AppointmentDate,
    string Status,
    string? ServiceName,
    string? Symptoms,
    bool IsNew,
    bool HasActiveTreatment); // Bệnh nhân đang giữa liệu trình (có plan "Đang thực hiện") → buổi này là tái khám

public record DentistPatientsResponse(
    DateOnly Date,
    int TotalWaiting,
    int TotalInProgress,
    int TotalCompleted,
    List<DentistPatientDto> Patients);

public class GetDentistPatientsHandler(AppDbContext dbContext)
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<DentistPatientsResponse> HandleAsync(Guid dentistId, DateOnly date, CancellationToken ct = default)
    {
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamDateStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, vietnamTz.BaseUtcOffset);
        var utcStart = vietnamDateStart.ToUniversalTime();
        var utcEnd = utcStart.AddDays(1);

        var appointments = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentistId &&
                        a.AppointmentDate >= utcStart &&
                        a.AppointmentDate < utcEnd &&
                        // Chỉ hiện bệnh nhân đã check-in trở đi (đồng nhất với hàng đợi).
                        (a.Status == AppointmentStatus.CheckedIn    ||
                         a.Status == AppointmentStatus.InProgress   ||
                         a.Status == AppointmentStatus.PendingPayment ||
                         a.Status == AppointmentStatus.Completed))
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);

        var activeTreatmentPatientIds = await GetActiveTreatmentPatientIdsAsync(
            appointments.Select(a => a.PatientId).Distinct().ToList(), dbContext, ct);

        var patients = appointments.Select(a => new DentistPatientDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.Patient.FullName,
            CalculateAge(a.Patient.DateOfBirth),
            a.Patient.Gender ?? "Khác",
            a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
            a.AppointmentDate,
            a.Status.ToString(),
            a.Service?.Name,
            a.Symptoms,
            IsNewPatient(a.PatientId),
            activeTreatmentPatientIds.Contains(a.PatientId)
        )).ToList();

        return new DentistPatientsResponse(
            date,
            appointments.Count(a => a.Status == AppointmentStatus.CheckedIn),
            appointments.Count(a => a.Status == AppointmentStatus.InProgress),
            appointments.Count(a => a.Status == AppointmentStatus.PendingPayment ||
                                   a.Status == AppointmentStatus.Completed),
            patients);
    }

    private static int CalculateAge(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return 0;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value > today.AddYears(-age)) age--;
        return age;
    }

    private bool IsNewPatient(Guid patientId)
    {
        return !dbContext.Appointments.Any(a => a.PatientId == patientId && a.Status == AppointmentStatus.Completed);
    }

    /// <summary>Các bệnh nhân đang giữa liệu trình điều trị (có plan "Đang thực hiện") — dùng đánh dấu buổi tái khám.</summary>
    public static async Task<HashSet<Guid>> GetActiveTreatmentPatientIdsAsync(
        List<Guid> patientIds, AppDbContext dbContext, CancellationToken ct)
    {
        if (patientIds.Count == 0) return new HashSet<Guid>();
        var ids = await dbContext.TreatmentPlans
            .Where(tp => patientIds.Contains(tp.PatientId) && tp.Status == TreatmentPlanStatus.InProgress)
            .Select(tp => tp.PatientId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }
}
