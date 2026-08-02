using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Dashboard.DashboardDateHelper;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetServiceDistributionQuery(string? Range, int TopN) : IRequest<ServiceDistributionDto>;

public class GetServiceDistributionHandler(AppDbContext dbContext)
    : IRequestHandler<GetServiceDistributionQuery, ServiceDistributionDto>
{
    public async Task<ServiceDistributionDto> Handle(GetServiceDistributionQuery query, CancellationToken ct)
    {
        var normalizedRange = NormalizeRange(query.Range);
        var clampedTopN = Math.Clamp(query.TopN, 1, 20);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);
        var startOffset = ToVn(currentStart);
        var endOffset = ToVn(currentEnd);

        var grouped = await dbContext.Appointments
            .Where(a => a.AppointmentDate >= startOffset && a.AppointmentDate < endOffset
                        && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = grouped.Sum(g => g.Count);
        if (total == 0)
            return new ServiceDistributionDto(normalizedRange, 0, []);

        var serviceIds = grouped.Where(g => g.ServiceId.HasValue).Select(g => g.ServiceId!.Value).ToList();
        var namesById = await dbContext.Services
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var ranked = grouped.OrderByDescending(g => g.Count).ToList();
        var top = ranked.Take(clampedTopN).ToList();
        var otherCount = ranked.Skip(clampedTopN).Sum(g => g.Count);

        var items = top
            .Select(g => new ServiceDistributionItemDto(
                g.ServiceId,
                g.ServiceId.HasValue ? namesById.GetValueOrDefault(g.ServiceId.Value) : null,
                g.Count,
                Math.Round((double)g.Count / total * 100, 1)))
            .ToList();

        if (otherCount > 0)
            items.Add(new ServiceDistributionItemDto(null, null, otherCount, Math.Round((double)otherCount / total * 100, 1)));

        return new ServiceDistributionDto(normalizedRange, total, items);
    }
}
