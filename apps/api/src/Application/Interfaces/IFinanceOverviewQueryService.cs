using DentalClinic.API.Application.DTOs.Finance;

namespace DentalClinic.API.Application.Interfaces;

public interface IFinanceOverviewQueryService
{
    Task<FinanceOverviewDto> GetOverviewAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
