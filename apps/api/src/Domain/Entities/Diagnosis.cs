using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Diagnosis
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string DiagnosisCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    // Các trường khám lâm sàng mới
    public decimal? HeartRate { get; private set; }           // Nhịp tim (lần/phút)
    public decimal? Temperature { get; private set; }          // Nhiệt độ (°C)
    public decimal? BloodPressureSystolic { get; private set; } // Huyết áp tâm thu (mmHg)
    public decimal? BloodPressureDiastolic { get; private set; } // Huyết áp tâm trương (mmHg)
    public string? MedicalHistory { get; private set; }        // Tiền sử bệnh lý
    public string? AllergyHistory { get; private set; }         // Tiền sử dị ứng
    public string? DentalCondition { get; private set; }        // Tình trạng răng
    public string? Conclusion { get; private set; }              // Kết luận

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation property
    public Appointment Appointment { get; private set; } = null!;

    private Diagnosis() { }

    public static Diagnosis Create(
        Guid appointmentId,
        string diagnosisCode,
        string description,
        string? notes = null,
        decimal? heartRate = null,
        decimal? temperature = null,
        decimal? bloodPressureSystolic = null,
        decimal? bloodPressureDiastolic = null,
        string? medicalHistory = null,
        string? allergyHistory = null,
        string? dentalCondition = null,
        string? conclusion = null)
    {
        return new Diagnosis
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            DiagnosisCode = diagnosisCode,
            Description = description,
            Notes = notes,
            HeartRate = heartRate,
            Temperature = temperature,
            BloodPressureSystolic = bloodPressureSystolic,
            BloodPressureDiastolic = bloodPressureDiastolic,
            MedicalHistory = medicalHistory,
            AllergyHistory = allergyHistory,
            DentalCondition = dentalCondition,
            Conclusion = conclusion,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string diagnosisCode,
        string description,
        string? notes,
        decimal? heartRate,
        decimal? temperature,
        decimal? bloodPressureSystolic,
        decimal? bloodPressureDiastolic,
        string? medicalHistory,
        string? allergyHistory,
        string? dentalCondition,
        string? conclusion)
    {
        DiagnosisCode = diagnosisCode;
        Description = description;
        Notes = notes;
        HeartRate = heartRate;
        Temperature = temperature;
        BloodPressureSystolic = bloodPressureSystolic;
        BloodPressureDiastolic = bloodPressureDiastolic;
        MedicalHistory = medicalHistory;
        AllergyHistory = allergyHistory;
        DentalCondition = dentalCondition;
        Conclusion = conclusion;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
