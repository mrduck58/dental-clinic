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
    public AppointmentType AppointmentType { get; private set; } = AppointmentType.GeneralExam;
    public int DurationMinutes { get; private set; } = 30;

    /// <summary>Lịch này do bệnh nhân tự đặt từ xa hay do lễ tân lập tại quầy — xem <see cref="UndoCheckIn"/>.</summary>
    public AppointmentOrigin Origin { get; private set; }
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
    // Ảnh chụp chiếu lúc khám + ảnh đính kèm yêu cầu vật tư (dấu răng, răng lợi...) — tách theo
    // AppointmentPhoto.Section, xem ghi chú trên entity đó.
    public ICollection<AppointmentPhoto> Photos { get; private set; } = new List<AppointmentPhoto>();

    // Sessions executed or planned in this appointment
    public ICollection<AppointmentSession> AppointmentSessions { get; private set; } = new List<AppointmentSession>();

    // Follow-up appointments (dữ liệu lịch sử — luồng tạo lịch tái khám cũ)
    public Guid? FollowUpFromAppointmentId { get; private set; }
    public Appointment? FollowUpFromAppointment { get; private set; }
    public ICollection<Appointment> FollowUpAppointments { get; private set; } = new List<Appointment>();

    // Liên kết với thực thể FollowUp độc lập
    public Guid? FollowUpId { get; private set; }
    public FollowUp? FollowUpOrder { get; private set; }
    public ICollection<FollowUp> OriginatedFollowUps { get; private set; } = new List<FollowUp>();

    // Nhắc tái khám (giữ tương thích)
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
        string? notes = null,
        AppointmentType appointmentType = AppointmentType.GeneralExam,
        int durationMinutes = 30,
        Guid? followUpId = null)
    {
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            ServiceId = serviceId,
            AppointmentDate = appointmentDate,
            Status = AppointmentStatus.Pending,
            Origin = AppointmentOrigin.Online,
            AppointmentType = appointmentType,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : 30,
            FollowUpId = followUpId,
            Symptoms = symptoms,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Lịch lập tại quầy khi bệnh nhân đã có mặt: vào thẳng CheckedIn.
    /// </summary>
    public static Appointment CreateWalkIn(
        Guid patientId,
        Guid dentistId,
        DateTimeOffset appointmentDate,
        string? symptoms = null,
        Guid? serviceId = null,
        Guid? followUpFromAppointmentId = null,
        AppointmentType appointmentType = AppointmentType.GeneralExam,
        int durationMinutes = 30,
        Guid? followUpId = null)
    {
        var appointment = Create(patientId, dentistId, appointmentDate, symptoms, serviceId, appointmentType: appointmentType, durationMinutes: durationMinutes, followUpId: followUpId);
        appointment.Origin = AppointmentOrigin.WalkIn;
        appointment.FollowUpFromAppointmentId = followUpFromAppointmentId;
        appointment.CheckIn();
        return appointment;
    }

    public void Confirm() => Status = AppointmentStatus.Confirmed;
    public void CheckIn()
    {
        Status = AppointmentStatus.CheckedIn;
        CheckedInAt = DateTimeOffset.UtcNow;
    }
    /// <summary>Ghi nhận bệnh nhân vắng mặt — đã xác nhận nhưng không đến khám.</summary>
    public void MarkNoShow() => Status = AppointmentStatus.NoShow;

    /// <summary>
    /// Gỡ một lần ghi nhận vắng mặt bấm nhầm.
    /// </summary>
    public void UndoNoShow()
    {
        if (Status != AppointmentStatus.NoShow)
            throw new ConflictException(
                $"Chỉ hoàn tác được lịch hẹn đang ở trạng thái vắng mặt. Trạng thái hiện tại: '{Status}'.");

        Status = AppointmentStatus.Confirmed;
    }

    public const string UndoCheckInCancellationNote = "Hủy do nhân viên bấm nhầm check-in.";

    public void UndoCheckIn(Guid? undoneByUserId, DateTimeOffset now)
    {
        if (Status != AppointmentStatus.CheckedIn)
            throw new ConflictException(
                $"Chỉ hoàn tác được lịch hẹn đang ở trạng thái đã check-in. Trạng thái hiện tại: '{Status}'.");

        CheckedInAt = null;
        QueueOrder = null;
        QueueEntryOrder = null;
        Status = AppointmentStatus.Confirmed;
    }
    public void StartTreatment() => Status = AppointmentStatus.InProgress;
    public void EndTreatment() => Status = AppointmentStatus.PendingPayment;
    public void Complete() => Status = AppointmentStatus.Completed;

    private static readonly AppointmentStatus[] ChangeableStatuses =
        [AppointmentStatus.Pending, AppointmentStatus.Confirmed, AppointmentStatus.NoShow, AppointmentStatus.Rebooking];

    public bool CanBeChanged => ChangeableStatuses.Contains(Status);

    public void Cancel(Enums.CancellationReason reason, string? note, Guid? cancelledByUserId, DateTimeOffset now)
    {
        if (!CanBeChanged)
            throw new ConflictException(
                $"Không thể hủy lịch hẹn đang ở trạng thái '{Status}'.");

        if (reason == Enums.CancellationReason.Other && string.IsNullOrWhiteSpace(note))
            throw new ValidationException("Vui lòng nhập lý do hủy cụ thể.");

        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        CancellationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        CancelledAt = now;
        CancelledByUserId = cancelledByUserId;
    }

    public void Reschedule(
        DateTimeOffset newDate,
        Guid newDentistId,
        Guid? newServiceId,
        bool requiresReconfirmation,
        bool isRebooking,
        DateTimeOffset now)
    {
        if (!CanBeChanged)
            throw new ConflictException(
                $"Không thể dời lịch hẹn đang ở trạng thái '{Status}'.");

        AppointmentDate = newDate;
        DentistId = newDentistId;
        ServiceId = newServiceId;
        RescheduledCount++;
        LastRescheduledAt = now;

        if (isRebooking)
            Status = AppointmentStatus.Rebooking;
        else if (requiresReconfirmation)
            Status = AppointmentStatus.Pending;
    }

    public void SetDuration(int durationMinutes)
    {
        if (durationMinutes > 0) DurationMinutes = durationMinutes;
    }

    public void SetAppointmentType(AppointmentType type) => AppointmentType = type;
    public void SetFollowUpId(Guid? followUpId) => FollowUpId = followUpId;

    public void ReassignDentist(Guid dentistId) => DentistId = dentistId;

    public void SetQueueOrder(long order) => QueueOrder = order;
    public void SetQueueEntryOrder(long order) => QueueEntryOrder = order;

    public void SetFollowUpReminder(DateOnly? date, string? note)
    {
        FollowUpDate = date;
        FollowUpNote = date == null ? null : note;
    }

    public void SetAiSummary(string summary, int basedOnPastAppointmentCount)
    {
        AiSummary = summary;
        AiSummaryGeneratedAt = DateTimeOffset.UtcNow;
        AiSummaryBasedOnCount = basedOnPastAppointmentCount;
    }

    public long? QueueOrder { get; private set; }
    public long? QueueEntryOrder { get; private set; }
}
