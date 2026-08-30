using System.Text.Json;
using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

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

    // ── Appointment photo ────────────────────────────────────────────────────

    public static AppointmentPhotoDto ToDto(AppointmentPhoto p) => new()
    {
        Id = p.Id,
        AppointmentId = p.AppointmentId,
        Section = p.Section,
        Url = p.Url,
        Note = p.Note,
        UploadedBy = p.UploadedBy,
        CreatedAt = p.CreatedAt
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

    // ── Treatment Sessions & Items ───────────────────────────────────────────

    public static TreatmentSessionDto ToDto(TreatmentSession s) => new()
    {
        Id = s.Id,
        TreatmentPlanItemId = s.TreatmentPlanItemId,
        TreatmentProcedureId = s.TreatmentProcedureId,
        SessionNumber = s.SessionNumber,
        Name = s.Name,
        Status = s.Status.ToString(),
        DurationMinutes = s.DurationMinutes,
        DentistId = s.DentistId,
        DentistName = s.Dentist?.FullName ?? string.Empty,
        PerformedAt = s.PerformedAt,
        Note = s.Note,
        CreatedAt = s.CreatedAt
    };

    public static TreatmentPlanItemDto ToDto(
        TreatmentPlanItem item,
        IEnumerable<int>? procedureStepNumbers = null)
    {
        var distinctSessions = item.Sessions.DistinctBy(s => s.Id).OrderBy(s => s.SessionNumber).ToList();
        var sessionDtos = distinctSessions.Select(ToDto).ToList();
        var stepEntries = distinctSessions.Select(s => new StepProgressEntryDto
        {
            Id = s.Id,
            StepNumber = s.SessionNumber,
            StepName = s.Name,
            Percent = s.Percent > 0 ? s.Percent : (s.Status == TreatmentSessionStatus.Completed ? 100 : (s.Status == TreatmentSessionStatus.InProgress ? 50 : 0)),
            Date = s.PerformedAt.HasValue ? DateOnly.FromDateTime(s.PerformedAt.Value.Date) : DateOnly.FromDateTime(s.CreatedAt.Date),
            DentistName = s.Dentist?.FullName ?? string.Empty,
            Note = s.Note
        }).ToList();

        var total = procedureStepNumbers != null && procedureStepNumbers.Any()
            ? procedureStepNumbers.Count()
            : distinctSessions.Count;
        var completed = distinctSessions.Count(s => s.Percent >= 100 || s.Status == TreatmentSessionStatus.Completed);
        var percent = total > 0 ? (int)Math.Round((double)distinctSessions.Sum(s => s.Percent > 0 ? s.Percent : (s.Status == TreatmentSessionStatus.Completed ? 100 : 0)) / total) : 0;
        if (percent > 100) percent = 100;

        return new TreatmentPlanItemDto
        {
            Id = item.Id,
            TreatmentPlanId = item.TreatmentPlanId,
            ServiceId = item.ServiceId,
            ServiceName = item.Service?.Name ?? string.Empty,
            ServiceOptionId = item.ServiceOptionId,
            ServiceOptionName = item.ServiceOptionName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            Teeth = item.Teeth,
            Status = item.Status.ToString(),
            WarrantyUntil = item.WarrantyUntil,
            Notes = item.Notes,
            TotalCost = item.TotalCost,
            Sessions = sessionDtos,
            StepProgress = stepEntries,
            TotalSteps = total,
            CompletedSteps = completed,
            ProgressPercent = percent,
            CreatedAt = item.CreatedAt,
            CompletedAt = item.CompletedAt
        };
    }

    public static TreatmentPlanDto ToDto(
        TreatmentPlan tp,
        decimal amountPaid = 0,
        bool isInvoiced = false,
        IEnumerable<int>? procedureStepNumbers = null)
    {
        var itemDtos = tp.Items.Select(i => ToDto(i, procedureStepNumbers)).ToList();
        var primaryItem = itemDtos.FirstOrDefault();

        var allSessions = itemDtos.SelectMany(i => i.Sessions).DistinctBy(s => s.Id).OrderBy(s => s.SessionNumber).ToList();
        var totalSteps = itemDtos.Sum(i => i.TotalSteps);
        var completedSteps = itemDtos.Sum(i => i.CompletedSteps);
        var progressPercent = itemDtos.Count > 0 ? (int)Math.Round(itemDtos.Average(i => i.ProgressPercent)) : 0;

        return new TreatmentPlanDto
        {
            Id = tp.Id,
            PatientId = tp.PatientId,
            DentistId = tp.DentistId,
            DentistName = tp.Dentist?.FullName ?? string.Empty,
            AppointmentId = tp.AppointmentId,
            Title = tp.Title,
            Status = tp.Status.ToString(),
            Notes = tp.Notes,
            TotalCost = tp.TotalCost,
            AmountPaid = amountPaid,
            IsInvoiced = isInvoiced,
            Items = itemDtos,

            // Thuộc tính tương thích ngược cho DTO cũ
            ServiceId = primaryItem?.ServiceId ?? Guid.Empty,
            ServiceName = primaryItem?.ServiceName ?? (itemDtos.Count > 0 ? string.Join(", ", itemDtos.Select(i => i.ServiceName)) : tp.Title),
            ServiceOptionName = primaryItem?.ServiceOptionName,
            UnitPrice = primaryItem?.UnitPrice ?? 0,
            Quantity = primaryItem?.Quantity ?? 1,
            Teeth = primaryItem?.Teeth,
            WarrantyUntil = primaryItem?.WarrantyUntil,
            StepProgress = itemDtos.SelectMany(i => i.StepProgress).DistinctBy(s => s.Id).OrderBy(s => s.StepNumber).ToList(),
            TotalSteps = totalSteps,
            CompletedSteps = completedSteps,
            ProgressPercent = progressPercent,

            CreatedAt = tp.CreatedAt,
            CompletedAt = tp.CompletedAt
        };
    }

    public static AppointmentSessionDto ToDto(AppointmentSession ase) => new()
    {
        Id = ase.Id,
        AppointmentId = ase.AppointmentId,
        TreatmentSessionId = ase.TreatmentSessionId,
        SessionName = ase.TreatmentSession?.Name ?? string.Empty,
        ServiceName = ase.TreatmentSession?.TreatmentPlanItem?.Service?.Name ?? string.Empty,
        Teeth = ase.TreatmentSession?.TreatmentPlanItem?.Teeth,
        Sequence = ase.Sequence,
        DurationMinutes = ase.DurationMinutes,
        Status = ase.TreatmentSession?.Status.ToString() ?? "Planned",
        Note = ase.Note
    };

    public static FollowUpDto ToDto(FollowUp f) => new()
    {
        Id = f.Id,
        PatientId = f.PatientId,
        PatientName = f.Patient?.FullName ?? string.Empty,
        PatientPhone = f.Patient?.User?.PhoneNumber,
        DentistId = f.DentistId,
        DentistName = f.Dentist?.FullName ?? string.Empty,
        OriginAppointmentId = f.OriginAppointmentId,
        TreatmentPlanItemId = f.TreatmentPlanItemId,
        ServiceName = f.TreatmentPlanItem?.Service?.Name,
        TreatmentSessionId = f.TreatmentSessionId,
        SessionName = f.TreatmentSession?.Name,
        DueDate = f.DueDate,
        Note = f.Note,
        Status = f.Status.ToString(),
        AppointmentId = f.AppointmentId,
        CreatedAt = f.CreatedAt,
        CompletedAt = f.CompletedAt
    };

    public static TreatmentProcedureDto ToDto(TreatmentProcedure p) => new()
    {
        Id = p.Id,
        ServiceId = p.ServiceId,
        StepNumber = p.StepNumber,
        Name = p.Name,
        DurationMinutes = p.DurationMinutes,
        IsRequired = p.IsRequired,
        Description = p.Description
    };

    public static ExaminationDto ToExaminationDto(Appointment a) => new()
    {
        AppointmentId = a.Id,
        AppointmentCode = AppointmentCode(a),
        Patient = new PatientBriefDto
        {
            Id = a.PatientId,
            FullName = a.Patient?.FullName ?? string.Empty,
            PhoneNumber = a.Patient?.PhoneNumber ?? a.Patient?.User?.PhoneNumber,
            Email = a.Patient?.User?.Email,
            DateOfBirth = a.Patient?.DateOfBirth,
            Gender = a.Patient?.Gender,
            Address = a.Patient?.Address
        },
        Dentist = new DentistBriefDto
        {
            Id = a.DentistId,
            FullName = a.Dentist?.FullName ?? string.Empty
        },
        ServiceName = a.Service?.Name,
        AppointmentDate = a.AppointmentDate,
        Status = a.Status.ToString(),
        AppointmentType = a.AppointmentType.ToString(),
        DurationMinutes = a.DurationMinutes,
        Symptoms = a.Symptoms,
        Notes = a.Notes,
        StartTime = a.CheckedInAt,
        FollowUpDate = a.FollowUpDate,
        FollowUpNote = a.FollowUpNote,
        IsFollowUpVisit = a.FollowUpFromAppointmentId.HasValue || a.FollowUpId.HasValue,
        Diagnoses = a.Diagnoses.Select(ToDto).ToList(),
        TreatmentPlans = a.TreatmentPlans.Select(tp => ToDto(tp, 0, false)).ToList(),
        AppointmentSessions = a.AppointmentSessions.Select(ToDto).ToList(),
        FollowUpOrder = a.FollowUpOrder != null ? ToDto(a.FollowUpOrder) : null,
        Prescription = a.Prescriptions.FirstOrDefault() is { } pres ? ToDto(pres) : null
    };

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
        a.TreatmentPlans.SelectMany(tp => tp.Items.Count > 0
            ? tp.Items.Select(i => new MedicalHistoryTreatmentPlanDto(
                string.IsNullOrWhiteSpace(i.Teeth) ? (i.Service?.Name ?? tp.Title) : $"{i.Service?.Name ?? tp.Title} - Răng {i.Teeth}",
                i.Status.ToString(),
                i.TotalCost))
            : new[] { new MedicalHistoryTreatmentPlanDto(tp.Title, tp.Status.ToString(), tp.TotalCost) }
        ).ToList();

    public static List<MedicalHistoryPrescriptionItemDto> ToMedicalHistoryPrescriptionItems(Appointment a) =>
        a.Prescriptions.SelectMany(p => p.Items.Select(i => new MedicalHistoryPrescriptionItemDto(
            i.MedicineName,
            i.Dosage,
            i.Quantity,
            i.Unit,
            i.Usage,
            i.Notes
        ))).ToList();

    public static List<MedicalHistoryPhotoDto> ToMedicalHistoryPhotos(Appointment a) =>
        a.Photos.Where(p => p.Section == "exam").Select(p => new MedicalHistoryPhotoDto(
            p.Url,
            p.Note,
            p.CreatedAt
        )).ToList();
}
