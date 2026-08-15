using DentalClinic.API.Application.DTOs.Commissions;

namespace DentalClinic.API.Application.Interfaces;

public interface ICommissionQueryService
{
    Task<CommissionRulesResultDto> GetRulesWithCommissionAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
