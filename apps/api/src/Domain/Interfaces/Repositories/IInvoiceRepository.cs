using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>
/// Invoice và các truy vấn Appointment/TreatmentPlan chỉ tồn tại để phục vụ nghiệp vụ xuất/thu hóa đơn
/// (ví dụ: liệt kê buổi hẹn CHƯA xuất hóa đơn, chuỗi tái khám để gộp dịch vụ). Những method Appointment/
/// TreatmentPlan này nằm ở đây — KHÔNG phải ở IAppointmentRepository/ITreatmentPlanRepository — vì chúng
/// không phải nhu cầu chung của Appointment/TreatmentPlan mà là chi tiết triển khai riêng của module Invoice.
/// Việc xác nhận thanh toán chạm vào nhiều entity (Invoice+TreatmentPlan+Appointment) trong 1 giao dịch nên
/// các method đọc-để-ghi ở đây trả entity CÓ tracking; caller tự gọi <see cref="IUnitOfWork"/> để lưu.
/// </summary>
public interface IInvoiceRepository
{
    /// <summary>Đọc để ghi (có tracking), không kèm navigation.</summary>
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Đọc để ghi (có tracking) kèm Appointment — cần khi xác nhận thanh toán gọi <c>invoice.Appointment.Complete()</c>.</summary>
    Task<Invoice?> GetByIdWithAppointmentAsync(Guid id, CancellationToken ct = default);

    /// <summary>Đọc để hiển thị (AsNoTracking) kèm Appointment→Patient — dùng khi tạo yêu cầu thanh toán.</summary>
    Task<Invoice?> GetByIdWithAppointmentAndPatientAsync(Guid id, CancellationToken ct = default);

    /// <summary>Đọc để hiển thị (AsNoTracking) đầy đủ Items + Appointment/Patient/Dentist — dùng map DTO trả về client.</summary>
    Task<Invoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    Task<bool> HasChildInvoiceAsync(Guid parentInvoiceId, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Hóa đơn đặt cọc đang thu phần còn lại, chưa có hóa đơn con — tab "Liệu trình → Hóa đơn".</summary>
    Task<IReadOnlyList<Invoice>> GetCollectingRemainingParentsAsync(CancellationToken ct = default);

    /// <summary>Tab "Công nợ" (hóa đơn): đặt cọc còn dư nợ, chưa tất toán, không tính hóa đơn con thu phần còn lại.</summary>
    Task<IReadOnlyList<Invoice>> GetOutstandingInvoicesAsync(CancellationToken ct = default);

    /// <summary>Tab "Chờ thanh toán": mọi hóa đơn Unpaid.</summary>
    Task<IReadOnlyList<Invoice>> GetPendingInvoicesAsync(CancellationToken ct = default);

    /// <summary>Hóa đơn Unpaid của một bệnh nhân cụ thể (mobile app).</summary>
    Task<IReadOnlyList<Invoice>> GetPendingInvoicesByPatientAsync(Guid patientId, CancellationToken ct = default);

    /// <summary>Hóa đơn đã Paid của một bệnh nhân cụ thể (mobile app — lịch sử giao dịch).</summary>
    Task<IReadOnlyList<Invoice>> GetPaidInvoicesByPatientAsync(Guid patientId, CancellationToken ct = default);

    /// <summary>Tab "Lịch sử hóa đơn": mọi hóa đơn đã Paid.</summary>
    Task<IReadOnlyList<Invoice>> GetInvoiceHistoryAsync(CancellationToken ct = default);

    /// <summary>Thêm hóa đơn mới — chỉ stage, KHÔNG tự SaveChanges (caller dùng <see cref="IUnitOfWork"/>).</summary>
    void Add(Invoice invoice);

    /// <summary>UserId của bệnh nhân gắn với 1 buổi hẹn (qua Invoice.AppointmentId) — dùng gửi thông báo.</summary>
    Task<Guid?> GetPatientUserIdByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);

    // --- Đọc Appointment/TreatmentPlan phục vụ riêng nghiệp vụ hóa đơn ---

    /// <summary>Đọc để ghi (tracking) kèm Invoices — dùng khi xuất hóa đơn mới cho 1 buổi hẹn.</summary>
    Task<Appointment?> GetAppointmentWithInvoicesAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>Buổi hẹn đã kết thúc điều trị (PendingPayment), kèm Patient/Dentist/Diagnoses — tab "Liệu trình → Hóa đơn".</summary>
    Task<IReadOnlyList<Appointment>> GetPendingPaymentAppointmentsWithDetailsAsync(CancellationToken ct = default);

    /// <summary>Map Id→FollowUpFromAppointmentId của mọi buổi hẹn thuộc các bệnh nhân này — dựng chuỗi tái khám.</summary>
    Task<Dictionary<Guid, Guid?>> GetFollowUpParentMapAsync(IReadOnlyList<Guid> patientIds, CancellationToken ct = default);

    /// <summary>Chuỗi tái khám của một buổi hẹn: chính nó + các buổi gốc phía trên (đi ngược FollowUpFromAppointmentId).</summary>
    Task<HashSet<Guid>> GetFollowUpChainAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>Đọc để ghi (tracking) kèm Service — dùng khi thu 1 đợt của liệu trình.</summary>
    Task<TreatmentPlan?> GetTreatmentPlanWithServiceAsync(Guid treatmentPlanId, CancellationToken ct = default);

    /// <summary>Liệu trình chưa hủy của các bệnh nhân này, kèm Service — tab "Liệu trình → Hóa đơn".</summary>
    Task<IReadOnlyList<TreatmentPlan>> GetActiveTreatmentPlansByPatientIdsAsync(IReadOnlyList<Guid> patientIds, CancellationToken ct = default);

    /// <summary>Liệu trình đang điều trị (InProgress), kèm Patient/Dentist/Service — tab "Công nợ" (liệu trình).</summary>
    Task<IReadOnlyList<TreatmentPlan>> GetInProgressTreatmentPlansWithDetailsAsync(CancellationToken ct = default);

    /// <summary>Giá dòng (UnitPrice/Quantity) của các liệu trình theo Id — chặn xuất hóa đơn vượt tổng tiền dịch vụ.</summary>
    Task<IReadOnlyList<TreatmentPlanBillingInfo>> GetTreatmentPlanBillingInfoAsync(IReadOnlyList<Guid> treatmentPlanIds, CancellationToken ct = default);

    /// <summary>Giá dòng của các liệu trình (chưa hủy) thuộc các buổi hẹn này — kiểm tra "còn dịch vụ nào chưa xuất hóa đơn".</summary>
    Task<IReadOnlyList<TreatmentPlanBillingInfo>> GetTreatmentPlanBillingInfoByAppointmentIdsAsync(IReadOnlyList<Guid> appointmentIds, CancellationToken ct = default);
}

/// <summary>ServiceId dùng để khớp đúng khuyến mãi theo dịch vụ (không phải so tên chuỗi) — khớp theo
/// ServiceId của liệu trình nên tự động đúng cho MỌI option đã chọn của dịch vụ đó (UnitPrice là giá
/// option thực tế, không phải giá gốc dịch vụ).</summary>
public record TreatmentPlanBillingInfo(Guid Id, decimal UnitPrice, int Quantity, Guid ServiceId);
