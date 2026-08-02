using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Invoices.InvoiceHelpers;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetOutstandingPlansQuery : IRequest<List<OutstandingPlanDto>>;

/// <summary>Tab "Công nợ" — phần liệu trình điều trị còn nợ.</summary>
public class GetOutstandingPlansHandler(AppDbContext dbContext, InvoiceQueryHelper invoiceQuery)
    : IRequestHandler<GetOutstandingPlansQuery, List<OutstandingPlanDto>>
{
    public async Task<List<OutstandingPlanDto>> Handle(GetOutstandingPlansQuery query, CancellationToken ct)
    {
        var plans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Patient).ThenInclude(p => p.User)
            .Include(tp => tp.Dentist).ThenInclude(d => d.User)
            .Include(tp => tp.Service)
            .Where(tp => tp.Status == TreatmentPlanStatus.InProgress)
            .OrderByDescending(tp => tp.CreatedAt)
            .ToListAsync(ct);

        var paidMap = await invoiceQuery.GetPlanPaidMapAsync(plans.Select(p => p.Id).ToList(), ct);

        return plans
            .Select(p =>
            {
                var paid = paidMap.GetValueOrDefault(p.Id, 0m);
                return new OutstandingPlanDto
                {
                    TreatmentPlanId = p.Id,
                    PlanName = BuildPlanName(p),
                    PatientName = p.Patient.FullName,
                    PatientPhone = p.Patient.User?.PhoneNumber,
                    Gender = p.Patient.Gender,
                    DentistName = p.Dentist.FullName,
                    TotalCost = p.TotalCost,
                    AmountPaid = paid,
                    RemainingAmount = Math.Max(0, p.TotalCost - paid),
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt
                };
            })
            // Chỉ tính là công nợ khi ĐÃ thu một phần mà còn thiếu (trả góp dở dang).
            // Liệu trình chưa thu đồng nào chỉ là "đang điều trị", chưa phải nợ — sẽ xuất
            // hóa đơn ở tab "Liệu trình → Hóa đơn" của buổi tương ứng.
            .Where(dto => dto.AmountPaid > 0 && dto.RemainingAmount > 0)
            .ToList();
    }
}
