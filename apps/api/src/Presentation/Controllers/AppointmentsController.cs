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
    TransferQueuePatientHandler transferQueuePatientHandler,
    ReorderQueuePatientHandler reorderQueuePatientHandler,
    GetDentistPatientsHandler getDentistPatientsHandler,
    DentistDashboardHandler dentistDashboardHandler,
    UpdateAppointmentStatusHandler updateAppointmentStatusHandler,
    GetExaminationHandler getExaminationHandler,
    DiagnosisHandler diagnosisHandler,
    TreatmentPlanHandler treatmentPlanHandler,
    PrescriptionHandler prescriptionHandler,
    FollowUpReminderHandler followUpReminderHandler,
    GetStaffScheduleHandler staffScheduleHandler,
    CreateWalkInAppointmentHandler createWalkInHandler,
    SummarizePatientHistoryHandler summarizePatientHistoryHandler) : ControllerBase
{
    /// <summary>POST api/appointments — Đặt lịch khám mới</summary>
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        Console.WriteLine($"[API] CreateAppointment: Received request for DentistId={request.DentistId}, PatientId={request.PatientId ?? Guid.Empty} (Null if self/not set)");
        
        var cmd = new CreateAppointmentCommand(
            userId,
            request.DentistId,
            request.AppointmentDate,
            request.Symptoms,
            request.ServiceId,
            request.PatientId);

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
    [Authorize(Roles = "Staff,Admin,Owner")]
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
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> ConfirmAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.ConfirmAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/cancel — Hủy lịch hẹn</summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Patient,Staff,Admin,Owner")]
    public async Task<IActionResult> CancelAppointment(
        Guid id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CancelAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        var reason = request?.Reason;
        await updateAppointmentStatusHandler.CancelAsync(id, reason, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/checkin — Check-in bệnh nhân (Staff/Admin)</summary>
    [HttpPut("{id}/checkin")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> CheckInAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.CheckInAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/no-show — Ghi nhận bệnh nhân vắng mặt (Staff/Admin)</summary>
    [HttpPut("{id}/no-show")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.MarkNoShowAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/start — Bắt đầu khám (Dentist/Staff/Admin)</summary>
    [HttpPut("{id}/start")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> StartTreatment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.StartTreatmentAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/complete — Hoàn thành khám (Staff/Admin)</summary>
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Staff,Admin,Owner")]
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

    /// <summary>GET api/appointments/{id}/ai-summary?force=true — Tóm tắt lịch sử khám của bệnh nhân bằng
    /// AI (Staff/Admin/Dentist). Mặc định trả cache nếu chưa có lịch khám mới; force=true bắt tạo lại.</summary>
    [HttpGet("{id}/ai-summary")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> GetPatientAiSummary(
        Guid id, [FromQuery] bool force, CancellationToken cancellationToken)
    {
        var result = await summarizePatientHistoryHandler.HandleAsync(id, force, cancellationToken);
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

    /// <summary>GET api/appointments/patients/{patientId}/treatment-plans — Tất cả liệu trình của một bệnh nhân.</summary>
    [HttpGet("patients/{patientId}/treatment-plans")]
    [Authorize(Roles = "Staff,Admin,Dentist,Owner")]
    public async Task<IActionResult> GetPatientTreatmentPlans(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await treatmentPlanHandler.GetByPatientAsync(patientId, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/treatment-plan/{treatmentPlanId}/progress — Ghi nhận bước điều trị đã thực hiện.</summary>
    [HttpPost("treatment-plan/{treatmentPlanId}/progress")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> AddTreatmentPlanProgress(
        Guid treatmentPlanId,
        [FromBody] AddStepProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await treatmentPlanHandler.AddStepProgressAsync(treatmentPlanId, request, cancellationToken);
        return Ok(result);
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

    #region Follow-up Reminder (nhắc tái khám)

    /// <summary>PUT api/appointments/{id}/follow-up-reminder — Hẹn ngày tái khám (Staff/Admin/Dentist)</summary>
    [HttpPut("{id}/follow-up-reminder")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> SetFollowUpReminder(
        Guid id,
        [FromBody] SetFollowUpReminderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await followUpReminderHandler.SetAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/{id}/follow-up-reminder — Hủy hẹn tái khám (Staff/Admin/Dentist)</summary>
    [HttpDelete("{id}/follow-up-reminder")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> ClearFollowUpReminder(Guid id, CancellationToken cancellationToken)
    {
        var result = await followUpReminderHandler.ClearAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/follow-up-due — Bệnh nhân đang chờ tái khám (Staff/Admin)</summary>
    [HttpGet("follow-up-due")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetFollowUpDue(CancellationToken cancellationToken)
    {
        var result = await followUpReminderHandler.GetDueAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/{id}/follow-up-check-in — Check-in bệnh nhân đến tái khám (Staff/Admin)</summary>
    [HttpPost("{id}/follow-up-check-in")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CheckInFollowUp(Guid id, CancellationToken cancellationToken)
    {
        var newAppointmentId = await followUpReminderHandler.CheckInAsync(id, cancellationToken);
        return Ok(new { appointmentId = newAppointmentId });
    }

    #endregion

    /// <summary>GET api/appointments/queue — Lấy hàng đợi theo bác sĩ (Staff/Admin)</summary>
    [HttpGet("queue")]
    [Authorize(Roles = "Staff,Admin,Owner")]
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

    /// <summary>PUT api/appointments/{id}/queue-room — Chuyển bệnh nhân đang chờ sang hàng đợi phòng khác (Staff/Admin)</summary>
    [HttpPut("{id}/queue-room")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> TransferQueuePatient(
        Guid id,
        [FromBody] TransferQueuePatientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await transferQueuePatientHandler.HandleAsync(id, request.RoomName, request.DentistId, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/queue-order — Đổi chỗ thứ tự với bệnh nhân liền kề trong hàng đợi (Staff/Admin)</summary>
    [HttpPut("{id}/queue-order")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> ReorderQueuePatient(
        Guid id,
        [FromBody] ReorderQueuePatientRequest request,
        CancellationToken cancellationToken)
    {
        await reorderQueuePatientHandler.HandleAsync(id, request.SwapWithAppointmentId, cancellationToken);
        return NoContent();
    }

    /// <summary>GET api/appointments/dentist/dashboard — Dữ liệu tổng quan cho bác sĩ (Dentist/Admin)</summary>
    [HttpGet("dentist/dashboard")]
    [Authorize(Roles = "Dentist,Admin")]
    public async Task<IActionResult> GetDentistDashboard(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await dentistDashboardHandler.HandleAsync(userId, cancellationToken);
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

    /// <summary>DEBUG: Kiểm tra trạng thái dữ liệu seed cho dashboard bác sĩ</summary>
    [HttpGet("debug/dashboard-state")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugDashboardState(CancellationToken cancellationToken)
    {
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vnNow  = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);
        var today  = DateOnly.FromDateTime(vnNow);
        var offset = vietnamTz.BaseUtcOffset;
        var utcStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, offset).ToUniversalTime();
        var utcEnd   = utcStart.AddDays(1);

        var dentists = await dbContext.Dentists
            .Select(d => new { d.Id, d.FullName, d.UserId })
            .ToListAsync(cancellationToken);

        var todaySchedules = await dbContext.WorkSchedules
            .Where(s => s.Date == today)
            .Select(s => new { s.StaffName, s.Shift, s.Room, s.Date })
            .ToListAsync(cancellationToken);

        var weekStart = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
        var weekSchedules = await dbContext.WorkSchedules
            .Where(s => s.Date >= weekStart && s.Date < weekStart.AddDays(7))
            .Select(s => new { s.StaffName, s.Shift, s.Date })
            .ToListAsync(cancellationToken);

        var todayAppts = await dbContext.Appointments
            .Where(a => a.AppointmentDate >= utcStart && a.AppointmentDate < utcEnd)
            .Select(a => new { a.DentistId, a.AppointmentDate, Status = a.Status.ToString() })
            .ToListAsync(cancellationToken);

        var thaoUser = await dbContext.Users
            .Where(u => u.Email == "thao.nguyen@dentalclinic.com")
            .Select(u => new { u.Id, u.Email, u.FullName })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new {
            serverVnTime   = vnNow.ToString("yyyy-MM-dd HH:mm:ss"),
            today          = today.ToString(),
            utcStart       = utcStart.ToString("o"),
            utcEnd         = utcEnd.ToString("o"),
            thaoUser,
            dentistCount   = dentists.Count,
            dentists,
            todayScheduleCount = todaySchedules.Count,
            todaySchedules,
            weekScheduleCount  = weekSchedules.Count,
            weekSchedules,
            todayApptCount     = todayAppts.Count,
            todayAppts,
        });
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

    /// <summary>GET api/appointments/staff/schedule — Lịch trống hôm nay cho staff đặt tại quầy</summary>
    [HttpGet("staff/schedule")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetStaffSchedule(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var result = await staffScheduleHandler.HandleAsync(date, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/walkin — Đặt lịch tại quầy (Staff/Admin)</summary>
    [HttpPost("walkin")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CreateWalkInAppointment(
        [FromBody] CreateWalkInRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateWalkInCommand(
            request.DentistId,
            request.AppointmentDate,
            request.PatientName,
            request.PatientPhone,
            request.DateOfBirth,
            request.Gender,
            request.ServiceId,
            request.Symptoms,
            request.PatientId);
        var result = await createWalkInHandler.HandleAsync(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/dentist/patients/past — Lấy danh sách bệnh nhân đã từng khám của bác sĩ</summary>
    [HttpGet("dentist/patients/past")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetDentistPastPatients(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        if (dentist == null)
            return NotFound(new { title = "Không tìm thấy thông tin bác sĩ." });

        var appointments = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentist.Id &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

        var patients = appointments.Select(a => new DentistPatientDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.Patient.FullName,
            CalculateAge(a.Patient.DateOfBirth),
            a.Patient.Gender ?? "Khác",
            a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
            a.AppointmentDate,
            a.Status.ToString(),
            a.Service?.Name,
            a.Symptoms,
            false,
            a.FollowUpFromAppointmentId != null
        )).ToList();

        return Ok(patients);
    }

    /// <summary>GET api/patients/{patientId}/medical-history — Lấy lịch sử khám của bệnh nhân (Dentist/Staff/Admin)</summary>
    [HttpGet("patients/{patientId}/medical-history")]
    [Authorize(Roles = "Dentist,Staff,Admin,Owner")]
    public async Task<IActionResult> GetPatientMedicalHistory(Guid patientId, CancellationToken cancellationToken)
    {
        var appointments = await dbContext.Appointments
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .Where(a => a.PatientId == patientId &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        var history = appointments.Select(a => new PatientMedicalHistoryDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.AppointmentDate,
            a.Dentist.FullName,
            a.Service?.Name ?? "Khám tổng quát",
            a.Symptoms,
            a.Diagnoses.Select(d => new MedicalHistoryDiagnosisDto(
                d.DiagnosisCode,
                d.Description,
                d.Conclusion,
                d.CreatedAt
            )).ToList(),
            a.TreatmentPlans.Select(tp => new MedicalHistoryTreatmentPlanDto(
                string.IsNullOrWhiteSpace(tp.Teeth) ? tp.Service.Name : $"{tp.Service.Name} - Răng {tp.Teeth}",
                tp.Status.ToString(),
                tp.TotalCost
            )).ToList(),
            a.Prescriptions.FirstOrDefault() != null
                ? a.Prescriptions.First().Items.Select(i => new MedicalHistoryPrescriptionItemDto(
                    i.MedicineName,
                    i.Dosage,
                    i.Quantity,
                    i.Unit
                )).ToList()
                : new List<MedicalHistoryPrescriptionItemDto>()
        )).ToList();

        return Ok(history);
    }

    private static int CalculateAge(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return 0;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value > today.AddYears(-age)) age--;
        return age;
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
    Guid? ServiceId,
    Guid? PatientId);

public record TransferQueuePatientRequest(string RoomName, Guid? DentistId = null);

public record ReorderQueuePatientRequest(Guid SwapWithAppointmentId);

public record CreateWalkInRequest(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string PatientPhone,
    DateOnly DateOfBirth,
    string Gender,
    Guid? ServiceId,
    string? Symptoms,
    Guid? PatientId = null);

public record CancelAppointmentRequest(string? Reason);

// DTOs for patient medical history
public record PatientMedicalHistoryDto(
    Guid AppointmentId,
    string AppointmentCode,
    DateTimeOffset AppointmentDate,
    string DentistName,
    string ServiceName,
    string? Symptoms,
    List<MedicalHistoryDiagnosisDto> Diagnoses,
    List<MedicalHistoryTreatmentPlanDto> TreatmentPlans,
    List<MedicalHistoryPrescriptionItemDto> PrescriptionItems);

public record MedicalHistoryDiagnosisDto(
    string DiagnosisCode,
    string Description,
    string? Conclusion,
    DateTimeOffset CreatedAt);

public record MedicalHistoryTreatmentPlanDto(
    string Description,
    string Status,
    decimal? EstimatedCost);

public record MedicalHistoryPrescriptionItemDto(
    string MedicineName,
    string Dosage,
    int Quantity,
    string Unit);
