using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;
using static DentalClinic.API.Application.UseCases.Invoices.InvoiceHelpers;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetOutstandingPlansQuery : IRequest<List<OutstandingPlanDto>>;

/// <summary>Tab "Công nợ" — phần liệu trình điều trị còn nợ.</summary>
public class GetOutstandingPlansHandler(IInvoiceRepository invoiceRepository, InvoiceQueryHelper invoiceQuery)
    : IRequestHandler<GetOutstandingPlansQuery, List<OutstandingPlanDto>>
{
    public async Task<List<OutstandingPlanDto>> Handle(GetOutstandingPlansQuery query, CancellationToken ct)
    {
        var plans = await invoiceRepository.GetInProgressTreatmentPlansWithDetailsAsync(ct);

        var planIds = plans.Select(p => p.Id).ToList();
        var paidMap = await invoiceQuery.GetPlanPaidMapAsync(planIds, ct);
        // Phần đã gắn vào hóa đơn — để không tính lại khoản nợ đang nằm ở tab "hóa đơn còn nợ".
        var billedMap = await invoiceQuery.GetPlanBilledMapAsync(planIds, ct);

        return plans
            .Select(p =>
            {
                var paid = paidMap.GetValueOrDefault(p.Id, 0m);
                var billed = billedMap.GetValueOrDefault(p.Id, 0m);
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
                    UnbilledAmount = Math.Max(0, p.TotalCost - billed),
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt
                };
            })
            // Chỉ tính là công nợ khi ĐÃ thu một phần mà còn thiếu (trả góp dở dang).
            // Liệu trình chưa thu đồng nào chỉ là "đang điều trị", chưa phải nợ — sẽ xuất
            // hóa đơn ở tab "Liệu trình → Hóa đơn" của buổi tương ứng.
            //
            // Danh sách này nhìn công nợ theo LIỆU TRÌNH, còn danh sách hóa đơn còn nợ nhìn theo
            // HÓA ĐƠN — cùng một khoản tiền có thể xuất hiện ở cả hai. Vì vậy giao diện tách hai
            // danh sách thành hai tab có tổng riêng, KHÔNG được cộng tổng của hai bên với nhau
            // (UnbilledAmount là phần chắc chắn chưa nằm trên hóa đơn nào).
            .Where(dto => dto.AmountPaid > 0 && dto.RemainingAmount > 0)
            .ToList();
    }
}
