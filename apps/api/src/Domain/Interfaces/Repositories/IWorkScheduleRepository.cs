using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IWorkScheduleRepository
{
    Task<IEnumerable<WorkSchedule>> GetByWeekAsync(DateOnly weekStart, CancellationToken ct = default);
    Task ReplaceWeekAsync(DateOnly weekStart, IEnumerable<WorkSchedule> entries, CancellationToken ct = default);
}
