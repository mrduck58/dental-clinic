using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public class ExaminationDto
{
    public Guid AppointmentId { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public PatientBriefDto Patient { get; set; } = null!;
    public DentistBriefDto Dentist { get; set; } = null!;
    public string? ServiceName { get; set; }
    public DateTimeOffset AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Symptoms { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
    public bool IsFollowUpVisit { get; set; }
    // Chuỗi buổi hẹn gốc của lượt tái khám (đi ngược FollowUpFromAppointmentId) —
    // frontend dùng để chỉ hiển thị liệu trình thuộc đúng chuỗi đơn được tái khám.
    public List<Guid> RelatedAppointmentIds { get; set; } = new();
    public List<DiagnosisDto> Diagnoses { get; set; } = new();
    public List<TreatmentPlanDto> TreatmentPlans { get; set; } = new();
    public PrescriptionDto? Prescription { get; set; }
}

public class PatientBriefDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
}

public class DentistBriefDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class DiagnosisDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;   // Chẩn đoán
    // Tình trạng lợi – niêm mạc
    public string? GumCondition { get; set; }
    public string? OralMucosaCondition { get; set; }
    public string? GumBleeding { get; set; }
    public string? PainOnChewing { get; set; }
    // Tình trạng răng
    public string? TeethCount { get; set; }
    public string? DecayedTeeth { get; set; }
    public string? WornOrBrokenTeeth { get; set; }
    public string? LooseTeeth { get; set; }
    // Vệ sinh răng miệng
    public string? Tartar { get; set; }
    public string? Plaque { get; set; }
    public string? BadBreath { get; set; }
    // Khớp thái dương hàm / khớp cắn
    public string? TmjSymptoms { get; set; }
    public string? Occlusion { get; set; }
    public string? OcclusionDeviation { get; set; }
    // Tiền sử
    public string? MedicalHistory { get; set; }
    public string? AllergyHistory { get; set; }
    public string? Conclusion { get; set; }                   // Kết quả & kế hoạch điều trị
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class PrescriptionDto
{
    public Guid Id { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class PrescriptionItemDto
{
    public Guid Id { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? TimesPerDay { get; set; }
    public int? DurationDays { get; set; }
    public DateOnly? StartDate { get; set; }
}

public class GetExaminationHandler(AppDbContext dbContext)
{
    public async Task<ExaminationDto?> HandleAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Dentist)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment == null)
            return null;

        var dto = ToDto(appointment);
        dto.RelatedAppointmentIds = await GetFollowUpChainAsync(appointment.Id, ct);
        return dto;
    }

    /// <summary>
    /// Toàn bộ chuỗi tái khám của một buổi hẹn — cả buổi gốc (đi ngược FollowUpFromAppointmentId)
    /// LẪN các buổi tái khám sau nó (đi xuôi). Trước đây chỉ đi ngược nên xem từ buổi hẹn GỐC sẽ
    /// không thấy các liệu trình/đơn thuốc được ghi thêm ở các buổi TÁI KHÁM sau — chỉ xem từ buổi
    /// tái khám mới nhất mới thấy đủ. Dò 2 chiều tới khi không còn buổi hẹn mới nào (chặn vòng lặp).
    /// </summary>
    private async Task<List<Guid>> GetFollowUpChainAsync(Guid appointmentId, CancellationToken ct)
    {
        var chain = new HashSet<Guid> { appointmentId };
        bool added;
        do
        {
            added = false;

            var parents = await dbContext.Appointments
                .Where(a => chain.Contains(a.Id) && a.FollowUpFromAppointmentId != null)
                .Select(a => a.FollowUpFromAppointmentId!.Value)
                .ToListAsync(ct);
            foreach (var p in parents)
            {
                if (chain.Add(p)) added = true;
            }

            var children = await dbContext.Appointments
                .Where(a => a.FollowUpFromAppointmentId != null && chain.Contains(a.FollowUpFromAppointmentId.Value))
                .Select(a => a.Id)
                .ToListAsync(ct);
            foreach (var c in children)
            {
                if (chain.Add(c)) added = true;
            }
        } while (added);

        chain.Remove(appointmentId);
        return chain.ToList();
    }

    public static ExaminationDto ToDto(Appointment appointment)
    {
        return new ExaminationDto
        {
            AppointmentId = appointment.Id,
            AppointmentCode = $"DK{appointment.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}",
            Patient = new PatientBriefDto
            {
                Id = appointment.PatientId,
                FullName = appointment.Patient.FullName,
                PhoneNumber = appointment.Patient.PhoneNumber ?? appointment.Patient.User?.PhoneNumber,
                // Walk-in patients (có PhoneNumber trực tiếp trên Patient) không hiện email vì staff không thu thập
                Email = appointment.Patient.PhoneNumber == null ? appointment.Patient.User?.Email : null,
                DateOfBirth = appointment.Patient.DateOfBirth,
                Gender = appointment.Patient.Gender,
                Address = appointment.Patient.Address
            },
            Dentist = new DentistBriefDto
            {
                Id = appointment.DentistId,
                FullName = appointment.Dentist.FullName
            },
            ServiceName = appointment.Service?.Name,
            AppointmentDate = appointment.AppointmentDate,
            Status = appointment.Status.ToString(),
            Symptoms = appointment.Symptoms,
            Notes = appointment.Notes,
            StartTime = appointment.Status == AppointmentStatus.InProgress ? DateTimeOffset.UtcNow : null,
            FollowUpDate = appointment.FollowUpDate,
            FollowUpNote = appointment.FollowUpNote,
            IsFollowUpVisit = appointment.FollowUpFromAppointmentId != null,
            Diagnoses = appointment.Diagnoses.Select(DiagnosisHandler.ToDto).ToList(),
            TreatmentPlans = appointment.TreatmentPlans
                .Select(tp => TreatmentPlanHandler.ToDto(tp))
                .ToList(),
            Prescription = appointment.Prescriptions.FirstOrDefault() != null
                ? new PrescriptionDto
                {
                    Id = appointment.Prescriptions.First().Id,
                    Notes = appointment.Prescriptions.First().Notes,
                    CreatedAt = appointment.Prescriptions.First().CreatedAt,
                    Items = appointment.Prescriptions.First().Items.Select(i => new PrescriptionItemDto
                    {
                        Id = i.Id,
                        MedicineName = i.MedicineName,
                        Dosage = i.Dosage,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Usage = i.Usage,
                        Notes = i.Notes
                    }).ToList()
                }
                : null
        };
    }
}
