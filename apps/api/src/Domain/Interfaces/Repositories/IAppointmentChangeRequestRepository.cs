using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IAppointmentChangeRequestRepository
{
    Task<AppointmentChangeRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppointmentChangeRequest?> GetPendingByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentChangeRequest>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentChangeRequest>> GetStaffChangeRequestsAsync(
        AppointmentChangeRequestStatus? status = null,
        DateOnly? date = null,
        CancellationToken ct = default);
    Task AddAsync(AppointmentChangeRequest request, CancellationToken ct = default);
    Task UpdateAsync(AppointmentChangeRequest request, CancellationToken ct = default);
}
