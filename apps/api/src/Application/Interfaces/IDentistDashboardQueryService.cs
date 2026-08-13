using DentalClinic.API.Application.UseCases.DentistDashboard;

namespace DentalClinic.API.Application.Interfaces;

/// <summary>Read-model tổng hợp đa entity (Appointment, DentistProfile, WorkSchedule) cho nhóm DentistDashboard.</summary>
public interface IDentistDashboardQueryService
{
    Task<DentistDashboardResponse> GetDashboardAsync(Guid userId, CancellationToken ct);

    Task<List<DentistPatientDto>?> GetPastPatientsAsync(Guid userId, CancellationToken ct);

    Task<DentistPatientsResponse> GetPatientsAsync(Guid dentistId, DateOnly date, CancellationToken ct);

    Task<DentistPatientsResponse?> GetMyPatientsAsync(Guid userId, DateOnly? date, CancellationToken ct);
}
