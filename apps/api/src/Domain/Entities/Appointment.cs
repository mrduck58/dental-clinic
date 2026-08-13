using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Entities;

public class Appointment
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public Guid? ServiceId { get; private set; }
    public DateTimeOffset AppointmentDate { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? Symptoms { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Thời điểm bệnh nhân check-in — mốc tính THỜI GIAN CHỜ (null nếu chưa check-in).</summary>
    public DateTimeOffset? CheckedInAt { get; private set; }

    /// <summary>Tóm tắt lịch sử khám do AI tạo, gắn với LẦN KHÁM NÀY (dựa trên các lịch hẹn trước đó
    /// của cùng bệnh nhân). Cache lại để không gọi Gemini lại mỗi lần bác sĩ mở trang nếu dữ liệu
    /// chưa đổi — xem <see cref="AiSummaryBasedOnCount"/> để biết cache còn hợp lệ hay không.</summary>
    public string? AiSummary { get; private set; }
    public DateTimeOffset? AiSummaryGeneratedAt { get; private set; }

    /// <summary>Số lịch hẹn trước đó đã dùng để tạo <see cref="AiSummary"/> — nếu số này khác với số
    /// lịch hẹn trước đó hiện tại (có thêm lịch mới), cache coi như cũ và phải tạo lại.</summary>
    public int? AiSummaryBasedOnCount { get; private set; }

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public DentistProfile Dentist { get; private set; } = null!;
    public Service? Service { get; private set; }
    public ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();

    // Examination related
    public ICollection<Diagnosis> Diagnoses { get; private set; } = new List<Diagnosis>();
    public ICollection<TreatmentPlan> TreatmentPlans { get; private set; } = new List<TreatmentPlan>();
    public ICollection<Prescription> Prescriptions { get; private set; } = new List<Prescription>();

    // Follow-up appointments (dữ liệu lịch sử — luồng tạo lịch tái khám cũ đã bỏ)
    public Guid? FollowUpFromAppointmentId { get; private set; }
    public Appointment? FollowUpFromAppointment { get; private set; }
    public ICollection<Appointment> FollowUpAppointments { get; private set; } = new List<Appointment>();

    // Nhắc tái khám: chỉ hẹn ngày khám lại, không đặt lịch mới.
    // Khi bác sĩ kết thúc điều trị, hệ thống gửi thông báo cho bệnh nhân.
    public DateOnly? FollowUpDate { get; private set; }
    public string? FollowUpNote { get; private set; }

    // ── Hủy lịch ──────────────────────────────────────────────────────────────
    public Enums.CancellationReason? CancellationReason { get; private set; }

    /// <summary>Ghi chú tự do bệnh nhân/nhân viên nhập kèm lý do hủy.</summary>
    public string? CancellationNote { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>Ai bấm hủy — phân biệt bệnh nhân tự hủy với phòng khám hủy, hai việc rất khác nhau khi làm báo cáo.</summary>
    public Guid? CancelledByUserId { get; private set; }

    // ── Dời lịch ──────────────────────────────────────────────────────────────
    public int RescheduledCount { get; private set; }
    public DateTimeOffset? LastRescheduledAt { get; private set; }

    private Appointment() { }

    public static Appointment Create(
        Guid patientId,
        Guid dentistId,
        DateTimeOffset appointmentDate,
        string? symptoms = null,
        Guid? serviceId = null,
        string? notes = null)
    {
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            ServiceId = serviceId,
            AppointmentDate = appointmentDate,
            Status = AppointmentStatus.Pending,
            Symptoms = symptoms,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Confirm() => Status = AppointmentStatus.Confirmed;
    public void CheckIn()
    {
        Status = AppointmentStatus.CheckedIn;
        CheckedInAt = DateTimeOffset.UtcNow;
    }
    /// <summary>Ghi nhận bệnh nhân vắng mặt — đã xác nhận nhưng không đến khám.</summary>
    public void MarkNoShow() => Status = AppointmentStatus.NoShow;
    public void StartTreatment() => Status = AppointmentStatus.InProgress;
    public void EndTreatment() => Status = AppointmentStatus.PendingPayment;
    public void Complete() => Status = AppointmentStatus.Completed;
    /// <summary>
    /// Các trạng thái còn hủy hoặc dời được. Sau khi bệnh nhân đã check-in thì buổi khám đã bắt đầu
    /// diễn ra trên thực tế — hủy lúc đó sẽ tạo ra lịch hẹn "đã hủy" nhưng vẫn kèm bệnh án và hóa đơn,
    /// một trạng thái vô nghĩa mà phần còn lại của hệ thống không xử lý được.
    /// </summary>
    private static readonly AppointmentStatus[] ChangeableStatuses =
        [AppointmentStatus.Pending, AppointmentStatus.Confirmed];

    public bool CanBeChanged => ChangeableStatuses.Contains(Status);

    /// <summary>
    /// Hủy lịch. <paramref name="reason"/> là nhóm lý do để thống kê, <paramref name="note"/> là
    /// ghi chú tự do (bắt buộc khi chọn <see cref="CancellationReason.Other"/> — chọn "lý do khác"
    /// mà không nói khác thế nào thì không ghi nhận được gì).
    /// </summary>
    // Enums.CancellationReason chỉ đích danh vì thuộc tính cùng tên ở trên che khuất tên kiểu trong class này.
    public void Cancel(Enums.CancellationReason reason, string? note, Guid? cancelledByUserId, DateTimeOffset now)
    {
        if (!CanBeChanged)
            throw new ConflictException(
                $"Không thể hủy lịch hẹn đang ở trạng thái '{Status}'. Chỉ hủy được lịch chờ xác nhận hoặc đã xác nhận.");

        if (reason == Enums.CancellationReason.Other && string.IsNullOrWhiteSpace(note))
            throw new ValidationException("Vui lòng nhập lý do hủy cụ thể.");

        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        CancellationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        CancelledAt = now;
        CancelledByUserId = cancelledByUserId;
    }

    /// <summary>
    /// Dời lịch sang khung giờ (và có thể là bác sĩ / dịch vụ) khác. Việc khung giờ mới có trống hay
    /// không do tầng ứng dụng kiểm tra trước khi gọi — entity không truy cập được lịch hẹn khác.
    ///
    /// <paramref name="requiresReconfirmation"/>: bệnh nhân tự dời thì lịch quay về Pending vì với
    /// phòng khám đây là khung giờ mới cần sắp xếp lại nhân sự; nhân viên dời thì giữ nguyên trạng
    /// thái vì chính họ đang là người sắp xếp.
    /// </summary>
    public void Reschedule(
        DateTimeOffset newDate,
        Guid newDentistId,
        Guid? newServiceId,
        bool requiresReconfirmation,
        DateTimeOffset now)
    {
        if (!CanBeChanged)
            throw new ConflictException(
                $"Không thể dời lịch hẹn đang ở trạng thái '{Status}'. Chỉ dời được lịch chờ xác nhận hoặc đã xác nhận.");

        AppointmentDate = newDate;
        DentistId = newDentistId;
        ServiceId = newServiceId;
        RescheduledCount++;
        LastRescheduledAt = now;

        if (requiresReconfirmation)
            Status = AppointmentStatus.Pending;
    }

    /// <summary>
    /// Chuyển buổi hẹn sang bác sĩ khác. Dùng khi lễ tân kéo bệnh nhân đang chờ
    /// sang hàng đợi của phòng khác — phòng được quyết định bởi bác sĩ phụ trách.
    /// Giữ nguyên <see cref="CheckedInAt"/> để bệnh nhân không mất lượt ở hàng đợi mới.
    /// </summary>
    public void ReassignDentist(Guid dentistId) => DentistId = dentistId;

    /// <summary>
    /// Đặt vị trí hàng đợi thủ công (tick UTC): đẩy lên/xuống khi lễ tân đổi thứ tự, hoặc đưa xuống
    /// cuối hàng đợi phòng mới khi chuyển phòng. Không đụng tới <see cref="CheckedInAt"/> nên thời
    /// gian chờ được giữ nguyên.
    /// </summary>
    public void SetQueueOrder(long order) => QueueOrder = order;

    /// <summary>Đặt mốc vào hàng đợi phòng (dùng đánh số). Đặt lại khi chuyển phòng để đánh số mới ở cuối.</summary>
    public void SetQueueEntryOrder(long order) => QueueEntryOrder = order;

    /// <summary>Đặt hoặc xóa lịch hẹn tái khám (chỉ nhắc ngày, không tạo lịch hẹn mới).</summary>
    public void SetFollowUpReminder(DateOnly? date, string? note)
    {
        FollowUpDate = date;
        FollowUpNote = date == null ? null : note;
    }

    /// <summary>Lưu kết quả tóm tắt AI mới tạo cùng thời điểm và số lịch hẹn đã dùng làm căn cứ,
    /// để lần sau xác định được cache còn hợp lệ hay không (xem <see cref="AiSummaryBasedOnCount"/>).</summary>
    public void SetAiSummary(string summary, int basedOnPastAppointmentCount)
    {
        AiSummary = summary;
        AiSummaryGeneratedAt = DateTimeOffset.UtcNow;
        AiSummaryBasedOnCount = basedOnPastAppointmentCount;
    }

    /// <summary>
    /// Staff check-in bệnh nhân đến tái khám (từ tab Tái khám ở quầy): tạo buổi hẹn mới
    /// đã check-in ngay, gắn về buổi gốc qua <see cref="FollowUpFromAppointmentId"/> —
    /// đây là căn cứ để phía bác sĩ đánh dấu buổi này là tái khám.
    /// </summary>
    public static Appointment CheckInFollowUp(
        Guid originalAppointmentId,
        Guid patientId,
        Guid dentistId,
        Guid? serviceId = null,
        string? symptoms = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            ServiceId = serviceId,
            AppointmentDate = now,
            Status = AppointmentStatus.CheckedIn,
            CheckedInAt = now,
            Symptoms = symptoms,
            FollowUpFromAppointmentId = originalAppointmentId,
            CreatedAt = now
        };
    }
    /// <summary>
    /// VỊ TRÍ HIỂN THỊ trong hàng đợi (tick UTC). Tách khỏi <see cref="CheckedInAt"/> để lễ tân đẩy
    /// bệnh nhân lên/xuống hoặc chuyển phòng (xuống cuối) mà KHÔNG làm đổi thời gian chờ. Null =
    /// chưa can thiệp thủ công, xếp theo giờ check-in như mặc định.
    /// </summary>
    public long? QueueOrder { get; private set; }

    /// <summary>
    /// Mốc "vào hàng đợi phòng" (tick UTC) — dùng để ĐÁNH SỐ thứ tự. Khác <see cref="QueueOrder"/> ở
    /// chỗ KHÔNG bị thao tác đẩy lên/xuống làm đổi, nên số của mỗi người giữ nguyên khi lễ tân sắp lại
    /// chỗ. Khi CHUYỂN PHÒNG thì đặt = hiện tại để bệnh nhân nhận số MỚI ở cuối hàng đợi phòng mới.
    /// Null = lùi về giờ check-in.
    /// </summary>
    public long? QueueEntryOrder { get; private set; }
}
