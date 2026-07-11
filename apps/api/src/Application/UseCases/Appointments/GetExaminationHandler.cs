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
    public string DiagnosisCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    // Các trường khám lâm sàng
    public decimal? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? BloodPressureSystolic { get; set; }
    public decimal? BloodPressureDiastolic { get; set; }
    public string? MedicalHistory { get; set; }
    public string? AllergyHistory { get; set; }
    public string? DentalCondition { get; set; }
    public string? Conclusion { get; set; }
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
}

public class GetExaminationHandler(AppDbContext dbContext)
{
    public async Task<ExaminationDto?> HandleAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Dentist)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment == null)
            return null;

        return ToDto(appointment);
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
            Diagnoses = appointment.Diagnoses.Select(d => new DiagnosisDto
            {
                Id = d.Id,
                DiagnosisCode = d.DiagnosisCode,
                Description = d.Description,
                Notes = d.Notes,
                HeartRate = d.HeartRate,
                Temperature = d.Temperature,
                BloodPressureSystolic = d.BloodPressureSystolic,
                BloodPressureDiastolic = d.BloodPressureDiastolic,
                MedicalHistory = d.MedicalHistory,
                AllergyHistory = d.AllergyHistory,
                DentalCondition = d.DentalCondition,
                Conclusion = d.Conclusion,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList(),
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
