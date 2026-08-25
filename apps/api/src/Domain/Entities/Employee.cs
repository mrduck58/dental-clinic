namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Thông tin nhân sự (HR) dùng chung cho mọi nhân viên phòng khám — bao gồm cả Staff (lễ tân/hành chính)
/// và Dentist (bác sĩ, có thêm hồ sơ chuyên môn riêng ở <see cref="DentistProfile"/>).
/// Trước đây các field này bị lặp lại y hệt giữa 2 entity Staff và Dentist.
/// </summary>
public class Employee
{
    public const string DefaultEmploymentStatus = "Active";

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

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

    // Navigation properties
    public User User { get; set; } = null!;
    public DentistProfile? DentistProfile { get; private set; }

    private Employee() { }

    public static Employee Create(
        Guid userId,
        string employeeId,
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
        decimal? leaveAccrued = null)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmployeeId = employeeId,
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
            LeaveAccrued = leaveAccrued
        };
    }

    public void Update(
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
        decimal? leaveAccrued)
    {
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
    }
}
