using DentalClinic.API.Application.UseCases.Staff;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "Admin,Owner")]
public class StaffController(
    GetStaffHandler getStaffHandler,
    ISender sender) : ControllerBase
{
    /// <summary>GET api/staff — Danh sách nhân viên với filter + phân trang</summary>
    [HttpGet]
    public async Task<IActionResult> GetStaff(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? specialty,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var result = await getStaffHandler.HandleAsync(
            new GetStaffQuery(search, role, status, specialty, page, pageSize, sortBy, sortDir),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>GET api/staff/{id} — Lấy chi tiết 1 nhân viên</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStaffById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await getStaffHandler.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/staff — Tạo hồ sơ nhân viên mới (chưa có tài khoản)</summary>
    [HttpPost]
    public async Task<IActionResult> CreateStaff(
        [FromBody] CreateStaffRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateStaffCommand(
                request.FullName, request.Email, request.PhoneNumber, request.Role,
                request.EmployeeId, request.Department, request.EmploymentStatus,
                request.ProfilePictureUrl, request.ProfessionalNotes,
                request.Specialty, request.LicenseNumber, request.YearsOfExperience,
                request.Gender, request.DateOfBirth, request.Address,
                request.StartDate, request.ServicesHandled, request.CertificateIssuedDate,
                request.CertificateIssuedBy, request.Education, request.Bio, request.Position,
                request.EmploymentType, request.BaseSalary, request.SalaryUnit, request.LeaveAccrued,
                request.Allowance, request.RatePerShift, request.Content),
            cancellationToken);

        return CreatedAtAction(nameof(GetStaffById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/staff/{id} — Cập nhật thông tin nhân viên</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStaff(
        Guid id,
        [FromBody] UpdateStaffRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateStaffCommand(
                id, request.FullName, request.Email, request.PhoneNumber, request.Role,
                request.Department, request.EmploymentStatus,
                request.ProfilePictureUrl, request.ProfessionalNotes, request.IsActive,
                request.Specialty, request.LicenseNumber, request.YearsOfExperience,
                request.Gender, request.DateOfBirth, request.Address,
                request.StartDate, request.ServicesHandled, request.CertificateIssuedDate,
                request.CertificateIssuedBy, request.Education, request.Bio, request.Position,
                request.EmploymentType, request.BaseSalary, request.SalaryUnit, request.LeaveAccrued,
                request.Allowance, request.RatePerShift, request.Content),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/staff/{id}/reset-password — Đặt lại mật khẩu</summary>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetStaffPasswordCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/staff/{id}/create-account — Tạo tài khoản đăng nhập cho nhân viên</summary>
    [HttpPost("{id:guid}/create-account")]
    public async Task<IActionResult> CreateAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateStaffAccountCommand(id), cancellationToken);
        return Ok(result);
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateStaffRequestDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    string? EmployeeId,
    string? Department,
    string? EmploymentStatus,
    string? ProfilePictureUrl,
    string? ProfessionalNotes,
    string? Specialty,
    string? LicenseNumber,
    int? YearsOfExperience,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Address,
    DateOnly? StartDate,
    string? ServicesHandled,
    DateOnly? CertificateIssuedDate,
    string? CertificateIssuedBy,
    string? Education,
    string? Bio,
    string? Position,
    string? EmploymentType,
    decimal? BaseSalary,
    string? SalaryUnit,
    decimal? LeaveAccrued,
    decimal? Allowance,
    decimal? RatePerShift,
    string? Content);

public record UpdateStaffRequestDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    string? Department,
    string? EmploymentStatus,
    string? ProfilePictureUrl,
    string? ProfessionalNotes,
    bool IsActive,
    string? Specialty,
    string? LicenseNumber,
    int? YearsOfExperience,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Address,
    DateOnly? StartDate,
    string? ServicesHandled,
    DateOnly? CertificateIssuedDate,
    string? CertificateIssuedBy,
    string? Education,
    string? Bio,
    string? Position,
    string? EmploymentType,
    decimal? BaseSalary,
    string? SalaryUnit,
    decimal? LeaveAccrued,
    decimal? Allowance,
    decimal? RatePerShift,
    string? Content);
