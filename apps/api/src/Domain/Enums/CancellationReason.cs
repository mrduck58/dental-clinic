namespace DentalClinic.API.Domain.Enums;

/// <summary>
/// Lý do hủy lịch hẹn, dạng có cấu trúc để thống kê được — trước đây lý do bị nối vào cột Notes
/// dạng văn bản tự do nên không trả lời được câu hỏi quan trọng nhất mà dữ liệu này sinh ra để trả lời:
/// lịch hẹn bị bỏ chủ yếu vì gì, và do phía bệnh nhân hay phía phòng khám.
///
/// Ghi chú tự do đi kèm nằm ở <c>Appointment.CancellationNote</c>, không gộp vào đây.
/// Ai được chọn giá trị nào do <c>CancellationReasonCatalog</c> quy định.
/// </summary>
public enum CancellationReason
{
    // ── Bệnh nhân tự hủy trên app ─────────────────────────────────────────────

    /// <summary>Bận việc đột xuất, đổi kế hoạch cá nhân.</summary>
    ChangeOfPlans,

    /// <summary>Trùng lịch với công việc khác.</summary>
    ScheduleConflict,

    /// <summary>Ốm, mệt, không tới khám được.</summary>
    HealthIssue,

    /// <summary>Chuyển sang phòng khám khác.</summary>
    FoundAnotherClinic,

    // ── Nhân viên phòng khám hủy / từ chối ────────────────────────────────────

    /// <summary>Bệnh nhân gọi điện tới quầy xin hủy, lễ tân hủy hộ.</summary>
    PatientRequested,

    /// <summary>Gọi xác nhận nhiều lần không được, không dám giữ chỗ tiếp.</summary>
    PatientUnreachable,

    /// <summary>Bác sĩ nghỉ phép, nghỉ đột xuất hoặc kín ca.</summary>
    DentistUnavailable,

    /// <summary>Khung giờ đã kín hoặc vượt khả năng tiếp nhận của phòng khám.</summary>
    SlotUnavailable,

    /// <summary>Phòng khám nghỉ lễ, mất điện, sự cố thiết bị.</summary>
    ClinicClosed,

    /// <summary>Bệnh nhân đặt trùng nhiều lịch cho cùng một buổi.</summary>
    DuplicateBooking,

    // ── Dùng chung ────────────────────────────────────────────────────────────

    /// <summary>Không thuộc các nhóm trên — bắt buộc kèm ghi chú tự do ở CancellationNote.</summary>
    Other,

    /// <summary>
    /// CŨ — đã tách thành <see cref="DentistUnavailable"/> và <see cref="ClinicClosed"/> vì "phòng khám
    /// không tiếp nhận được" quá chung để làm báo cáo. Giữ lại giá trị này để EF đọc được những bản ghi
    /// đã lưu trước khi tách; không còn xuất hiện trong danh sách cho người dùng chọn.
    /// </summary>
    ClinicUnavailable,
}
