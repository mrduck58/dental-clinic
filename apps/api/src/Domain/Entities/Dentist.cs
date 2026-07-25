namespace DentalClinic.API.Domain.Entities;

public class Dentist
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    
    // Properties
    public string EmployeeId { get; private set; } = string.Empty;
    public string? Department { get; private set; }
    public string? Position { get; private set; }
    public string EmploymentStatus { get; private set; } = "Active";
    public string? EmploymentType { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public decimal? BaseSalary { get; private set; }
    public string? SalaryUnit { get; private set; }
    public decimal? Allowance { get; private set; }
    public decimal? LeaveAccrued { get; private set; }
    
    public string Specialization { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public int? ExperienceYears { get; private set; }
    public string? Education { get; private set; }
    public string? Biography { get; private set; }
    public DateOnly? CertificateIssuedDate { get; private set; }
    public string? CertificateIssuedBy { get; private set; }
    public string Shift { get; private set; } = "morning"; // "morning" | "afternoon"

    // Read-only delegated properties from User
    public string FullName => User?.FullName ?? string.Empty;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; private set; } = [];

    private Dentist() { }

    public static Dentist Create(
        Guid userId,
        string specialization,
        int experienceYears)
    {
        return Create(
            userId: userId,
            employeeId: $"DT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            specialization: specialization,
            licenseNumber: "N/A",
            experienceYears: experienceYears
        );
    }

    public static Dentist Create(
        Guid userId,
        string employeeId,
        string specialization,
        string licenseNumber,
        int? experienceYears = null,
        string? department = null,
        string? position = null,
        string employmentStatus = "Active",
        string? employmentType = null,
        DateOnly? startDate = null,
        DateOnly? dateOfBirth = null,
        string? address = null,
        string? profilePictureUrl = null,
        decimal? baseSalary = null,
        string? salaryUnit = null,
        decimal? allowance = null,
        decimal? leaveAccrued = null,
        string? education = null,
        string? biography = null,
        DateOnly? certificateIssuedDate = null,
        string? certificateIssuedBy = null,
        string shift = "morning")
    {
        return new Dentist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmployeeId = employeeId,
            Specialization = specialization,
            LicenseNumber = licenseNumber,
            ExperienceYears = experienceYears,
            Department = department,
            Position = position,
            EmploymentStatus = employmentStatus,
            EmploymentType = employmentType,
            StartDate = startDate,
            DateOfBirth = dateOfBirth,
            Address = address,
            ProfilePictureUrl = profilePictureUrl,
            BaseSalary = baseSalary,
            SalaryUnit = salaryUnit,
            Allowance = allowance,
            LeaveAccrued = leaveAccrued,
            Education = education,
            Biography = biography,
            CertificateIssuedDate = certificateIssuedDate,
            CertificateIssuedBy = certificateIssuedBy,
            Shift = shift
        };
    }

    public void Update(
        string specialization,
        string licenseNumber,
        int? experienceYears,
        string? department,
        string? position,
        string employmentStatus,
        string? employmentType,
        DateOnly? startDate,
        DateOnly? dateOfBirth,
        string? address,
        string? profilePictureUrl,
        decimal? baseSalary,
        string? salaryUnit,
        decimal? allowance,
        decimal? leaveAccrued,
        string? education,
        string? biography,
        DateOnly? certificateIssuedDate,
        string? certificateIssuedBy,
        string shift)
    {
        Specialization = specialization;
        LicenseNumber = licenseNumber;
        ExperienceYears = experienceYears;
        Department = department;
        Position = position;
        EmploymentStatus = employmentStatus;
        EmploymentType = employmentType;
        StartDate = startDate;
        DateOfBirth = dateOfBirth;
        Address = address;
        ProfilePictureUrl = profilePictureUrl;
        BaseSalary = baseSalary;
        SalaryUnit = salaryUnit;
        Allowance = allowance;
        LeaveAccrued = leaveAccrued;
        Education = education;
        Biography = biography;
        CertificateIssuedDate = certificateIssuedDate;
        CertificateIssuedBy = certificateIssuedBy;
        Shift = shift;
    }
}
