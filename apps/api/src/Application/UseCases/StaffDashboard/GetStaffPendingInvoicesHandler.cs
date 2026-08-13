using DentalClinic.API.Application.DTOs.StaffDashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.StaffDashboard;

public record GetStaffPendingInvoicesQuery(int Limit) : IRequest<IReadOnlyList<StaffPendingInvoiceDto>>;

/// <summary>Hóa đơn chưa thanh toán, cũ nhất trước — khớp thứ tự với tab "Chờ thanh toán".</summary>
public class GetStaffPendingInvoicesHandler(IStaffDashboardQueryService staffDashboardQueryService)
    : IRequestHandler<GetStaffPendingInvoicesQuery, IReadOnlyList<StaffPendingInvoiceDto>>
{
    public Task<IReadOnlyList<StaffPendingInvoiceDto>> Handle(GetStaffPendingInvoicesQuery query, CancellationToken ct) =>
        staffDashboardQueryService.GetPendingInvoicesAsync(query.Limit, ct);
}
