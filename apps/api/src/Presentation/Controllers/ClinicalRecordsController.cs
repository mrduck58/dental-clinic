using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>
/// Bounded context ClinicalRecords — phiếu khám: trạng thái buổi khám (bắt đầu/hoàn thành/kết thúc
/// điều trị), chẩn đoán, liệu trình điều trị, đơn thuốc và lịch sử khám.
/// Route ghi TUYỆT ĐỐI, y hệt path cũ trong AppointmentsController.
/// </summary>
[ApiController]
public class ClinicalRecordsController(ISender sender) : ControllerBase
{
    /// <summary>PUT api/appointments/{id}/start — Bắt đầu khám (Dentist/Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/start")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> StartTreatment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new StartTreatmentCommand(id), cancellationToken);
        return Ok(new { message = "Đã bắt đầu khám." });
    }

    /// <summary>PUT api/appointments/{id}/complete — Hoàn thành khám (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/complete")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> CompleteTreatment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CompleteAppointmentCommand(id), cancellationToken);
        return Ok(new { message = "Đã hoàn thành khám." });
    }

    /// <summary>PUT api/appointments/{id}/end-treatment — Kết thúc điều trị, chuyển sang chờ thanh toán (Staff/Admin/Dentist)</summary>
    [HttpPut("api/appointments/{id}/end-treatment")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> EndTreatment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new EndTreatmentCommand(id), cancellationToken);
        return Ok(new { message = "Đã kết thúc điều trị, chuyển sang chờ thanh toán." });
    }

    /// <summary>GET api/appointments/{id}/examination — Lấy thông tin khám bệnh (Staff/Admin/Dentist)</summary>
    [HttpGet("api/appointments/{id}/examination")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> GetExamination(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExaminationQuery(id), cancellationToken);
        if (result == null)
            return NotFound(new { title = "Không tìm thấy lịch hẹn." });
        return Ok(result);
    }

    #region Diagnosis

    /// <summary>POST api/appointments/{id}/diagnosis — Thêm chuẩn đoán (Staff/Admin/Dentist)</summary>
    [HttpPost("api/appointments/{id}/diagnosis")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CreateDiagnosis(
        Guid id,
        [FromBody] CreateDiagnosisRequest request,
        CancellationToken cancellationToken)
    {
        var diagnosisRequest = request with { AppointmentId = id };
        var result = await sender.Send(diagnosisRequest, cancellationToken);
        return CreatedAtAction(nameof(GetExamination), new { id }, result);
    }

    /// <summary>PUT api/appointments/diagnosis/{diagnosisId} — Cập nhật chuẩn đoán (Staff/Admin/Dentist)</summary>
    [HttpPut("api/appointments/diagnosis/{diagnosisId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdateDiagnosis(
        Guid diagnosisId,
        [FromBody] UpdateDiagnosisRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { DiagnosisId = diagnosisId };
        var result = await sender.Send(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/diagnosis/{diagnosisId} — Xóa chuẩn đoán (Staff/Admin/Dentist)</summary>
    [HttpDelete("api/appointments/diagnosis/{diagnosisId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeleteDiagnosis(Guid diagnosisId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDiagnosisCommand(diagnosisId), cancellationToken);
        return Ok(new { message = "Đã xóa chuẩn đoán." });
    }

    #endregion

    #region Treatment Plan

    /// <summary>POST api/appointments/{id}/treatment-plan — Thêm liệu trình điều trị (Staff/Admin/Dentist)</summary>
    [HttpPost("api/appointments/{id}/treatment-plan")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CreateTreatmentPlan(
        Guid id,
        [FromBody] CreateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        var treatmentPlanRequest = request with { AppointmentId = id };
        var result = await sender.Send(treatmentPlanRequest, cancellationToken);
        return CreatedAtAction(nameof(GetExamination), new { id }, result);
    }

    /// <summary>PUT api/appointments/treatment-plan/{treatmentPlanId} — Cập nhật liệu trình (Staff/Admin/Dentist)</summary>
    [HttpPut("api/appointments/treatment-plan/{treatmentPlanId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdateTreatmentPlan(
        Guid treatmentPlanId,
        [FromBody] UpdateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { TreatmentPlanId = treatmentPlanId };
        var result = await sender.Send(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/treatment-plan/{treatmentPlanId} — Xóa liệu trình (Staff/Admin/Dentist)</summary>
    [HttpDelete("api/appointments/treatment-plan/{treatmentPlanId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeleteTreatmentPlan(Guid treatmentPlanId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTreatmentPlanCommand(treatmentPlanId), cancellationToken);
        return Ok(new { message = "Đã xóa liệu trình điều trị." });
    }

    /// <summary>GET api/appointments/patients/{patientId}/treatment-plans — Tất cả liệu trình của một bệnh nhân.</summary>
    [HttpGet("api/appointments/patients/{patientId}/treatment-plans")]
    [Authorize(Roles = "Staff,Admin,Dentist,Owner")]
    public async Task<IActionResult> GetPatientTreatmentPlans(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPatientTreatmentPlansQuery(patientId), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/treatment-plan/{treatmentPlanId}/progress — Ghi nhận bước điều trị đã thực hiện.</summary>
    [HttpPost("api/appointments/treatment-plan/{treatmentPlanId}/progress")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> AddTreatmentPlanProgress(
        Guid treatmentPlanId,
        [FromBody] AddStepProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddStepProgressCommand(treatmentPlanId, request), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/treatment-plan/{treatmentPlanId}/progress — Sửa một mục trong nhật ký điều trị.</summary>
    [HttpPut("api/appointments/treatment-plan/{treatmentPlanId}/progress")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdateTreatmentPlanProgress(
        Guid treatmentPlanId,
        [FromBody] UpdateStepProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateStepProgressCommand(treatmentPlanId, request), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/treatment-plan/{treatmentPlanId}/progress/reorder — Đổi thứ tự các mục nhật ký điều trị.</summary>
    [HttpPut("api/appointments/treatment-plan/{treatmentPlanId}/progress/reorder")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> ReorderTreatmentPlanProgress(
        Guid treatmentPlanId,
        [FromBody] ReorderStepProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReorderStepProgressCommand(treatmentPlanId, request), cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/treatment-plan/{treatmentPlanId}/progress/{entryIndex} — Xóa một mục trong nhật ký điều trị.</summary>
    [HttpDelete("api/appointments/treatment-plan/{treatmentPlanId}/progress/{entryIndex:int}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeleteTreatmentPlanProgress(
        Guid treatmentPlanId,
        int entryIndex,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteStepProgressCommand(treatmentPlanId, entryIndex), cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Prescription

    /// <summary>POST api/appointments/{id}/prescription — Tạo đơn thuốc (Staff/Admin/Dentist)</summary>
    [HttpPost("api/appointments/{id}/prescription")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CreatePrescription(
        Guid id,
        [FromBody] CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var prescriptionRequest = request with { AppointmentId = id };
        var result = await sender.Send(prescriptionRequest, cancellationToken);
        return CreatedAtAction(nameof(GetExamination), new { id }, result);
    }

    /// <summary>PUT api/appointments/prescription/{prescriptionId} — Cập nhật đơn thuốc (Staff/Admin/Dentist)</summary>
    [HttpPut("api/appointments/prescription/{prescriptionId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdatePrescription(
        Guid prescriptionId,
        [FromBody] UpdatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { PrescriptionId = prescriptionId };
        var result = await sender.Send(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/prescription/{prescriptionId}/items — Thêm thuốc vào đơn (Staff/Admin/Dentist)</summary>
    [HttpPost("api/appointments/prescription/{prescriptionId}/items")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> AddPrescriptionItem(
        Guid prescriptionId,
        [FromBody] AddPrescriptionItemRequest request,
        CancellationToken cancellationToken)
    {
        var itemRequest = request with { PrescriptionId = prescriptionId };
        var result = await sender.Send(itemRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/prescription-items/{itemId} — Cập nhật thuốc trong đơn (Staff/Admin/Dentist)</summary>
    [HttpPut("api/appointments/prescription-items/{itemId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdatePrescriptionItem(
        Guid itemId,
        [FromBody] UpdatePrescriptionItemRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { ItemId = itemId };
        var result = await sender.Send(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/prescription-items/{itemId} — Xóa thuốc khỏi đơn (Staff/Admin/Dentist)</summary>
    [HttpDelete("api/appointments/prescription-items/{itemId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeletePrescriptionItem(Guid itemId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePrescriptionItemCommand(itemId), cancellationToken);
        return Ok(new { message = "Đã xóa thuốc khỏi đơn." });
    }

    #endregion

    #region Lịch sử khám của bệnh nhân

    /// <summary>
    /// GET api/appointments/my/treatment-plans — Liệu trình điều trị (kèm nhật ký tiến độ thật) của chính
    /// bệnh nhân đang đăng nhập và các thành viên gia đình. Lọc theo patientId nếu chỉ muốn xem 1 thành viên.
    /// </summary>
    [HttpGet("api/appointments/my/treatment-plans")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyTreatmentPlans([FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetMyTreatmentPlansQuery(userId, patientId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET api/appointments/my/medical-history — Lịch sử khám của chính bệnh nhân đang đăng nhập
    /// (và các thành viên gia đình dưới tài khoản này). Lọc theo patientId nếu chỉ muốn xem 1 thành viên.
    /// </summary>
    [HttpGet("api/appointments/my/medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyMedicalHistory([FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetMyExaminationHistoryQuery(userId, patientId), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/patients/{patientId}/medical-history — Lấy lịch sử khám của bệnh nhân (Dentist/Staff/Admin)</summary>
    [HttpGet("api/appointments/patients/{patientId}/medical-history")]
    [Authorize(Roles = "Dentist,Staff,Admin,Owner")]
    public async Task<IActionResult> GetPatientMedicalHistory(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPatientMedicalHistoryQuery(patientId), cancellationToken);
        return Ok(result);
    }

    #endregion

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        return Guid.Parse(sub);
    }
}
