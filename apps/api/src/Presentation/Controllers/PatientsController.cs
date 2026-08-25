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
    /// POST api/patients/accounts/verification — BƯỚC 1: gửi mã xác thực tới email bệnh nhân vừa
    /// cung cấp. Bệnh nhân mở hộp thư, đọc mã cho lễ tân nhập lại ở bước tạo tài khoản.
    ///
    /// Không có bước này thì lễ tân gõ nhầm một ký tự là mật khẩu bay tới hộp thư người lạ, kèm
    /// quyền đăng nhập vào hồ sơ bệnh án của bệnh nhân thật.
    /// </summary>
    [HttpPost("accounts/verification")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> RequestPatientEmailVerification(
        [FromBody] RequestPatientEmailVerificationRequest request,
        CancellationToken ct)
    {
        await sender.Send(new RequestPatientEmailVerificationCommand(request.Email), ct);
        return Ok(new { message = "Đã gửi mã xác thực tới email của bệnh nhân." });
    }

    /// <summary>
    /// POST api/patients/accounts — BƯỚC 2: lập tài khoản cho bệnh nhân, sau khi mã xác thực email
    /// đã được nhập đúng. Đây là đường DUY NHẤT sinh tài khoản bệnh nhân sau khi bỏ tự đăng ký.
    /// Mật khẩu tạm gửi về email và bệnh nhân bị buộc đổi ngay lần đăng nhập đầu tiên.
    /// </summary>
    [HttpPost("accounts")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> CreatePatientAccount(
        [FromBody] CreatePatientAccountRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreatePatientAccountCommand(
                request.FullName, request.Email, request.PhoneNumber, request.DateOfBirth, request.Gender,
                request.VerificationCode),
            ct);

        return Ok(result);
    }

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

    /// <summary>
    /// GET api/patients/{patientId} — Thông tin bệnh nhân kèm toàn bộ lịch sử khám (mọi trạng thái)
    /// và trạng thái thanh toán từng buổi — màn hình chi tiết bệnh nhân của Owner/Staff/Admin.
    /// </summary>
    [HttpGet("{patientId:guid}")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> GetPatientDetail(Guid patientId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPatientDetailQuery(patientId), ct);
        return Ok(result);
    }

    /// <summary>
    /// GET api/patients/balances — Công nợ tổng hợp của TẤT CẢ bệnh nhân: đã thanh toán / còn nợ bao
    /// nhiêu, theo từng dịch vụ — màn hình tổng hợp công nợ bệnh nhân của Owner/Staff/Admin.
    /// </summary>
    [HttpGet("balances")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> GetAllPatientsBalance(CancellationToken ct)
    {
        var result = await sender.Send(new GetAllPatientsBalanceQuery(), ct);
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
