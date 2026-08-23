using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IAppointmentPhotoRepository
{
    Task<AppointmentPhoto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>section null → lấy tất cả ảnh của buổi hẹn (cả 2 khu vực).</summary>
    Task<List<AppointmentPhoto>> GetByAppointmentIdAsync(Guid appointmentId, string? section = null, CancellationToken ct = default);

    Task AddAsync(AppointmentPhoto photo, CancellationToken ct = default);
    Task UpdateAsync(AppointmentPhoto photo, CancellationToken ct = default);
    Task DeleteAsync(AppointmentPhoto photo, CancellationToken ct = default);
}
