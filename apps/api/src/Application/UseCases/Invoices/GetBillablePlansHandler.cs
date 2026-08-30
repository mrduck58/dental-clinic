using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;
using static DentalClinic.API.Application.UseCases.Invoices.InvoiceHelpers;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetBillablePlansQuery : IRequest<List<BillablePlanDto>>;

/// <summary>
/// Tab "Liệu trình → Hóa đơn": lịch hẹn đã kết thúc điều trị, chưa xuất hóa đơn.
/// Các dòng dịch vụ được gợi ý từ liệu trình điều trị (Description + EstimatedCost).
/// </summary>
public class GetBillablePlansHandler(IInvoiceRepository invoiceRepository, InvoiceQueryHelper invoiceQuery)
    : IRequestHandler<GetBillablePlansQuery, List<BillablePlanDto>>
{
    public async Task<List<BillablePlanDto>> Handle(GetBillablePlansQuery query, CancellationToken ct)
    {
        // 1) Buổi đã kết thúc điều trị — cho phép xuất NHIỀU hóa đơn/buổi (mỗi dịch vụ một hóa đơn,
        // kiểu thanh toán riêng), nên KHÔNG lọc "buổi chưa có hóa đơn".
        var appointments = (await invoiceRepository.GetPendingPaymentAppointmentsWithDetailsAsync(ct)).ToList();

        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();

        // Tất cả liệu trình (chưa hủy) của các bệnh nhân liên quan — KHÔNG lọc theo trạng thái lâm sàng:
        // một dịch vụ chưa thu đủ tiền vẫn phải xuất hóa đơn dù bác sĩ đã đánh dấu "Hoàn thành".
        var allPlans = (await invoiceRepository.GetActiveTreatmentPlansByPatientIdsAsync(patientIds, ct)).ToList();
        var planIds = allPlans.Select(p => p.Id).ToList();
        var billedByPlan = await invoiceQuery.GetPlanBilledMapAsync(planIds, ct); // đã gắn vào hóa đơn (tránh xuất trùng)

        // Bản đồ cha-con để gom liệu trình theo ĐÚNG chuỗi tái khám của mỗi buổi.
        var parentById = await invoiceRepository.GetFollowUpParentMapAsync(patientIds, ct);

        HashSet<Guid> ChainOf(Guid id)
        {
            var chain = new HashSet<Guid>();
            Guid? cursor = id;
            while (cursor is Guid c && chain.Add(c))
                cursor = parentById.TryGetValue(c, out var next) ? next : null;
            return chain;
        }

        // Gán mỗi dịch vụ chưa xuất hóa đơn hết cho buổi PendingPayment MỚI NHẤT của bệnh nhân
        var byHost = new Dictionary<Guid, List<TreatmentPlan>>();
        foreach (var plan in allPlans.OrderBy(p => p.CreatedAt))
        {
            if (plan.TotalCost - billedByPlan.GetValueOrDefault(plan.Id, 0m) <= 0) continue;

            Appointment? host = appointments
                .Where(a => a.PatientId == plan.PatientId)
                .OrderByDescending(a => a.AppointmentDate)
                .FirstOrDefault();
            if (host == null) continue;

            if (!byHost.TryGetValue(host.Id, out var list)) byHost[host.Id] = list = new List<TreatmentPlan>();
            list.Add(plan);
        }

        var result = new List<BillablePlanDto>();
        foreach (var a in appointments)
        {
            if (!byHost.TryGetValue(a.Id, out var plans)) continue;
            var items = plans.Select(p =>
            {
                var billed = billedByPlan.GetValueOrDefault(p.Id, 0m);
                var toBill = p.TotalCost - billed;
                return billed <= 0
                    ? new InvoiceItemDto(BuildPlanName(p), p.Quantity, p.UnitPrice, p.Id, p.ServiceId)
                    : new InvoiceItemDto($"{BuildPlanName(p)} (còn lại)", 1, toBill, p.Id, p.ServiceId);
            }).ToList();

            result.Add(new BillablePlanDto
            {
                AppointmentId = a.Id,
                AppointmentCode = BuildAppointmentCode(a),
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.User?.PhoneNumber,
                Gender = a.Patient.Gender,
                DentistName = a.Dentist.FullName,
                AppointmentDate = a.AppointmentDate,
                Diagnosis = string.Join("; ", a.Diagnoses.Select(d => d.Description)),
                Items = items,
                SuggestedTotal = items.Sum(i => i.LineTotal)
            });
        }

        // 2) Hóa đơn đặt cọc đang được yêu cầu thu phần còn lại (chưa tạo hóa đơn con).
        var remainingParents = await invoiceRepository.GetCollectingRemainingParentsAsync(ct);

        foreach (var parent in remainingParents)
        {
            var a = parent.Appointment;
            var remaining = parent.TotalAmount - parent.DepositAmount;
            result.Add(new BillablePlanDto
            {
                AppointmentId = a.Id,
                AppointmentCode = BuildAppointmentCode(a),
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.User?.PhoneNumber,
                Gender = a.Patient.Gender,
                DentistName = a.Dentist.FullName,
                // Hiển thị ở ngày hôm nay (ngày yêu cầu thu phần còn lại), KHÔNG dùng ngày hẹn gốc —
                // nếu không, mục này sẽ nằm ở ngày quá khứ và biến mất khỏi bộ lọc mặc định "hôm nay".
                AppointmentDate = DateTimeOffset.UtcNow,
                Diagnosis = $"Thu phần còn lại của hóa đơn {parent.InvoiceNumber}",
                Items = new List<InvoiceItemDto> { new($"Phần còn lại - HĐ {parent.InvoiceNumber}", 1, remaining) },
                SuggestedTotal = remaining,
                OutstandingInvoiceId = parent.Id,
                SourceInvoiceNumber = parent.InvoiceNumber
            });
        }

        return result;
    }
}
