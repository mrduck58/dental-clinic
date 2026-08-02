using DentalClinic.API.Domain.Entities;

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
}
