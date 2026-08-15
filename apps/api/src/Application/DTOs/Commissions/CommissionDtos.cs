namespace DentalClinic.API.Application.DTOs.Commissions;

public record CommissionRuleDto(
    Guid Id,
    Guid? DentistId,
    string? DentistName,
    string? ServiceName,
    decimal RatePercent,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? Note,
    decimal RevenueBasis,
    decimal CommissionAmount);

public record CommissionRulesResultDto(IReadOnlyList<CommissionRuleDto> Items, decimal TotalCommission);

public record CommissionRuleRequest(
    Guid? DentistId,
    string? ServiceName,
    decimal RatePercent,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Note);
