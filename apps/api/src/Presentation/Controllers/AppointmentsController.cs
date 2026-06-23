using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(
    AppDbContext dbContext,
    CreateAppointmentHandler createAppointmentHandler,
    GetMyAppointmentsHandler getMyAppointmentsHandler,
    GetAllAppointmentsHandler getAllAppointmentsHandler,
    GetWaitingQueueHandler getWaitingQueueHandler,
    GetDentistPatientsHandler getDentistPatientsHandler,
    UpdateAppointmentStatusHandler updateAppointmentStatusHandler,
    GetExaminationHandler getExaminationHandler,
    DiagnosisHandler diagnosisHandler,
    TreatmentPlanHandler treatmentPlanHandler,
    PrescriptionHandler prescriptionHandler,
    FollowUpAppointmentHandler followUpAppointmentHandler) : ControllerBase
{
    /// <summary>POST api/appointments — Đặt lịch khám mới</summary>
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var cmd = new CreateAppointmentCommand(
            userId,
            request.DentistId,
            request.AppointmentDate,
            request.Symptoms,
            request.ServiceId);

        var result = await createAppointmentHandler.HandleAsync(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/my — Lịch hẹn của bệnh nhân hiện tại</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await getMyAppointmentsHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments — Danh sách tất cả lịch hẹn (Staff/Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetAllAppointments(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await getAllAppointmentsHandler.HandleAsync(date, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/confirm — Xác nhận lịch hẹn (Staff/Admin)</summary>
    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> ConfirmAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.ConfirmAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/cancel — Hủy lịch hẹn (Staff/Admin)</summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.CancelAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/checkin — Check-in bệnh nhân (Staff/Admin)</summary>
    [HttpPut("{id}/checkin")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CheckInAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.CheckInAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/start — Bắt đầu khám (Staff/Admin)</summary>
    [HttpPut("{id}/start")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> StartTreatment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.StartTreatmentAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/complete — Hoàn thành khám (Staff/Admin)</summary>
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CompleteTreatment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.CompleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/end-treatment — Kết thúc điều trị, chuyển sang chờ thanh toán (Staff/Admin/Dentist)</summary>
    [HttpPut("{id}/end-treatment")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> EndTreatment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.EndTreatmentAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>GET api/appointments/{id}/examination — Lấy thông tin khám bệnh (Staff/Admin/Dentist)</summary>
    [HttpGet("{id}/examination")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> GetExamination(Guid id, CancellationToken cancellationToken)
    {
        var result = await getExaminationHandler.HandleAsync(id, cancellationToken);
        if (result == null)
            return NotFound(new { title = "Không tìm thấy lịch hẹn." });
        return Ok(result);
    }

    #region Diagnosis

    /// <summary>POST api/appointments/{id}/diagnosis — Thêm chuẩn đoán (Staff/Admin/Dentist)</summary>
    [HttpPost("{id}/diagnosis")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CreateDiagnosis(
        Guid id,
        [FromBody] CreateDiagnosisRequest request,
        CancellationToken cancellationToken)
    {
        var diagnosisRequest = request with { AppointmentId = id };
        var result = await diagnosisHandler.CreateAsync(diagnosisRequest, cancellationToken);
        return CreatedAtAction(nameof(GetExamination), new { id }, result);
    }

    /// <summary>PUT api/appointments/diagnosis/{diagnosisId} — Cập nhật chuẩn đoán (Staff/Admin/Dentist)</summary>
    [HttpPut("diagnosis/{diagnosisId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdateDiagnosis(
        Guid diagnosisId,
        [FromBody] UpdateDiagnosisRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { DiagnosisId = diagnosisId };
        var result = await diagnosisHandler.UpdateAsync(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/diagnosis/{diagnosisId} — Xóa chuẩn đoán (Staff/Admin/Dentist)</summary>
    [HttpDelete("diagnosis/{diagnosisId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeleteDiagnosis(Guid diagnosisId, CancellationToken cancellationToken)
    {
        await diagnosisHandler.DeleteAsync(diagnosisId, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Treatment Plan

    /// <summary>POST api/appointments/{id}/treatment-plan — Thêm liệu trình điều trị (Staff/Admin/Dentist)</summary>
    [HttpPost("{id}/treatment-plan")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CreateTreatmentPlan(
        Guid id,
        [FromBody] CreateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        var treatmentPlanRequest = request with { AppointmentId = id };
        var result = await treatmentPlanHandler.CreateAsync(treatmentPlanRequest, cancellationToken);
        return CreatedAtAction(nameof(GetExamination), new { id }, result);
    }

    /// <summary>PUT api/appointments/treatment-plan/{treatmentPlanId} — Cập nhật liệu trình (Staff/Admin/Dentist)</summary>
    [HttpPut("treatment-plan/{treatmentPlanId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdateTreatmentPlan(
        Guid treatmentPlanId,
        [FromBody] UpdateTreatmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { TreatmentPlanId = treatmentPlanId };
        var result = await treatmentPlanHandler.UpdateAsync(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/treatment-plan/{treatmentPlanId} — Xóa liệu trình (Staff/Admin/Dentist)</summary>
    [HttpDelete("treatment-plan/{treatmentPlanId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeleteTreatmentPlan(Guid treatmentPlanId, CancellationToken cancellationToken)
    {
        await treatmentPlanHandler.DeleteAsync(treatmentPlanId, cancellationToken);
        return NoContent();
    }

    /// <summary>POST api/appointments/treatment-plan/{treatmentPlanId}/steps — Thêm bước điều trị (Staff/Admin/Dentist)</summary>
    [HttpPost("treatment-plan/{treatmentPlanId}/steps")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> AddTreatmentStep(
        Guid treatmentPlanId,
        [FromBody] AddTreatmentStepRequest request,
        CancellationToken cancellationToken)
    {
        var stepRequest = request with { TreatmentPlanId = treatmentPlanId };
        var result = await treatmentPlanHandler.AddStepAsync(stepRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/treatment-steps/{stepId} — Cập nhật bước điều trị (Staff/Admin/Dentist)</summary>
    [HttpPut("treatment-steps/{stepId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdateTreatmentStep(
        Guid stepId,
        [FromBody] UpdateTreatmentStepRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { StepId = stepId };
        var result = await treatmentPlanHandler.UpdateStepAsync(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/treatment-steps/{stepId}/complete — Hoàn thành bước điều trị (Staff/Admin/Dentist)</summary>
    [HttpPut("treatment-steps/{stepId}/complete")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CompleteTreatmentStep(Guid stepId, CancellationToken cancellationToken)
    {
        var result = await treatmentPlanHandler.CompleteStepAsync(stepId, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/treatment-steps/{stepId} — Xóa bước điều trị (Staff/Admin/Dentist)</summary>
    [HttpDelete("treatment-steps/{stepId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeleteTreatmentStep(Guid stepId, CancellationToken cancellationToken)
    {
        await treatmentPlanHandler.DeleteStepAsync(stepId, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Prescription

    /// <summary>POST api/appointments/{id}/prescription — Tạo đơn thuốc (Staff/Admin/Dentist)</summary>
    [HttpPost("{id}/prescription")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CreatePrescription(
        Guid id,
        [FromBody] CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var prescriptionRequest = request with { AppointmentId = id };
        var result = await prescriptionHandler.CreateAsync(prescriptionRequest, cancellationToken);
        return CreatedAtAction(nameof(GetExamination), new { id }, result);
    }

    /// <summary>PUT api/appointments/prescription/{prescriptionId} — Cập nhật đơn thuốc (Staff/Admin/Dentist)</summary>
    [HttpPut("prescription/{prescriptionId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdatePrescription(
        Guid prescriptionId,
        [FromBody] UpdatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { PrescriptionId = prescriptionId };
        var result = await prescriptionHandler.UpdateAsync(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/prescription/{prescriptionId}/items — Thêm thuốc vào đơn (Staff/Admin/Dentist)</summary>
    [HttpPost("prescription/{prescriptionId}/items")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> AddPrescriptionItem(
        Guid prescriptionId,
        [FromBody] AddPrescriptionItemRequest request,
        CancellationToken cancellationToken)
    {
        var itemRequest = request with { PrescriptionId = prescriptionId };
        var result = await prescriptionHandler.AddItemAsync(itemRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/prescription-items/{itemId} — Cập nhật thuốc trong đơn (Staff/Admin/Dentist)</summary>
    [HttpPut("prescription-items/{itemId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> UpdatePrescriptionItem(
        Guid itemId,
        [FromBody] UpdatePrescriptionItemRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request with { ItemId = itemId };
        var result = await prescriptionHandler.UpdateItemAsync(updateRequest, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/prescription-items/{itemId} — Xóa thuốc khỏi đơn (Staff/Admin/Dentist)</summary>
    [HttpDelete("prescription-items/{itemId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeletePrescriptionItem(Guid itemId, CancellationToken cancellationToken)
    {
        await prescriptionHandler.DeleteItemAsync(itemId, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Follow-up Appointment

    /// <summary>POST api/appointments/{id}/follow-up — Tạo lịch tái khám (Staff/Admin/Dentist)</summary>
    [HttpPost("{id}/follow-up")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> CreateFollowUpAppointment(
        Guid id,
        [FromBody] CreateFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        var followUpRequest = request with { OriginalAppointmentId = id };
        var result = await followUpAppointmentHandler.CreateAsync(followUpRequest, cancellationToken);
        return CreatedAtAction(nameof(GetExamination), new { id }, result);
    }

    /// <summary>GET api/appointments/{id}/follow-ups — Lấy danh sách lịch tái khám (Staff/Admin/Dentist)</summary>
    [HttpGet("{id}/follow-ups")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> GetFollowUpAppointments(Guid id, CancellationToken cancellationToken)
    {
        var result = await followUpAppointmentHandler.GetByOriginalAppointmentAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/follow-up/{followUpId} — Xóa lịch tái khám (Staff/Admin/Dentist)</summary>
    [HttpDelete("follow-up/{followUpId}")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> DeleteFollowUpAppointment(Guid followUpId, CancellationToken cancellationToken)
    {
        await followUpAppointmentHandler.DeleteAsync(followUpId, cancellationToken);
        return NoContent();
    }

    #endregion

    /// <summary>GET api/appointments/queue — Lấy hàng đợi theo bác sĩ (Staff/Admin)</summary>
    [HttpGet("queue")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetWaitingQueue(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);
        var queryDate = date ?? DateOnly.FromDateTime(vietnamNow);
        var result = await getWaitingQueueHandler.HandleAsync(queryDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/dentist/patients — Lấy danh sách bệnh nhân của bác sĩ trong ngày (Dentist)</summary>
    [HttpGet("dentist/patients")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetDentistPatients(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        if (dentist == null)
            return NotFound(new { title = "Không tìm thấy thông tin bác sĩ." });

        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);
        var queryDate = date ?? DateOnly.FromDateTime(vietnamNow);

        var result = await getDentistPatientsHandler.HandleAsync(dentist.Id, queryDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>DEBUG: Check appointment status values in database</summary>
    [HttpGet("debug/status")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugStatusValues(CancellationToken cancellationToken)
    {
        var appointments = await dbContext.Appointments
            .Select(a => new { a.Id, Status = (int)a.Status, StatusName = a.Status.ToString() })
            .Take(20)
            .ToListAsync(cancellationToken);
        return Ok(new {
            EnumDefinition = Enum.GetValues<AppointmentStatus>()
                .Select(s => new { Value = (int)s, Name = s.ToString() }),
            Appointments = appointments
        });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        return Guid.Parse(sub);
    }
}

public record CreateAppointmentRequest(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string? Symptoms,
    Guid? ServiceId);
