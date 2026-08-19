using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>
/// Bounded context Booking — đặt lịch / tra lịch hẹn / đổi trạng thái trước khi vào khám.
/// Tách ra từ AppointmentsController (919 dòng, inject 22 handler + AppDbContext).
/// Route được ghi TUYỆT ĐỐI ở từng attribute, y hệt path cũ (3 app frontend đang hardcode).
/// </summary>
[ApiController]
public class AppointmentBookingController(ISender sender) : ControllerBase
{
    /// <summary>POST api/appointments — Đặt lịch khám mới</summary>
    [HttpPost("api/appointments")]
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

        var result = await sender.Send(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/my — Lịch hẹn của bệnh nhân hiện tại</summary>
    [HttpGet("api/appointments/my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetMyAppointmentsQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/booking-eligibility — Kiểm tra giới hạn 2 booking và cooldown của bệnh nhân</summary>
    [HttpGet("api/appointments/booking-eligibility")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetBookingEligibility(
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetBookingEligibilityQuery(userId, patientId), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/hold-slot — Giữ tạm thời một ca khám trong 5 phút</summary>
    [HttpPost("api/appointments/hold-slot")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> HoldSlot(
        [FromBody] HoldSlotRequest request,
        [FromServices] HoldSlotHandler handler,
        CancellationToken cancellationToken)
    {
        var cmd = new HoldSlotCommand(
            request.PatientId,
            request.DentistId,
            request.Date,
            request.TimeSlot);

        var result = await handler.Handle(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/release-hold — Giải phóng ca khám đang giữ tạm</summary>
    [HttpPost("api/appointments/release-hold")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> ReleaseHold(
        [FromBody] ReleaseHoldRequest request,
        [FromServices] ReleaseSlotHoldHandler handler,
        CancellationToken cancellationToken)
    {
        var cmd = new ReleaseSlotHoldCommand(
            request.PatientId,
            request.DentistId,
            request.Date,
            request.TimeSlot);

        var result = await handler.Handle(cmd, cancellationToken);
        return Ok(new { success = result });
    }

    /// <summary>GET api/appointments/active-hold — Lấy ca khám đang giữ tạm của bệnh nhân</summary>
    [HttpGet("api/appointments/active-hold")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetActiveHold(
        [FromQuery] Guid patientId,
        [FromServices] GetActiveSlotHoldHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(patientId, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments — Danh sách tất cả lịch hẹn (Staff/Admin)</summary>
    [HttpGet("api/appointments")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> GetAllAppointments(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllAppointmentsQuery(date, status), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/paged — Danh sách lịch hẹn có phân trang, lọc khoảng ngày/nhiều trạng thái/tìm kiếm (màn "Ca khám &amp; điều trị" của Owner)</summary>
    [HttpGet("api/appointments/paged")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> GetAppointmentsPaged(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAppointmentsPagedQuery(startDate, endDate, status, search, page, pageSize, sortDir),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/confirm — Xác nhận lịch hẹn (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/confirm")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> ConfirmAppointment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmAppointmentCommand(id), cancellationToken);
        return Ok(new { message = "Đã xác nhận lịch hẹn." });
    }

    /// <summary>
    /// GET api/appointments/cancellation-reasons — Danh sách lý do hủy để client dựng danh sách chọn.
    /// Trả về từ server thay vì mỗi app tự chép cứng, để thêm/sửa lý do không cần phát hành lại app
    /// và hai client không lệch nhau. Bệnh nhân không thấy các lý do dành riêng cho phòng khám.
    /// </summary>
    [HttpGet("api/appointments/cancellation-reasons")]
    [Authorize(Roles = "Patient,Staff,Admin,Owner")]
    public async Task<IActionResult> GetCancellationReasons(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCancellationReasonsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/cancel — Hủy lịch hẹn kèm lý do</summary>
    [HttpPut("api/appointments/{id}/cancel")]
    [Authorize(Roles = "Patient,Staff,Admin,Owner")]
    public async Task<IActionResult> CancelAppointment(
        Guid id,
        [FromBody] CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var reason = CancellationReasonCatalog.Parse(request.Reason);
        await sender.Send(new CancelAppointmentCommand(id, reason, request.Note), cancellationToken);
        return Ok(new { message = "Đã hủy lịch hẹn." });
    }

    /// <summary>PUT api/appointments/{id}/reschedule — Dời lịch sang khung giờ (và bác sĩ) khác</summary>
    [HttpPut("api/appointments/{id}/reschedule")]
    [Authorize(Roles = "Patient,Staff,Admin,Owner")]
    public async Task<IActionResult> RescheduleAppointment(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RescheduleAppointmentCommand(
                id, request.AppointmentDate, request.DentistId, request.ServiceId, request.Reason),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/checkin — Check-in bệnh nhân (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/checkin")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> CheckInAppointment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CheckInAppointmentCommand(id), cancellationToken);
        return Ok(new { message = "Đã check-in bệnh nhân." });
    }

    /// <summary>
    /// PUT api/appointments/{id}/undo-checkin — Gỡ một lần check-in bấm nhầm (Staff/Admin).
    /// Lịch hẹn quay về trạng thái Confirmed (danh sách Đang chờ check-in).
    /// </summary>
    [HttpPut("api/appointments/{id}/undo-checkin")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> UndoCheckIn(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UndoCheckInCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/no-show — Ghi nhận bệnh nhân vắng mặt (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/no-show")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNoShowCommand(id), cancellationToken);
        return Ok(new { message = "Đã ghi nhận bệnh nhân vắng mặt." });
    }

    /// <summary>
    /// PUT api/appointments/{id}/undo-noshow — Gỡ một lần ghi nhận vắng mặt bấm nhầm (Staff/Admin).
    /// Lịch quay về Confirmed, chờ bệnh nhân đến check-in.
    /// </summary>
    [HttpPut("api/appointments/{id}/undo-noshow")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> UndoNoShow(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new UndoNoShowCommand(id), cancellationToken);
        return Ok(new { message = "Đã hoàn tác ghi nhận vắng mặt." });
    }

    /// <summary>GET api/appointments/staff/schedule — Lịch trống hôm nay cho staff đặt tại quầy</summary>
    [HttpGet("api/appointments/staff/schedule")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetStaffSchedule(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStaffScheduleQuery(date), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/walkin — Đặt lịch tại quầy (Staff/Admin)</summary>
    [HttpPost("api/appointments/walkin")]
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
            request.PatientId,
            request.PatientEmail,
            request.EmailVerificationCode);
        var result = await sender.Send(cmd, cancellationToken);
        return Ok(result);
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

public record CreateWalkInRequest(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string PatientPhone,
    DateOnly DateOfBirth,
    string Gender,
    Guid? ServiceId,
    string? Symptoms,
    Guid? PatientId = null,
    /// <summary>
    /// Có email thì bệnh nhân mới được lập TÀI KHOẢN THẬT để lần sau tự đặt lịch trên app. Bỏ trống
    /// vẫn khám được — chỉ tạo hồ sơ không tài khoản, dành cho người không dùng email.
    /// </summary>
    string? PatientEmail = null,
    /// <summary>Mã bệnh nhân đọc từ hộp thư — thiếu nó thì chỉ tạo hồ sơ, không cấp tài khoản.</summary>
    string? EmailVerificationCode = null);

/// <param name="Reason">
/// Mã lý do lấy từ GET api/appointments/cancellation-reasons, ví dụ "PatientRequested".
/// Khai báo là string chứ không phải enum: dự án không cấu hình JsonStringEnumConverter nên
/// System.Text.Json chỉ bind được enum từ SỐ — để enum ở đây thì mọi mã chữ đều bị từ chối
/// ngay ở bước bind, trả 400 trước khi vào controller.
/// </param>
/// <param name="Note">Ghi chú tự do; bắt buộc với các lý do có RequiresNote = true.</param>
public record CancelAppointmentRequest(string Reason, string? Note);

/// <param name="DentistId">Bỏ trống để giữ nguyên bác sĩ hiện tại.</param>
/// <param name="ServiceId">Bỏ trống để giữ nguyên dịch vụ hiện tại.</param>
public record RescheduleAppointmentRequest(
    DateTimeOffset AppointmentDate,
    Guid? DentistId,
    Guid? ServiceId,
    string? Reason);

public record HoldSlotRequest(
    Guid PatientId,
    Guid DentistId,
    DateOnly Date,
    string TimeSlot);

public record ReleaseHoldRequest(
    Guid PatientId,
    Guid DentistId,
    DateOnly Date,
    string TimeSlot);
