using System.Text.Json;
using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

/// <summary>
/// Hàm mapping/truy vấn dùng chung giữa các handler ClinicalRecords sau khi các god-handler
/// (GetExaminationHandler / DiagnosisHandler / TreatmentPlanHandler / PrescriptionHandler) được
/// tách thành nhiều handler nhỏ. Để static nên không cần đăng ký DI.
/// </summary>
public static class ClinicalRecordMappers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string AppointmentCode(Appointment a) =>
        $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}";

    // ── Diagnosis ────────────────────────────────────────────────────────────

    public static DiagnosisDto ToDto(Diagnosis d) => new()
    {
        Id = d.Id,
        Description = d.Description,
        GumCondition = d.GumCondition,
        OralMucosaCondition = d.OralMucosaCondition,
        GumBleeding = d.GumBleeding,
        PainOnChewing = d.PainOnChewing,
        TeethCount = d.TeethCount,
        DecayedTeeth = d.DecayedTeeth,
        WornOrBrokenTeeth = d.WornOrBrokenTeeth,
        LooseTeeth = d.LooseTeeth,
        Tartar = d.Tartar,
        Plaque = d.Plaque,
        BadBreath = d.BadBreath,
        TmjSymptoms = d.TmjSymptoms,
        Occlusion = d.Occlusion,
        OcclusionDeviation = d.OcclusionDeviation,
        MedicalHistory = d.MedicalHistory,
        AllergyHistory = d.AllergyHistory,
        Conclusion = d.Conclusion,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };

    // ── Prescription ─────────────────────────────────────────────────────────

    public static PrescriptionDto ToDto(Prescription prescription) => new()
    {
        Id = prescription.Id,
        Notes = prescription.Notes,
        CreatedAt = prescription.CreatedAt,
        Items = prescription.Items.Select(i => new PrescriptionItemDto
        {
            Id = i.Id,
            MedicineName = i.MedicineName,
            Dosage = i.Dosage,
            Quantity = i.Quantity,
            Unit = i.Unit,
            Usage = i.Usage,
            Notes = i.Notes,
            TimesPerDay = i.TimesPerDay,
            DurationDays = i.DurationDays,
            StartDate = i.StartDate
        }).ToList()
    };

    // ── Treatment plan ───────────────────────────────────────────────────────

    public static List<StepProgressEntryDto> ParseStepProgress(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<StepProgressEntryDto>();
        try
        {
            return JsonSerializer.Deserialize<List<StepProgressEntryDto>>(json, JsonOptions) ?? new List<StepProgressEntryDto>();
        }
        catch (JsonException)
        {
            return new List<StepProgressEntryDto>();
        }
    }

    public static string SerializeStepProgress(List<StepProgressEntryDto> entries) =>
        JsonSerializer.Serialize(entries, JsonOptions);

    public static TreatmentPlanDto ToDto(TreatmentPlan tp, decimal amountPaid = 0, bool isInvoiced = false) => new()
    {
        Id = tp.Id,
        PatientId = tp.PatientId,
        DentistId = tp.DentistId,
        DentistName = tp.Dentist?.FullName ?? string.Empty,
        AppointmentId = tp.AppointmentId,
        ServiceId = tp.ServiceId,
        ServiceName = tp.Service?.Name ?? string.Empty,
        UnitPrice = tp.UnitPrice,
        Quantity = tp.Quantity,
        Teeth = tp.Teeth,
        Status = tp.Status.ToString(),
        WarrantyUntil = tp.WarrantyUntil,
        Notes = tp.Notes,
        TotalCost = tp.TotalCost,
        AmountPaid = amountPaid,
        IsInvoiced = isInvoiced,
        StepProgress = ParseStepProgress(tp.StepProgressJson),
        CreatedAt = tp.CreatedAt,
        CompletedAt = tp.CompletedAt
    };

    // ── Examination (phiếu khám tổng hợp) ────────────────────────────────────

    public static ExaminationDto ToExaminationDto(Appointment appointment)
    {
        return new ExaminationDto
        {
            AppointmentId = appointment.Id,
            AppointmentCode = AppointmentCode(appointment),
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
            Diagnoses = appointment.Diagnoses.Select(ToDto).ToList(),
            TreatmentPlans = appointment.TreatmentPlans
                .Select(tp => ToDto(tp))
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

    // ── Lịch sử khám ─────────────────────────────────────────────────────────

    public static List<MedicalHistoryDiagnosisDto> ToMedicalHistoryDiagnoses(Appointment a) =>
        a.Diagnoses.Select(d => new MedicalHistoryDiagnosisDto(
            d.Description,
            d.GumCondition,
            d.OralMucosaCondition,
            d.GumBleeding,
            d.PainOnChewing,
            d.TeethCount,
            d.DecayedTeeth,
            d.WornOrBrokenTeeth,
            d.LooseTeeth,
            d.Tartar,
            d.Plaque,
            d.BadBreath,
            d.TmjSymptoms,
            d.Occlusion,
            d.OcclusionDeviation,
            d.Conclusion,
            d.CreatedAt
        )).ToList();

    public static List<MedicalHistoryTreatmentPlanDto> ToMedicalHistoryTreatmentPlans(Appointment a) =>
        a.TreatmentPlans.Select(tp => new MedicalHistoryTreatmentPlanDto(
            string.IsNullOrWhiteSpace(tp.Teeth) ? tp.Service.Name : $"{tp.Service.Name} - Răng {tp.Teeth}",
            tp.Status.ToString(),
            tp.TotalCost
        )).ToList();

    public static List<MedicalHistoryPrescriptionItemDto> ToMedicalHistoryPrescriptionItems(Appointment a) =>
        a.Prescriptions.FirstOrDefault() != null
            ? a.Prescriptions.First().Items.Select(i => new MedicalHistoryPrescriptionItemDto(
                i.MedicineName,
                i.Dosage,
                i.Quantity,
                i.Unit,
                i.Usage,
                i.Notes
            )).ToList()
            : new List<MedicalHistoryPrescriptionItemDto>();
}
