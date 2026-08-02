using DentalClinic.API.Application.DTOs.StaffDashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.StaffDashboard;

public record GetStaffPendingInvoicesQuery(int Limit) : IRequest<IReadOnlyList<StaffPendingInvoiceDto>>;

/// <summary>Hóa đơn chưa thanh toán, cũ nhất trước — khớp thứ tự với tab "Chờ thanh toán".</summary>
public class GetStaffPendingInvoicesHandler(AppDbContext dbContext)
    : IRequestHandler<GetStaffPendingInvoicesQuery, IReadOnlyList<StaffPendingInvoiceDto>>
{
    public async Task<IReadOnlyList<StaffPendingInvoiceDto>> Handle(GetStaffPendingInvoicesQuery query, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(query.Limit, 1, 50);

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Where(i => i.Status == PaymentStatus.Unpaid)
            .OrderBy(i => i.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync(ct);

        return invoices
            .Select(i => new StaffPendingInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.Appointment.Patient.FullName,
                i.Items.FirstOrDefault()?.Name,
                i.TotalAmount))
            .ToList();
    }
}
