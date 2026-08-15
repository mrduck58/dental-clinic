using DentalClinic.API.Application.DTOs.Commissions;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Tính "tiền hoa hồng" cho từng quy tắc trong một kỳ đã chọn — doanh thu căn cứ chỉ tính được chính xác
/// cho nha sĩ (qua Appointment.DentistId), vì đó là chiều duy nhất doanh thu gắn trực tiếp theo người
/// trong dữ liệu hiện có.
/// </summary>
public class CommissionQueryService(AppDbContext db, ICommissionRuleRepository commissionRuleRepository)
    : ICommissionQueryService
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<CommissionRulesResultDto> GetRulesWithCommissionAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rules = await commissionRuleRepository.GetAllAsync(ct);

        var dentistIds = rules.Where(r => r.DentistId.HasValue).Select(r => r.DentistId!.Value).Distinct().ToList();
        var dentistNames = await db.DentistProfiles
            .AsNoTracking()
            .Where(d => dentistIds.Contains(d.Id))
            .Select(d => new { d.Id, Name = d.Employee.User.FullName })
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var items = new List<CommissionRuleDto>();
        foreach (var rule in rules)
        {
            var effStart = rule.EffectiveFrom > from ? rule.EffectiveFrom : from;
            var effEnd = rule.EffectiveTo is DateOnly ruleEnd && ruleEnd < to ? ruleEnd : to;

            var basis = effStart <= effEnd
                ? await ComputeRevenueBasisAsync(rule.DentistId, rule.ServiceName, effStart, effEnd, ct)
                : 0m;

            var dentistName = rule.DentistId.HasValue
                ? dentistNames.GetValueOrDefault(rule.DentistId.Value, "Bác sĩ")
                : null;

            items.Add(new CommissionRuleDto(
                rule.Id, rule.DentistId, dentistName, rule.ServiceName,
                rule.RatePercent, rule.EffectiveFrom, rule.EffectiveTo, rule.IsActive, rule.Note,
                basis, Math.Round(basis * rule.RatePercent / 100m, 0)));
        }

        return new CommissionRulesResultDto(items, items.Sum(i => i.CommissionAmount));
    }

    private async Task<decimal> ComputeRevenueBasisAsync(
        Guid? dentistId, string? serviceName, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (start, end) = PeriodBounds(from, to);

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            var query = db.InvoiceItems
                .AsNoTracking()
                .Where(it => it.Name == serviceName
                    && it.Invoice.Status == PaymentStatus.Paid
                    && it.Invoice.PaymentDate >= start && it.Invoice.PaymentDate < end);
            if (dentistId is Guid d)
                query = query.Where(it => it.Invoice.Appointment.DentistId == d);
            return await query.SumAsync(it => it.AmountCollected, ct);
        }

        var invoiceQuery = db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= start && i.PaymentDate < end);
        if (dentistId is Guid dentist)
            invoiceQuery = invoiceQuery.Where(i => i.Appointment.DentistId == dentist);
        return await invoiceQuery.SumAsync(i => i.DepositAmount, ct);
    }

    private (DateTimeOffset Start, DateTimeOffset End) PeriodBounds(DateOnly from, DateOnly to)
    {
        var (fromDate, toDate) = from > to ? (to, from) : (from, to);
        return (ToVn(fromDate), ToVn(toDate.AddDays(1)));
    }

    private DateTimeOffset ToVn(DateOnly date)
        => new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset).ToUniversalTime();
}
