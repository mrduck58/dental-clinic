using System.Text.RegularExpressions;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Validators;

public static partial class StaffValidator
{
    private static readonly string[] ValidRoles = ["Staff", "Dentist", "Admin"];
    private static readonly string[] ValidGenders = ["Nam", "Nữ", "Khác"];
    private static readonly string[] ValidEmploymentTypes = ["Full-time", "Part-time", "Shift-based", "Intern"];
    private static readonly string[] ValidSalaryUnits = ["Theo tháng", "Theo ngày", "Theo ca", "Theo giờ"];
    private static readonly string[] ValidEmploymentStatuses = ["Active", "Inactive", "On Leave", "Terminated"];

    public const int MaxFullNameLength = 200;
    public const int MaxEmailLength = 255;
    public const int MaxPhoneLength = 15;
    public const int MinPhoneLength = 10;
    public const int MaxAddressLength = 500;
    public const int MaxLicenseLength = 100;
    public const int MaxSpecialtyLength = 100;
    public const int MaxEducationLength = 200;
    public const int MaxBioLength = 2000;
    public const int MaxPositionLength = 100;
    public const int MaxDepartmentLength = 100;
    public const int MaxServicesLength = 500;
    public const int MaxCertIssuedByLength = 200;
    public const int MinAge = 16;
    public const int MaxAge = 100;
    public const int MaxYearsOfExperience = 70;
    public const decimal MaxBaseSalary = 999_999_999m;
    public const decimal MaxRatePerShift = 999_999_999m;
    /// <summary>Định mức phép tối đa/tháng, tính theo ca (không vượt quá tổng ca công một tháng: 26 ngày × 6 ca/ngày = 156).</summary>
    public const decimal MaxLeaveAccrued = 156m;

    /// <summary>
    /// Validates all staff fields for Create operation. Throws ValidationException if invalid.
    /// </summary>
    public static void ValidateCreate(
        string fullName, string email, string phoneNumber, string role,
        string? gender, DateOnly? dateOfBirth, string? address,
        string? specialty, string? licenseNumber, int? yearsOfExperience,
        DateOnly? startDate, string? servicesHandled,
        DateOnly? certificateIssuedDate, string? certificateIssuedBy,
        string? education, string? bio, string? position, string? department,
        string? employmentType, decimal? baseSalary, string? salaryUnit,
        decimal? leaveAccrued, string? employmentStatus, decimal? ratePerShift = null)
    {
        var errors = new Dictionary<string, List<string>>();

        ValidateFullName(fullName, errors);
        ValidateEmail(email, errors);
        ValidatePhoneNumber(phoneNumber, errors);
        ValidateRole(role, errors);
        ValidateGender(gender, errors);
        ValidateDateOfBirth(dateOfBirth, errors);
        ValidateAddress(address, errors);
        ValidateStartDate(startDate, errors);
        ValidateBio(bio, errors);
        ValidatePosition(position, errors);
        ValidateDepartment(department, errors);
        ValidateEducation(education, errors);
        ValidateEmploymentStatus(employmentStatus, errors);

        // Doctor-specific validations
        if (string.Equals(role, "Dentist", StringComparison.OrdinalIgnoreCase))
        {
            ValidateSpecialty(specialty, errors, required: true);
            ValidateLicenseNumber(licenseNumber, errors, required: true);
            ValidateYearsOfExperience(yearsOfExperience, errors);
            ValidateServicesHandled(servicesHandled, errors);
            ValidateCertificateIssuedDate(certificateIssuedDate, errors);
            ValidateCertificateIssuedBy(certificateIssuedBy, errors);
        }
        else
        {
            ValidateSpecialty(specialty, errors, required: false);
            ValidateLicenseNumber(licenseNumber, errors, required: false);
            ValidateYearsOfExperience(yearsOfExperience, errors);
        }

        // Salary & Leave validations
        ValidateEmploymentType(employmentType, errors);
        ValidateBaseSalary(baseSalary, errors);
        ValidateSalaryUnit(salaryUnit, employmentType, errors);
        ValidateLeaveAccrued(leaveAccrued, errors);
        ValidateRatePerShift(ratePerShift, errors);

        ThrowIfErrors(errors);
    }

    /// <summary>
    /// Validates all staff fields for Update operation.
    /// </summary>
    public static void ValidateUpdate(
        string fullName, string email, string phoneNumber, string role,
        string? gender, DateOnly? dateOfBirth, string? address,
        string? specialty, string? licenseNumber, int? yearsOfExperience,
        DateOnly? startDate, string? servicesHandled,
        DateOnly? certificateIssuedDate, string? certificateIssuedBy,
        string? education, string? bio, string? position, string? department,
        string? employmentType, decimal? baseSalary, string? salaryUnit,
        decimal? leaveAccrued, string? employmentStatus, decimal? ratePerShift = null)
    {
        // Same validation rules apply for update
        ValidateCreate(fullName, email, phoneNumber, role,
            gender, dateOfBirth, address,
            specialty, licenseNumber, yearsOfExperience,
            startDate, servicesHandled,
            certificateIssuedDate, certificateIssuedBy,
            education, bio, position, department,
            employmentType, baseSalary, salaryUnit,
            leaveAccrued, employmentStatus, ratePerShift);
    }

    // ── Individual field validators ──────────────────────────────────────────

    public static void ValidateFullName(string fullName, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            AddError(errors, "fullName", "Họ và tên không được để trống.");
            return;
        }
        if (fullName.Length > MaxFullNameLength)
            AddError(errors, "fullName", $"Họ và tên không được vượt quá {MaxFullNameLength} ký tự.");
    }

    public static void ValidateEmail(string email, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            AddError(errors, "email", "Email không được để trống.");
            return;
        }
        if (email.Length > MaxEmailLength)
        {
            AddError(errors, "email", $"Email không được vượt quá {MaxEmailLength} ký tự.");
            return;
        }
        if (!EmailRegex().IsMatch(email))
            AddError(errors, "email", "Email không đúng định dạng.");
    }

    public static void ValidatePhoneNumber(string phoneNumber, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            AddError(errors, "phoneNumber", "Số điện thoại không được để trống.");
            return;
        }
        var digits = phoneNumber.Trim();
        if (!PhoneRegex().IsMatch(digits))
        {
            AddError(errors, "phoneNumber", "Số điện thoại chỉ được chứa chữ số và dấu +, -, khoảng trắng.");
            return;
        }
        var digitCount = digits.Count(char.IsDigit);
        if (digitCount < MinPhoneLength)
            AddError(errors, "phoneNumber", $"Số điện thoại phải có ít nhất {MinPhoneLength} chữ số.");
        if (digitCount > MaxPhoneLength)
            AddError(errors, "phoneNumber", $"Số điện thoại không được vượt quá {MaxPhoneLength} chữ số.");
    }

    public static void ValidateRole(string role, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            AddError(errors, "role", "Vai trò không được để trống.");
            return;
        }
        if (!ValidRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            AddError(errors, "role", $"Vai trò phải là một trong: {string.Join(", ", ValidRoles)}.");
    }

    public static void ValidateGender(string? gender, Dictionary<string, List<string>> errors)
    {
        if (gender is null) return; // optional
        if (string.IsNullOrWhiteSpace(gender))
        {
            AddError(errors, "gender", "Giới tính không được để trống nếu đã cung cấp.");
            return;
        }
        if (!ValidGenders.Contains(gender))
            AddError(errors, "gender", $"Giới tính phải là một trong: {string.Join(", ", ValidGenders)}.");
    }

    public static void ValidateDateOfBirth(DateOnly? dateOfBirth, Dictionary<string, List<string>> errors)
    {
        if (dateOfBirth is null) return; // optional
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value > today.AddYears(-age)) age--;

        if (dateOfBirth.Value > today)
            AddError(errors, "dateOfBirth", "Ngày sinh không được là ngày trong tương lai.");
        else if (age < MinAge)
            AddError(errors, "dateOfBirth", $"Nhân viên phải từ {MinAge} tuổi trở lên.");
        else if (age > MaxAge)
            AddError(errors, "dateOfBirth", $"Ngày sinh không hợp lệ (tuổi vượt quá {MaxAge}).");
    }

    public static void ValidateAddress(string? address, Dictionary<string, List<string>> errors)
    {
        if (address is not null && address.Length > MaxAddressLength)
            AddError(errors, "address", $"Địa chỉ không được vượt quá {MaxAddressLength} ký tự.");
    }

    public static void ValidateStartDate(DateOnly? startDate, Dictionary<string, List<string>> errors)
    {
        if (startDate is null) return;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxFuture = today.AddYears(1);
        if (startDate.Value > maxFuture)
            AddError(errors, "startDate", "Ngày bắt đầu không được quá 1 năm trong tương lai.");
    }

    public static void ValidateSpecialty(string? specialty, Dictionary<string, List<string>> errors, bool required)
    {
        if (required && string.IsNullOrWhiteSpace(specialty))
        {
            AddError(errors, "specialty", "Chuyên khoa là bắt buộc đối với bác sĩ.");
            return;
        }
        if (specialty is not null && specialty.Length > MaxSpecialtyLength)
            AddError(errors, "specialty", $"Chuyên khoa không được vượt quá {MaxSpecialtyLength} ký tự.");
    }

    public static void ValidateLicenseNumber(string? licenseNumber, Dictionary<string, List<string>> errors, bool required)
    {
        if (required && string.IsNullOrWhiteSpace(licenseNumber))
        {
            AddError(errors, "licenseNumber", "Số giấy phép hành nghề là bắt buộc đối với bác sĩ.");
            return;
        }
        if (licenseNumber is not null && licenseNumber.Length > MaxLicenseLength)
            AddError(errors, "licenseNumber", $"Số giấy phép không được vượt quá {MaxLicenseLength} ký tự.");
    }

    public static void ValidateYearsOfExperience(int? yearsOfExperience, Dictionary<string, List<string>> errors)
    {
        if (yearsOfExperience is null) return;
        if (yearsOfExperience < 0)
            AddError(errors, "yearsOfExperience", "Số năm kinh nghiệm không được âm.");
        else if (yearsOfExperience > MaxYearsOfExperience)
            AddError(errors, "yearsOfExperience", $"Số năm kinh nghiệm không được vượt quá {MaxYearsOfExperience}.");
    }

    public static void ValidateServicesHandled(string? servicesHandled, Dictionary<string, List<string>> errors)
    {
        if (servicesHandled is not null && servicesHandled.Length > MaxServicesLength)
            AddError(errors, "servicesHandled", $"Dịch vụ phụ trách không được vượt quá {MaxServicesLength} ký tự.");
    }

    public static void ValidateCertificateIssuedDate(DateOnly? certificateIssuedDate, Dictionary<string, List<string>> errors)
    {
        if (certificateIssuedDate is null) return;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (certificateIssuedDate.Value > today)
            AddError(errors, "certificateIssuedDate", "Ngày cấp chứng chỉ không được là ngày trong tương lai.");
    }

    public static void ValidateCertificateIssuedBy(string? certificateIssuedBy, Dictionary<string, List<string>> errors)
    {
        if (certificateIssuedBy is not null && certificateIssuedBy.Length > MaxCertIssuedByLength)
            AddError(errors, "certificateIssuedBy", $"Nơi cấp không được vượt quá {MaxCertIssuedByLength} ký tự.");
    }

    public static void ValidateBio(string? bio, Dictionary<string, List<string>> errors)
    {
        if (bio is not null && bio.Length > MaxBioLength)
            AddError(errors, "bio", $"Tiểu sử không được vượt quá {MaxBioLength} ký tự.");
    }

    public static void ValidatePosition(string? position, Dictionary<string, List<string>> errors)
    {
        if (position is not null && position.Length > MaxPositionLength)
            AddError(errors, "position", $"Chức vụ không được vượt quá {MaxPositionLength} ký tự.");
    }

    public static void ValidateDepartment(string? department, Dictionary<string, List<string>> errors)
    {
        if (department is not null && department.Length > MaxDepartmentLength)
            AddError(errors, "department", $"Phòng ban không được vượt quá {MaxDepartmentLength} ký tự.");
    }

    public static void ValidateEducation(string? education, Dictionary<string, List<string>> errors)
    {
        if (education is not null && education.Length > MaxEducationLength)
            AddError(errors, "education", $"Trình độ học vấn không được vượt quá {MaxEducationLength} ký tự.");
    }

    public static void ValidateEmploymentStatus(string? employmentStatus, Dictionary<string, List<string>> errors)
    {
        if (employmentStatus is null) return;
        if (!ValidEmploymentStatuses.Contains(employmentStatus, StringComparer.OrdinalIgnoreCase))
            AddError(errors, "employmentStatus", $"Trạng thái công tác phải là một trong: {string.Join(", ", ValidEmploymentStatuses)}.");
    }

    // ── Salary & Leave validators ────────────────────────────────────────────

    public static void ValidateEmploymentType(string? employmentType, Dictionary<string, List<string>> errors)
    {
        if (employmentType is null) return;
        if (!ValidEmploymentTypes.Contains(employmentType, StringComparer.OrdinalIgnoreCase))
            AddError(errors, "employmentType", $"Loại hình công việc phải là một trong: {string.Join(", ", ValidEmploymentTypes)}.");
    }

    public static void ValidateBaseSalary(decimal? baseSalary, Dictionary<string, List<string>> errors)
    {
        if (baseSalary is null) return;
        if (baseSalary < 0)
            AddError(errors, "baseSalary", "Lương cơ bản không được âm.");
        else if (baseSalary > MaxBaseSalary)
            AddError(errors, "baseSalary", $"Lương cơ bản không được vượt quá {MaxBaseSalary:N0}.");
    }

    public static void ValidateSalaryUnit(string? salaryUnit, string? employmentType, Dictionary<string, List<string>> errors)
    {
        if (salaryUnit is null) return;
        if (!ValidSalaryUnits.Contains(salaryUnit, StringComparer.OrdinalIgnoreCase))
        {
            AddError(errors, "salaryUnit", $"Đơn vị tính lương phải là một trong: {string.Join(", ", ValidSalaryUnits)}.");
            return;
        }
        // Full-time must use "Theo tháng"
        if (string.Equals(employmentType, "Full-time", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(salaryUnit, "Theo tháng", StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "salaryUnit", "Nhân viên Full-time chỉ được tính lương theo tháng.");
        }
    }

    public static void ValidateLeaveAccrued(decimal? leaveAccrued, Dictionary<string, List<string>> errors)
    {
        if (leaveAccrued is null) return;
        if (leaveAccrued < 0)
            AddError(errors, "leaveAccrued", "Số ca phép tích luỹ không được âm.");
        else if (leaveAccrued > MaxLeaveAccrued)
            AddError(errors, "leaveAccrued", $"Số ca phép tích luỹ không được vượt quá {MaxLeaveAccrued}.");
    }

    /// <summary>Đơn giá/ca — dùng để tính lương cho nhân sự KHÔNG phải Full-time (lương = số ca đã lên
    /// lịch trong tháng × đơn giá này).</summary>
    public static void ValidateRatePerShift(decimal? ratePerShift, Dictionary<string, List<string>> errors)
    {
        if (ratePerShift is null) return;
        if (ratePerShift < 0)
            AddError(errors, "ratePerShift", "Đơn giá/ca không được âm.");
        else if (ratePerShift > MaxRatePerShift)
            AddError(errors, "ratePerShift", $"Đơn giá/ca không được vượt quá {MaxRatePerShift:N0}.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = [];
            errors[field] = list;
        }
        list.Add(message);
    }

    private static void ThrowIfErrors(Dictionary<string, List<string>> errors)
    {
        if (errors.Count == 0) return;
        var dict = errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        throw new ValidationException(dict);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[\d\s\+\-\(\)]+$")]
    private static partial Regex PhoneRegex();
}
