using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>
/// Interface định nghĩa các thao tác dữ liệu của Appointment.
/// Implementation nằm ở tầng Infrastructure/Persistence/Repositories/.
/// </summary>
public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByDentistIdAsync(Guid dentistId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task<Guid?> GetDentistUserIdAsync(Guid dentistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lịch hẹn khác mà bác sĩ đang khám dở TRONG CÙNG NGÀY (trả null nếu không có).
    /// Giới hạn theo ngày để một ca cũ quên bấm kết thúc không khóa bác sĩ ở những ngày sau.
    /// </summary>
    Task<Appointment?> GetInProgressByDentistAsync(
        Guid dentistId, Guid excludeAppointmentId,
        DateTimeOffset utcStart, DateTimeOffset utcEnd,
        CancellationToken cancellationToken = default);

    /// <summary>Bệnh nhân có đang/đã trong một buổi khám (InProgress/PendingPayment/Completed) hay không —
    /// dùng để chặn ghi nhận chuẩn đoán/đơn thuốc/nhật ký điều trị ngoài lúc đang khám.</summary>
    Task<bool> HasActiveVisitAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Bệnh nhân đã hoàn tất (Completed/PendingPayment) ít nhất 1 buổi khám với nha sĩ này chưa —
    /// điều kiện để được phép đánh giá nha sĩ.</summary>
    Task<bool> HasCompletedVisitAsync(Guid dentistId, Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Số bệnh nhân khác nhau đã hoàn tất buổi khám với một nha sĩ — hiển thị trên trang chi tiết nha sĩ.</summary>
    Task<int> CountDistinctPatientsWithCompletedVisitAsync(Guid dentistId, CancellationToken cancellationToken = default);

    /// <summary>Số buổi khám đã hoàn tất của một nha sĩ (khác với số bệnh nhân — một bệnh nhân có thể khám nhiều lần).</summary>
    Task<int> CountCompletedVisitsAsync(Guid dentistId, CancellationToken cancellationToken = default);

    /// <summary>Số buổi khám đã hoàn tất của một bệnh nhân với một nha sĩ cụ thể.</summary>
    Task<int> CountCompletedVisitsForPatientAsync(Guid dentistId, Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Tổng số buổi khám đã hoàn tất của một bệnh nhân trên toàn hệ thống phòng khám.</summary>
    Task<int> CountOverallCompletedVisitsAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Tên các dịch vụ nha sĩ đã thực hiện, xếp theo số ca giảm dần — hiển thị trên trang chi tiết nha sĩ.</summary>
    Task<IReadOnlyList<string>> GetTopServiceNamesByDentistAsync(Guid dentistId, int take, CancellationToken cancellationToken = default);

    /// <summary>Slot (bác sĩ + giờ hẹn) đã có người đặt (khác Cancelled) hay chưa — kiểm tra trước khi tạo lịch vãng lai.</summary>
    Task<bool> IsSlotBookedAsync(Guid dentistId, DateTimeOffset appointmentDate, CancellationToken cancellationToken = default);

    /// <summary>Danh sách lịch hẹn cho màn hình lễ tân, kèm đầy đủ thông tin hiển thị, lọc theo ngày/trạng thái (tùy chọn).</summary>
    Task<IReadOnlyList<Appointment>> GetStaffAppointmentsAsync(DateOnly? date, AppointmentStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Lịch hẹn của chính bệnh nhân và người thân (PrimaryPatientId), kèm đầy đủ thông tin hiển thị.</summary>
    Task<IReadOnlyList<Appointment>> GetMyAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Lịch hẹn chưa hủy trong một khoảng UTC — dùng cho lịch làm việc lễ tân xem theo ngày.</summary>
    Task<IReadOnlyList<Appointment>> GetActiveInRangeAsync(DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default);

    /// <summary>Toàn bộ lịch hẹn chưa hủy của một bác sĩ (mọi thời điểm) — dùng để tính ngày còn slot trống trong tháng.</summary>
    Task<IReadOnlyList<Appointment>> GetActiveByDentistIdAsync(Guid dentistId, CancellationToken cancellationToken = default);

    /// <summary>Chi tiết đầy đủ một buổi khám (chẩn đoán/liệu trình/đơn thuốc) để hiển thị phiếu khám.</summary>
    Task<Appointment?> GetExaminationDetailAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Lịch sử khám đã hoàn tất của một bệnh nhân cụ thể (không gồm người thân), mới nhất trước.</summary>
    Task<IReadOnlyList<Appointment>> GetCompletedHistoryByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default);

    /// <summary>Lịch sử khám đã hoàn tất của chính chủ và người thân (PrimaryPatientId), có thể lọc thêm theo một bệnh nhân cụ thể.</summary>
    Task<IReadOnlyList<Appointment>> GetCompletedHistoryForFamilyAsync(Guid primaryPatientId, Guid? filterPatientId, int take, CancellationToken cancellationToken = default);

    /// <summary>Chuỗi tái khám (2 chiều: buổi gốc + các buổi tái khám sau) của một buổi hẹn, không gồm chính nó.</summary>
    Task<List<Guid>> GetFollowUpChainAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Lịch hẹn đang/đã diễn ra trong ngày (CheckedIn/InProgress/Completed) — dữ liệu nguồn cho hàng đợi lễ tân.</summary>
    Task<IReadOnlyList<Appointment>> GetQueueAppointmentsByDateRangeAsync(DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default);

    /// <summary>Lịch hẹn đang hoạt động (CheckedIn/InProgress) hôm nay của một bệnh nhân — để tra vị trí hàng đợi của họ.</summary>
    Task<Appointment?> GetActiveTodayByPatientAsync(Guid patientId, DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default);

    /// <summary>Mọi lịch hẹn (mọi trạng thái) của một bệnh nhân trong một khoảng ngày — chỉ dùng để chẩn đoán/log.</summary>
    Task<IReadOnlyList<Appointment>> GetByPatientAndDateRangeAsync(Guid patientId, DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default);

    /// <summary>Các buổi hẹn đã kết thúc điều trị và được bác sĩ hẹn ngày tái khám — nguồn cho danh sách "chờ tái khám".</summary>
    Task<IReadOnlyList<Appointment>> GetFollowUpScheduledAsync(CancellationToken cancellationToken = default);

    /// <summary>Id các buổi hẹn gốc đã được check-in tái khám (buổi con chưa hủy) — để ẩn khỏi danh sách "chờ tái khám".</summary>
    Task<HashSet<Guid>> GetCheckedInFollowUpOriginalIdsAsync(List<Guid> originalAppointmentIds, CancellationToken cancellationToken = default);

    /// <summary>Bản đồ Id buổi hẹn → Id buổi hẹn gốc (FollowUpFromAppointmentId) của các bệnh nhân — dùng để dựng chuỗi tái khám.</summary>
    Task<Dictionary<Guid, Guid?>> GetFollowUpParentMapAsync(List<Guid> patientIds, CancellationToken cancellationToken = default);

    /// <summary>Buổi hẹn gốc này đã có buổi tái khám được check-in (chưa hủy) hay chưa — chặn check-in tái khám lặp.</summary>
    Task<bool> HasActiveFollowUpCheckInAsync(Guid originalAppointmentId, CancellationToken cancellationToken = default);

    /// <summary>Buổi khám kèm chẩn đoán/liệu trình/đơn thuốc — dữ liệu nguồn cho AI gợi ý đơn thuốc.</summary>
    Task<Appointment?> GetForPrescriptionSuggestionAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Buổi khám kèm chẩn đoán — dữ liệu nguồn cho AI gợi ý hướng điều trị.</summary>
    Task<Appointment?> GetForTreatmentSuggestionAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Lịch sử khám (mọi trạng thái) của một bệnh nhân, không gồm một buổi hẹn cụ thể — dùng cho AI tóm tắt/gợi ý điều trị.</summary>
    Task<IReadOnlyList<Appointment>> GetPatientHistoryExcludingAsync(Guid patientId, Guid excludeAppointmentId, CancellationToken cancellationToken = default);

    /// <summary>Lịch hẹn sắp tới (Pending/Confirmed, từ một thời điểm) của một nhóm bệnh nhân (chính chủ + người thân) — cho chatbot tham chiếu khi hủy/dời lịch.</summary>
    Task<IReadOnlyList<Appointment>> GetUpcomingForPatientsAsync(List<Guid> patientIds, DateTimeOffset fromUtc, int take, CancellationToken cancellationToken = default);

    /// <summary>Lịch hẹn sắp tới GẦN NHẤT (Pending/Confirmed, trong một cửa sổ thời gian) của một nhóm bệnh nhân — dùng để nhắc khi bắt đầu hội thoại chatbot.</summary>
    Task<Appointment?> GetNextUpcomingForPatientsAsync(List<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);

    /// <summary>Lịch hẹn chưa hủy của một bệnh nhân (theo PatientId) HOẶC theo tài khoản (Patient.UserId) — dùng để tự sinh thông báo nhắc lịch hẹn.</summary>
    Task<IReadOnlyList<Appointment>> GetActiveByPatientOrUserAsync(Guid? patientId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Tài khoản/Bệnh nhân đã có lịch hẹn đang hoạt động (chưa hủy) trong ngày cụ thể hay chưa — dùng để chặn đặt nhiều lịch/ngày.</summary>
    Task<bool> HasActiveAppointmentOnDateAsync(Guid accountOrPatientId, DateOnly date, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh sách lịch hẹn có phân trang cho màn hình "Ca khám &amp; điều trị" của Owner — lọc theo khoảng ngày,
    /// một hoặc nhiều trạng thái (phân tách bởi dấu phẩy), và tìm theo tên/SĐT bệnh nhân hoặc tên nha sĩ.
    /// </summary>
    Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetAppointmentsPagedAsync(
        DateOnly? startDate, DateOnly? endDate, string? statusCsv, string? search,
        int page, int pageSize, string? sortDir = null, CancellationToken cancellationToken = default);

    /// <summary>Toàn bộ lịch hẹn (mọi trạng thái) của một bệnh nhân, kèm Dentist/Service/Invoices —
    /// dữ liệu nguồn cho trang chi tiết bệnh nhân (lịch sử khám + trạng thái thanh toán) của Owner.</summary>
    Task<IReadOnlyList<Appointment>> GetByPatientIdWithDetailsAsync(Guid patientId, CancellationToken cancellationToken = default);
}
