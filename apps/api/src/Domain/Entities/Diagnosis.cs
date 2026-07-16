namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Các trường của phiếu khám răng miệng. Gom thành record để <see cref="Diagnosis.Create"/> /
/// <see cref="Diagnosis.Update"/> không phải nhận hàng chục tham số rời (dễ truyền nhầm thứ tự).
/// </summary>
public record DiagnosisDetails(
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
    // Tiền sử (cần khi kê thuốc / gây tê)
    string? MedicalHistory,
    string? AllergyHistory,
    // Kết quả & kế hoạch điều trị
    string? Conclusion);

/// <summary>Phiếu khám răng miệng của một buổi hẹn.</summary>
public class Diagnosis
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }

    /// <summary>Chẩn đoán của bác sĩ.</summary>
    public string Description { get; private set; } = string.Empty;

    // ── Tình trạng lợi – niêm mạc ───────────────────────────────────────────
    public string? GumCondition { get; private set; }         // Tình trạng lợi
    public string? OralMucosaCondition { get; private set; }  // Tình trạng niêm mạc miệng
    public string? GumBleeding { get; private set; }          // Chảy máu lợi
    public string? PainOnChewing { get; private set; }        // Đau khi chạm / ăn nhai

    // ── Tình trạng răng ─────────────────────────────────────────────────────
    public string? TeethCount { get; private set; }           // Số răng hiện có
    public string? DecayedTeeth { get; private set; }         // Răng sâu
    public string? WornOrBrokenTeeth { get; private set; }    // Răng mòn / nứt / vỡ
    public string? LooseTeeth { get; private set; }           // Răng lung lay

    // ── Vệ sinh răng miệng ──────────────────────────────────────────────────
    public string? Tartar { get; private set; }               // Cao răng
    public string? Plaque { get; private set; }               // Mảng bám
    public string? BadBreath { get; private set; }            // Mùi hôi miệng

    // ── Khớp thái dương hàm / khớp cắn ──────────────────────────────────────
    public string? TmjSymptoms { get; private set; }          // Triệu chứng khớp thái dương hàm
    public string? Occlusion { get; private set; }            // Khớp cắn
    public string? OcclusionDeviation { get; private set; }   // Sai lệch khớp cắn

    // ── Tiền sử ─────────────────────────────────────────────────────────────
    public string? MedicalHistory { get; private set; }       // Tiền sử bệnh lý
    public string? AllergyHistory { get; private set; }       // Tiền sử dị ứng

    /// <summary>Kết quả &amp; kế hoạch điều trị / tư vấn.</summary>
    public string? Conclusion { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation property
    public Appointment Appointment { get; private set; } = null!;

    private Diagnosis() { }

    public static Diagnosis Create(Guid appointmentId, string description, DiagnosisDetails details)
    {
        var diagnosis = new Diagnosis
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        diagnosis.Apply(description, details);
        return diagnosis;
    }

    public void Update(string description, DiagnosisDetails details)
    {
        Apply(description, details);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(string description, DiagnosisDetails d)
    {
        Description = description;
        GumCondition = d.GumCondition;
        OralMucosaCondition = d.OralMucosaCondition;
        GumBleeding = d.GumBleeding;
        PainOnChewing = d.PainOnChewing;
        TeethCount = d.TeethCount;
        DecayedTeeth = d.DecayedTeeth;
        WornOrBrokenTeeth = d.WornOrBrokenTeeth;
        LooseTeeth = d.LooseTeeth;
        Tartar = d.Tartar;
        Plaque = d.Plaque;
        BadBreath = d.BadBreath;
        TmjSymptoms = d.TmjSymptoms;
        Occlusion = d.Occlusion;
        OcclusionDeviation = d.OcclusionDeviation;
        MedicalHistory = d.MedicalHistory;
        AllergyHistory = d.AllergyHistory;
        Conclusion = d.Conclusion;
    }
}
