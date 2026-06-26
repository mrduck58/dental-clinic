using DentalClinic.API.Application.UseCases.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "Admin")]
public class StaffController(
    GetStaffHandler getStaffHandler,
    CreateStaffHandler createStaffHandler,
    UpdateStaffHandler updateStaffHandler,
    ResetStaffPasswordHandler resetStaffPasswordHandler,
    CreateStaffAccountHandler createStaffAccountHandler) : ControllerBase
{
    /// <summary>GET api/staff — Danh sách nhân viên với filter + phân trang</summary>
    [HttpGet]
    public async Task<IActionResult> GetStaff(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await getStaffHandler.HandleAsync(
            new GetStaffQuery(search, role, status, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/staff — Tạo hồ sơ nhân viên mới (chưa có tài khoản)</summary>
    [HttpPost]
    public async Task<IActionResult> CreateStaff(
        [FromBody] CreateStaffRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await createStaffHandler.HandleAsync(
            new CreateStaffCommand(
                request.FullName, request.Email, request.PhoneNumber, request.Role,
                request.EmployeeId, request.Department, request.EmploymentStatus,
                request.ProfilePictureUrl, request.ProfessionalNotes,
                request.Specialty, request.LicenseNumber, request.YearsOfExperience,
                request.Gender, request.DateOfBirth, request.Address,
                request.StartDate, request.ServicesHandled, request.CertificateIssuedDate,
                request.CertificateIssuedBy, request.Education, request.Bio, request.Position),
            cancellationToken);

        return CreatedAtAction(nameof(GetStaff), new { id = result.Id }, result);
    }

    /// <summary>PUT api/staff/{id} — Cập nhật thông tin nhân viên</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStaff(
        Guid id,
        [FromBody] UpdateStaffRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await updateStaffHandler.HandleAsync(
            new UpdateStaffCommand(
                id, request.FullName, request.Email, request.PhoneNumber, request.Role,
                request.Department, request.EmploymentStatus,
                request.ProfilePictureUrl, request.ProfessionalNotes, request.IsActive,
                request.Specialty, request.LicenseNumber, request.YearsOfExperience,
                request.Gender, request.DateOfBirth, request.Address,
                request.StartDate, request.ServicesHandled, request.CertificateIssuedDate,
                request.CertificateIssuedBy, request.Education, request.Bio, request.Position),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/staff/{id}/reset-password — Đặt lại mật khẩu</summary>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await resetStaffPasswordHandler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/staff/{id}/create-account — Tạo tài khoản đăng nhập cho nhân viên</summary>
    [HttpPost("{id:guid}/create-account")]
    public async Task<IActionResult> CreateAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await createStaffAccountHandler.HandleAsync(id, cancellationToken);
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
    string? Position);

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
    string? Position);
