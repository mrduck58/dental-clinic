using DentalClinic.API.Application.DTOs.Patients;
using DentalClinic.API.Application.UseCases.Patients;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// GET api/patients/search?q= — Tra cứu bệnh nhân đã có hồ sơ, dùng cho staff điền nhanh
    /// form đặt lịch tại quầy. Cần tối thiểu 2 ký tự để tránh quét toàn bảng.
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string? q,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        var result = await sender.Send(new SearchPatientsQuery(q, limit), ct);
        return Ok(result);
    }

    [HttpGet("my-medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyMedicalHistory(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var medicalHistory = await sender.Send(new GetMyMedicalHistoryQuery(userId.Value), ct);
        return Ok(new { medicalHistory });
    }

    [HttpPut("my-medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> UpdateMyMedicalHistory([FromBody] UpdateMedicalHistoryRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        await sender.Send(new UpdateMyMedicalHistoryCommand(userId.Value, request.MedicalHistory), ct);
        return Ok(new { message = "Đã cập nhật tiền sử bệnh lý." });
    }

    [HttpGet("{patientId:guid}/medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetFamilyMemberMedicalHistory(Guid patientId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var medicalHistory = await sender.Send(new GetFamilyMemberMedicalHistoryQuery(userId.Value, patientId), ct);
        return Ok(new { medicalHistory });
    }

    [HttpPut("{patientId:guid}/medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> UpdateFamilyMemberMedicalHistory(Guid patientId, [FromBody] UpdateMedicalHistoryRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        await sender.Send(new UpdateFamilyMemberMedicalHistoryCommand(userId.Value, patientId, request.MedicalHistory), ct);
        return Ok(new { message = "Đã cập nhật tiền sử bệnh lý." });
    }

    [HttpGet("family-members")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetFamilyMembers(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var members = await sender.Send(new GetFamilyMembersQuery(userId.Value), ct);
        return Ok(members);
    }

    [HttpPost("family-members")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateFamilyMember([FromBody] CreateFamilyMemberRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var member = await sender.Send(
            new CreateFamilyMemberCommand(
                userId.Value,
                request.FullName,
                request.Relationship,
                request.DateOfBirth,
                request.Gender,
                request.PhoneNumber,
                request.ProfilePictureUrl),
            ct);

        return CreatedAtAction(null, member);
    }

    [HttpPut("family-members/{id:guid}")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> UpdateFamilyMember(Guid id, [FromBody] UpdateFamilyMemberRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        await sender.Send(
            new UpdateFamilyMemberCommand(
                userId.Value,
                id,
                request.FullName,
                request.Relationship,
                request.DateOfBirth,
                request.Gender,
                request.PhoneNumber,
                request.ProfilePictureUrl),
            ct);

        return Ok(new { message = "Đã cập nhật thông tin thành viên gia đình." });
    }

    [HttpDelete("family-members/{id:guid}")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> DeleteFamilyMember(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        await sender.Send(new DeleteFamilyMemberCommand(userId.Value, id), ct);
        return Ok(new { message = "Đã xóa thành viên gia đình." });
    }
}
