namespace DentalClinic.API.Application.DTOs.ClinicalRecords;

// ─────────────────────────────────────────────────────────────────────────────
// DTO dùng chung của bounded context ClinicalRecords (phiếu khám: chẩn đoán,
// liệu trình điều trị, đơn thuốc, lịch sử khám).
//
// Trước đây các DTO này nằm rải rác trong chính file handler (ExaminationDto/
// DiagnosisDto/PrescriptionDto trong GetExaminationHandler.cs, TreatmentPlanDto
// trong TreatmentPlanHandler.cs) và tham chiếu chéo lẫn nhau — không thể tách
// handler ra nhiều file/nhiều folder mà không sinh phụ thuộc vòng. Gom hết về
// một chỗ theo đúng chuẩn Application/DTOs/<Feature>/ của các feature khác.
// ─────────────────────────────────────────────────────────────────────────────

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

public class StepProgressEntryDto
{
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int Percent { get; set; }
    public DateOnly Date { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class TreatmentPlanDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Teeth { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? WarrantyUntil { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AmountPaid { get; set; }
    /// <summary>Đã được xuất hóa đơn (hóa đơn chưa hoàn tiền) — bác sĩ không được xóa/hủy liệu trình này nữa.</summary>
    public bool IsInvoiced { get; set; }
    public List<StepProgressEntryDto> StepProgress { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Lịch sử khám — trước đây khai báo trực tiếp trong AppointmentsController.cs
// (logic viết thẳng trong controller). Chuyển về Application layer để handler
// mới GetMyExaminationHistory/GetPatientMedicalHistory trả về được.
// Giữ NGUYÊN thứ tự và tên thuộc tính để JSON trả về không đổi.
// ─────────────────────────────────────────────────────────────────────────────

public record MedicalHistoryDiagnosisDto(
    string Description,
    // Tình trạng lợi – niêm mạc
    string? GumCondition,
    string? OralMucosaCondition,
    string? GumBleeding,
    string? PainOnChewing,
    // Tình trạng răng
    string? TeethCount,
    string? DecayedTeeth,
    string? WornOrBrokenTeeth,
    string? LooseTeeth,
    // Vệ sinh răng miệng
    string? Tartar,
    string? Plaque,
    string? BadBreath,
    // Khớp thái dương hàm / khớp cắn
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

public record PatientMedicalHistoryDto(
    Guid AppointmentId,
    string AppointmentCode,
    DateTimeOffset AppointmentDate,
    string DentistName,
    string ServiceName,
    string? Symptoms,
    List<MedicalHistoryDiagnosisDto> Diagnoses,
    List<MedicalHistoryTreatmentPlanDto> TreatmentPlans,
    List<MedicalHistoryPrescriptionItemDto> PrescriptionItems);

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
    // Chuỗi buổi hẹn gốc của lượt tái khám (đi ngược FollowUpFromAppointmentId) — dùng để gộp
    // liệu trình điều trị dài hạn (niềng răng, cấy ghép...) xuyên suốt nhiều buổi tái khám.
    List<Guid> RelatedAppointmentIds,
    List<MedicalHistoryDiagnosisDto> Diagnoses,
    List<MedicalHistoryTreatmentPlanDto> TreatmentPlans,
    List<MedicalHistoryPrescriptionItemDto> PrescriptionItems);
