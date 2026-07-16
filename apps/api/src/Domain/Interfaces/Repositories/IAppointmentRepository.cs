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
}
