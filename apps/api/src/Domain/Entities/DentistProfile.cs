namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Hồ sơ chuyên môn của bác sĩ — chỉ còn field lâm sàng/hành nghề (trước đây gộp chung với field HR
/// trong entity Dentist cũ, nay field HR đã chuyển sang <see cref="Employee"/>).
/// FK trỏ vào Employee, KHÔNG trỏ thẳng User — một User muốn là bác sĩ trước hết phải là Employee.
/// Id giữ nguyên ý nghĩa định danh như Dentist.Id cũ để Appointment/DentistReview/TreatmentPlan
/// không cần đổi cột FK.
/// </summary>
public class DentistProfile
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }

    public string Specialization { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public int? ExperienceYears { get; private set; }
    public string? Education { get; private set; }
    public string? Biography { get; private set; }
    public DateOnly? CertificateIssuedDate { get; private set; }
    public string? CertificateIssuedBy { get; private set; }
    public string Shift { get; private set; } = "morning"; // "morning" | "afternoon"

    /// <summary>Nội dung bài viết HTML giới thiệu chi tiết về nha sĩ (hiển thị trên clinic_website).</summary>
    public string Content { get; private set; } = string.Empty;

    // Read-only delegated properties
    public string FullName => Employee?.User?.FullName ?? string.Empty;
    public string? ProfilePictureUrl => Employee?.ProfilePictureUrl;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; private set; } = [];

    private DentistProfile() { }

    public static DentistProfile Create(
        Guid employeeId,
        string specialization,
        string licenseNumber,
        int? experienceYears = null,
        string? education = null,
        string? biography = null,
        DateOnly? certificateIssuedDate = null,
        string? certificateIssuedBy = null,
        string shift = "morning",
        string? content = null)
    {
        return new DentistProfile
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Specialization = specialization,
            LicenseNumber = licenseNumber,
            ExperienceYears = experienceYears,
            Education = education,
            Biography = biography,
            CertificateIssuedDate = certificateIssuedDate,
            CertificateIssuedBy = certificateIssuedBy,
            Shift = shift,
            Content = content ?? string.Empty
        };
    }

    public void Update(
        string specialization,
        string licenseNumber,
        int? experienceYears,
        string? education,
        string? biography,
        DateOnly? certificateIssuedDate,
        string? certificateIssuedBy,
        string shift,
        string? content = null)
    {
        Specialization = specialization;
        LicenseNumber = licenseNumber;
        ExperienceYears = experienceYears;
        Education = education;
        Biography = biography;
        CertificateIssuedDate = certificateIssuedDate;
        CertificateIssuedBy = certificateIssuedBy;
        Shift = shift;
        Content = content ?? string.Empty;
    }
}
