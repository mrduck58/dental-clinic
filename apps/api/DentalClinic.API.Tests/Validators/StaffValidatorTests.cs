using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Validators;

namespace DentalClinic.API.Tests.Validators;

[TestFixture]
public class StaffValidatorTests
{
    // ── Helper: build valid defaults for Create ─────────────────────────────
    private static void CallValidateCreate(
        string fullName = "Nguyễn Văn A",
        string email = "test@example.com",
        string phoneNumber = "0901234567",
        string role = "Staff",
        string? gender = "Nam",
        DateOnly? dateOfBirth = null,
        string? address = "123 Đường ABC",
        string? specialty = null,
        string? licenseNumber = null,
        int? yearsOfExperience = null,
        DateOnly? startDate = null,
        string? servicesHandled = null,
        DateOnly? certificateIssuedDate = null,
        string? certificateIssuedBy = null,
        string? education = "Đại học",
        string? bio = null,
        string? position = "Lễ tân",
        string? department = "Hành chính",
        string? employmentType = "Full-time",
        decimal? baseSalary = 12000000m,
        string? salaryUnit = "Theo tháng",
        decimal? leaveAccrued = 1.0m,
        string? employmentStatus = "Active")
    {
        dateOfBirth ??= new DateOnly(1995, 5, 15);
        startDate ??= DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));

        StaffValidator.ValidateCreate(
            fullName, email, phoneNumber, role,
            gender, dateOfBirth, address,
            specialty, licenseNumber, yearsOfExperience,
            startDate, servicesHandled,
            certificateIssuedDate, certificateIssuedBy,
            education, bio, position, department,
            employmentType, baseSalary, salaryUnit,
            leaveAccrued, employmentStatus);
    }

    // ════════════════════════════════════════════════════════════════════════
    // FullName
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void FullName_Normal_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(fullName: "Nguyễn Văn A"));
    }

    [Test]
    public void FullName_Boundary_SingleChar_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(fullName: "A"));
    }

    [Test]
    public void FullName_Boundary_MaxLength_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(fullName: new string('A', 200)));
    }

    [Test]
    public void FullName_Abnormal_Empty_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(fullName: ""));
        Assert.That(ex!.Errors.ContainsKey("fullName"), Is.True);
    }

    [Test]
    public void FullName_Abnormal_Whitespace_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(fullName: "   "));
        Assert.That(ex!.Errors.ContainsKey("fullName"), Is.True);
    }

    [Test]
    public void FullName_Abnormal_TooLong_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(fullName: new string('A', 201)));
        Assert.That(ex!.Errors.ContainsKey("fullName"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Email
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Email_Normal_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(email: "user@example.com"));
    }

    [Test]
    public void Email_Boundary_MinValid_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(email: "a@b.c"));
    }

    [Test]
    public void Email_Abnormal_Empty_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(email: ""));
        Assert.That(ex!.Errors.ContainsKey("email"), Is.True);
    }

    [Test]
    public void Email_Abnormal_NoAtSign_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(email: "invalidemail"));
        Assert.That(ex!.Errors.ContainsKey("email"), Is.True);
    }

    [Test]
    public void Email_Abnormal_TooLong_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(email: new string('a', 250) + "@b.com"));
        Assert.That(ex!.Errors.ContainsKey("email"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PhoneNumber
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void PhoneNumber_Normal_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(phoneNumber: "0901234567"));
    }

    [Test]
    public void PhoneNumber_Boundary_10Digits_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(phoneNumber: "0123456789"));
    }

    [Test]
    public void PhoneNumber_Boundary_15Digits_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(phoneNumber: "012345678901234"));
    }

    [Test]
    public void PhoneNumber_Abnormal_TooShort_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(phoneNumber: "012345"));
        Assert.That(ex!.Errors.ContainsKey("phoneNumber"), Is.True);
    }

    [Test]
    public void PhoneNumber_Abnormal_TooLong_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(phoneNumber: "0123456789012345"));
        Assert.That(ex!.Errors.ContainsKey("phoneNumber"), Is.True);
    }

    [Test]
    public void PhoneNumber_Abnormal_ContainsLetters_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(phoneNumber: "090abc1234"));
        Assert.That(ex!.Errors.ContainsKey("phoneNumber"), Is.True);
    }

    [Test]
    public void PhoneNumber_Abnormal_Empty_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(phoneNumber: ""));
        Assert.That(ex!.Errors.ContainsKey("phoneNumber"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Role
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Role_Normal_Staff_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(role: "Staff"));
    }

    [Test]
    public void Role_Normal_Dentist_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(role: "Dentist",
            specialty: "Nha khoa", licenseNumber: "BS-001"));
    }

    [Test]
    public void Role_Abnormal_Invalid_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(role: "InvalidRole"));
        Assert.That(ex!.Errors.ContainsKey("role"), Is.True);
    }

    [Test]
    public void Role_Abnormal_Empty_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(role: ""));
        Assert.That(ex!.Errors.ContainsKey("role"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gender
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Gender_Normal_Nam_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(gender: "Nam"));
    }

    [Test]
    public void Gender_Normal_Nu_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(gender: "Nữ"));
    }

    [Test]
    public void Gender_Normal_Null_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(gender: null));
    }

    [Test]
    public void Gender_Abnormal_Invalid_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(gender: "Other"));
        Assert.That(ex!.Errors.ContainsKey("gender"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // DateOfBirth
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void DateOfBirth_Normal_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(dateOfBirth: new DateOnly(1990, 1, 15)));
    }

    [Test]
    public void DateOfBirth_Boundary_Exactly16_NoError()
    {
        var dob = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16).AddDays(-1));
        Assert.DoesNotThrow(() => CallValidateCreate(dateOfBirth: dob));
    }

    [Test]
    public void DateOfBirth_Boundary_Exactly100_NoError()
    {
        var dob = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-100).AddDays(1));
        Assert.DoesNotThrow(() => CallValidateCreate(dateOfBirth: dob));
    }

    [Test]
    public void DateOfBirth_Abnormal_Future_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
        Assert.That(ex!.Errors.ContainsKey("dateOfBirth"), Is.True);
    }

    [Test]
    public void DateOfBirth_Abnormal_TooYoung_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15))));
        Assert.That(ex!.Errors.ContainsKey("dateOfBirth"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // BaseSalary
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void BaseSalary_Normal_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(baseSalary: 12000000m));
    }

    [Test]
    public void BaseSalary_Boundary_Zero_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(baseSalary: 0m));
    }

    [Test]
    public void BaseSalary_Boundary_Max_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(baseSalary: 999_999_999m));
    }

    [Test]
    public void BaseSalary_Abnormal_Negative_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(baseSalary: -1m));
        Assert.That(ex!.Errors.ContainsKey("baseSalary"), Is.True);
    }

    [Test]
    public void BaseSalary_Abnormal_TooLarge_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(baseSalary: 1_000_000_000m));
        Assert.That(ex!.Errors.ContainsKey("baseSalary"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // YearsOfExperience
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void YearsOfExperience_Normal_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(yearsOfExperience: 5));
    }

    [Test]
    public void YearsOfExperience_Boundary_Zero_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(yearsOfExperience: 0));
    }

    [Test]
    public void YearsOfExperience_Boundary_Max70_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(yearsOfExperience: 70));
    }

    [Test]
    public void YearsOfExperience_Abnormal_Negative_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(yearsOfExperience: -1));
        Assert.That(ex!.Errors.ContainsKey("yearsOfExperience"), Is.True);
    }

    [Test]
    public void YearsOfExperience_Abnormal_TooHigh_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(yearsOfExperience: 71));
        Assert.That(ex!.Errors.ContainsKey("yearsOfExperience"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // EmploymentType
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void EmploymentType_Normal_FullTime_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(employmentType: "Full-time"));
    }

    [Test]
    public void EmploymentType_Normal_PartTime_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(employmentType: "Part-time",
            salaryUnit: "Theo ngày"));
    }

    [Test]
    public void EmploymentType_Normal_Intern_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(employmentType: "Intern",
            salaryUnit: "Theo ngày"));
    }

    [Test]
    public void EmploymentType_Abnormal_Invalid_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(employmentType: "Freelance"));
        Assert.That(ex!.Errors.ContainsKey("employmentType"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SalaryUnit
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SalaryUnit_Normal_TheoThang_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(salaryUnit: "Theo tháng"));
    }

    [Test]
    public void SalaryUnit_Normal_TheoNgay_PartTime_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(
            employmentType: "Part-time", salaryUnit: "Theo ngày"));
    }

    [Test]
    public void SalaryUnit_Normal_TheoCa_PartTime_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(
            employmentType: "Part-time", salaryUnit: "Theo ca"));
    }

    [Test]
    public void SalaryUnit_Abnormal_Invalid_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(salaryUnit: "Theo năm"));
        Assert.That(ex!.Errors.ContainsKey("salaryUnit"), Is.True);
    }

    [Test]
    public void SalaryUnit_Abnormal_FullTimeMustBeTheoThang_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(employmentType: "Full-time", salaryUnit: "Theo ngày"));
        Assert.That(ex!.Errors.ContainsKey("salaryUnit"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // LeaveAccrued
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void LeaveAccrued_Normal_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(leaveAccrued: 1.5m));
    }

    [Test]
    public void LeaveAccrued_Boundary_Zero_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(leaveAccrued: 0m));
    }

    [Test]
    public void LeaveAccrued_Boundary_Max30_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(leaveAccrued: 30m));
    }

    [Test]
    public void LeaveAccrued_Abnormal_Negative_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(leaveAccrued: -0.5m));
        Assert.That(ex!.Errors.ContainsKey("leaveAccrued"), Is.True);
    }

    [Test]
    public void LeaveAccrued_Abnormal_TooHigh_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(leaveAccrued: 31m));
        Assert.That(ex!.Errors.ContainsKey("leaveAccrued"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Doctor-specific: Specialty & LicenseNumber required for Dentist
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Doctor_SpecialtyRequired_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(role: "Dentist", specialty: null, licenseNumber: "BS-001"));
        Assert.That(ex!.Errors.ContainsKey("specialty"), Is.True);
    }

    [Test]
    public void Doctor_LicenseRequired_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(role: "Dentist", specialty: "Nha khoa", licenseNumber: null));
        Assert.That(ex!.Errors.ContainsKey("licenseNumber"), Is.True);
    }

    [Test]
    public void Doctor_ValidSpecialtyAndLicense_NoError()
    {
        Assert.DoesNotThrow(() =>
            CallValidateCreate(role: "Dentist", specialty: "Nha khoa tổng quát", licenseNumber: "BS-12345"));
    }

    // ════════════════════════════════════════════════════════════════════════
    // StartDate
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void StartDate_Normal_NoError()
    {
        Assert.DoesNotThrow(() =>
            CallValidateCreate(startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3))));
    }

    [Test]
    public void StartDate_Boundary_Today_NoError()
    {
        Assert.DoesNotThrow(() =>
            CallValidateCreate(startDate: DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Test]
    public void StartDate_Abnormal_TooFarFuture_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1).AddDays(2))));
        Assert.That(ex!.Errors.ContainsKey("startDate"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // EmploymentStatus
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void EmploymentStatus_Normal_Active_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(employmentStatus: "Active"));
    }

    [Test]
    public void EmploymentStatus_Abnormal_Invalid_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(employmentStatus: "Fired"));
        Assert.That(ex!.Errors.ContainsKey("employmentStatus"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Address (boundary)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Address_Boundary_Max500_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(address: new string('A', 500)));
    }

    [Test]
    public void Address_Abnormal_TooLong_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(address: new string('A', 501)));
        Assert.That(ex!.Errors.ContainsKey("address"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Bio (boundary)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Bio_Boundary_Max2000_NoError()
    {
        Assert.DoesNotThrow(() => CallValidateCreate(bio: new string('B', 2000)));
    }

    [Test]
    public void Bio_Abnormal_TooLong_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() => CallValidateCreate(bio: new string('B', 2001)));
        Assert.That(ex!.Errors.ContainsKey("bio"), Is.True);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Multiple errors at once
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void MultipleErrors_ReturnsAllFields()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            CallValidateCreate(fullName: "", email: "bad", phoneNumber: "123", role: "Invalid"));
        Assert.That(ex!.Errors.Count, Is.GreaterThanOrEqualTo(4));
        Assert.That(ex.Errors.ContainsKey("fullName"), Is.True);
        Assert.That(ex.Errors.ContainsKey("email"), Is.True);
        Assert.That(ex.Errors.ContainsKey("phoneNumber"), Is.True);
        Assert.That(ex.Errors.ContainsKey("role"), Is.True);
    }
}
