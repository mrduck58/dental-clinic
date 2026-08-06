namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>
/// Đọc thông tin rút gọn (chỉ-đọc) của 1 lịch hẹn — tên bệnh nhân/nha sĩ/dịch vụ — dùng khi bác sĩ tạo yêu
/// cầu vật tư (MaterialRequest) từ 1 buổi khám.
///
/// Tạo interface riêng thay vì mở rộng IAppointmentRepository vì file đó đang được refactor song song bởi
/// một agent khác trong cùng đợt dọn Clean Architecture (tránh xung đột merge). Nên xem xét gộp lại vào
/// IAppointmentRepository sau khi các nhánh refactor đồng bộ xong.
/// </summary>
public interface IAppointmentSummaryReader
{
    Task<AppointmentSummary?> GetSummaryAsync(Guid appointmentId, CancellationToken ct = default);
}

/// <summary>DTO rút gọn — chỉ những trường MaterialRequest cần, tránh phải Include cả Patient/Dentist/Service.</summary>
public record AppointmentSummary(Guid PatientId, string PatientName, string DentistName, string? ServiceName);
