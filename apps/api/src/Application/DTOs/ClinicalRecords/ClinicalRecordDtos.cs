namespace DentalClinic.API.Application.DTOs.ClinicalRecords;

public class ExaminationDto
{
    public Guid AppointmentId { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public PatientBriefDto Patient { get; set; } = null!;
    public DentistBriefDto Dentist { get; set; } = null!;
    public string? ServiceName { get; set; }
    public DateTimeOffset AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AppointmentType { get; set; } = "GeneralExam";
    public int DurationMinutes { get; set; } = 30;
    public string? Symptoms { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
    public bool IsFollowUpVisit { get; set; }
    public List<Guid> RelatedAppointmentIds { get; set; } = new();
    public List<DiagnosisDto> Diagnoses { get; set; } = new();
    public List<TreatmentPlanDto> TreatmentPlans { get; set; } = new();
    public List<AppointmentSessionDto> AppointmentSessions { get; set; } = new();
    public FollowUpDto? FollowUpOrder { get; set; }
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
    public string Description { get; set; } = string.Empty;
    public string? GumCondition { get; set; }
    public string? OralMucosaCondition { get; set; }
    public string? GumBleeding { get; set; }
    public string? PainOnChewing { get; set; }
    public string? TeethCount { get; set; }
    public string? DecayedTeeth { get; set; }
    public string? WornOrBrokenTeeth { get; set; }
    public string? LooseTeeth { get; set; }
    public string? Tartar { get; set; }
    public string? Plaque { get; set; }
    public string? BadBreath { get; set; }
    public string? TmjSymptoms { get; set; }
    public string? Occlusion { get; set; }
    public string? OcclusionDeviation { get; set; }
    public string? MedicalHistory { get; set; }
    public string? AllergyHistory { get; set; }
    public string? Conclusion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class AppointmentPhotoDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
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

public class StepProgressEntryDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int Percent { get; set; }
    public DateOnly Date { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class TreatmentSessionDto
{
    public Guid Id { get; set; }
    public Guid TreatmentPlanItemId { get; set; }
    public Guid? TreatmentProcedureId { get; set; }
    public int SessionNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Planned";
    public int DurationMinutes { get; set; } = 30;
    public Guid? DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public DateTimeOffset? PerformedAt { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class TreatmentPlanItemDto
{
    public Guid Id { get; set; }
    public Guid TreatmentPlanId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public Guid? ServiceOptionId { get; set; }
    public string? ServiceOptionName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Teeth { get; set; }
    public int? EstimatedSessionCount { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public int? EstimatedDurationMax { get; set; }
    public string? EstimatedDurationUnit { get; set; }
    public DateOnly? EstimatedStartDate { get; set; }
    public DateOnly? EstimatedEndDate { get; set; }
    public string Status { get; set; } = "Planned";
    public DateOnly? WarrantyUntil { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCost { get; set; }
    public List<TreatmentSessionDto> Sessions { get; set; } = new();
    public List<StepProgressEntryDto> StepProgress { get; set; } = new();
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int ProgressPercent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class TreatmentPlanDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsInvoiced { get; set; }
    public List<TreatmentPlanItemDto> Items { get; set; } = new();

    // Thuộc tính tương thích cho code cũ / UI cũ
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceOptionName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Teeth { get; set; }
    public int? EstimatedSessionCount { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public int? EstimatedDurationMax { get; set; }
    public string? EstimatedDurationUnit { get; set; }
    public DateOnly? EstimatedStartDate { get; set; }
    public DateOnly? EstimatedEndDate { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public List<StepProgressEntryDto> StepProgress { get; set; } = new();
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int ProgressPercent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class AppointmentSessionDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid TreatmentSessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? Teeth { get; set; }
    public int Sequence { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class FollowUpDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public Guid DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public Guid OriginAppointmentId { get; set; }
    public Guid? TreatmentPlanItemId { get; set; }
    public string? ServiceName { get; set; }
    public Guid? TreatmentSessionId { get; set; }
    public string? SessionName { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? AppointmentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class TreatmentProcedureDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 30;
    public bool IsRequired { get; set; } = true;
    public string? Description { get; set; }
}

public record MedicalHistoryDiagnosisDto(
    string Description,
    string? GumCondition,
    string? OralMucosaCondition,
    string? GumBleeding,
    string? PainOnChewing,
    string? TeethCount,
    string? DecayedTeeth,
    string? WornOrBrokenTeeth,
    string? LooseTeeth,
    string? Tartar,
    string? Plaque,
    string? BadBreath,
    string? TmjSymptoms,
    string? Occlusion,
    string? OcclusionDeviation,
    string? Conclusion,
    DateTimeOffset CreatedAt);

public record MedicalHistoryTreatmentPlanDto(
    string Description,
    string Status,
    decimal? EstimatedCost);

public record MedicalHistoryPrescriptionItemDto(
    string MedicineName,
    string Dosage,
    int Quantity,
    string Unit,
    string Usage,
    string? Notes);

public record MedicalHistoryPhotoDto(
    string Url,
    string? Note,
    DateTimeOffset CreatedAt);

public record PatientMedicalHistoryDto(
    Guid AppointmentId,
    string AppointmentCode,
    DateTimeOffset AppointmentDate,
    string DentistName,
    string ServiceName,
    string? Symptoms,
    List<MedicalHistoryDiagnosisDto> Diagnoses,
    List<MedicalHistoryTreatmentPlanDto> TreatmentPlans,
    List<MedicalHistoryPrescriptionItemDto> PrescriptionItems,
    List<MedicalHistoryPhotoDto> Photos);

public record MyMedicalHistoryDto(
    Guid AppointmentId,
    string AppointmentCode,
    DateTimeOffset AppointmentDate,
    Guid DentistId,
    string DentistName,
    string ServiceName,
    string? Symptoms,
    Guid PatientId,
    string PatientName,
    string PatientRelationship,
    DateOnly? FollowUpDate,
    string? FollowUpNote,
    List<Guid> RelatedAppointmentIds,
    List<MedicalHistoryDiagnosisDto> Diagnoses,
    List<MedicalHistoryTreatmentPlanDto> TreatmentPlans,
    List<MedicalHistoryPrescriptionItemDto> PrescriptionItems,
    List<MedicalHistoryPhotoDto> Photos);
