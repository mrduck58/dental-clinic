using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using static DentalClinic.API.Application.UseCases.Invoices.InvoiceHelpers;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record IssueInvoiceCommand(
    Guid AppointmentId,
    List<IssueInvoiceItemRequest> Items,
    decimal Discount,
    string PaymentMethod,
    string? PaymentType,
    decimal DepositAmount,
    string? Notes,
    Guid? ParentInvoiceId,
    Guid? TreatmentPlanId,
    Guid? PromotionId = null) : IRequest<InvoiceDto>;

/// <summary>Xuất hóa đơn từ liệu trình điều trị của một lịch hẹn (hoặc thu phần còn lại, hoặc một đợt thu liệu trình).</summary>
public class IssueInvoiceHandler(
    IInvoiceRepository invoiceRepository,
    IPromotionRepository promotionRepository,
    INotificationService notificationService,
    InvoiceQueryHelper invoiceQuery) : IRequestHandler<IssueInvoiceCommand, InvoiceDto>
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<InvoiceDto> Handle(IssueInvoiceCommand command, CancellationToken ct)
    {
        // Trường hợp thu một đợt của liệu trình điều trị.
        if (command.TreatmentPlanId is Guid treatmentPlanId)
            return await IssuePlanInstallmentAsync(treatmentPlanId, command, ct);

        // Trường hợp thu phần còn lại của một hóa đơn đặt cọc.
        if (command.ParentInvoiceId is Guid parentId)
            return await IssueRemainingAsync(parentId, command, ct);

        var appointment = await invoiceRepository.GetAppointmentWithInvoicesAsync(command.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.PendingPayment)
            throw new ValidationException("Chỉ có thể xuất hóa đơn cho lịch hẹn đã kết thúc điều trị (chờ thanh toán).");

        if (command.Items == null || command.Items.Count == 0)
            throw new ValidationException("Hóa đơn phải có ít nhất một dịch vụ.");

        var subtotal = command.Items.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice);

        // Liệu trình gắn với các dòng hóa đơn — dùng cho cả (1) chặn xuất vượt tổng tiền dịch vụ bên
        // dưới, và (2) biết ServiceId THẬT của từng dòng để khớp đúng khuyến mãi.
        var lineByPlan = command.Items
            .Where(i => i.TreatmentPlanId != null)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice));
        var plans = lineByPlan.Count > 0
            ? await invoiceRepository.GetTreatmentPlanBillingInfoAsync(lineByPlan.Keys.ToList(), ct)
            : Array.Empty<TreatmentPlanBillingInfo>();
        var planServiceMap = plans.ToDictionary(p => p.Id, p => p.ServiceId);

        // Nếu staff chọn khuyến mãi, số tiền giảm luôn được TÍNH LẠI ở server từ Promotion thật —
        // không tin số discount client gửi lên, tránh bị sửa giảm giá vượt mức khuyến mãi cho phép.
        // Chỉ tính khuyến mãi trên ĐÚNG các dòng thuộc dịch vụ được áp dụng (theo ServiceId của liệu
        // trình gắn với dòng đó) — KHÔNG áp dụng lên toàn bộ hóa đơn như trước (dòng dịch vụ khác
        // không liên quan không được giảm giá theo). So theo ServiceId (không phải giá gốc dịch vụ)
        // nên tự động đúng cho MỌI option đã chọn của dịch vụ, vì UnitPrice của dòng luôn là giá
        // option thực tế đã dùng, không phải Service.Price.
        var effectiveDiscount = 0m;
        if (command.PromotionId is Guid promotionId)
        {
            var promotion = await promotionRepository.GetByIdAsync(promotionId, ct)
                ?? throw new ValidationException("Không tìm thấy khuyến mãi.");
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTz).DateTime);
            if (!promotion.IsActive || promotion.StartDate > today || promotion.EndDate < today)
                throw new ValidationException("Khuyến mãi không còn hiệu lực.");

            // ServiceIds rỗng nghĩa là khuyến mãi áp dụng cho TẤT CẢ dịch vụ (quy ước sẵn có).
            var promotedServiceIds = promotion.GetServiceIds().ToHashSet();
            var eligibleSubtotal = promotedServiceIds.Count == 0
                ? subtotal
                : command.Items
                    .Where(i => i.TreatmentPlanId is Guid tpId
                                && planServiceMap.TryGetValue(tpId, out var sid)
                                && promotedServiceIds.Contains(sid))
                    .Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice);

            if (eligibleSubtotal <= 0)
                throw new ValidationException("Khuyến mãi không áp dụng cho các dịch vụ trong hóa đơn này.");

            effectiveDiscount = promotion.DiscountType == "Percentage"
                ? Math.Round(eligibleSubtotal * promotion.DiscountValue / 100, 0)
                : Math.Min(promotion.DiscountValue, eligibleSubtotal);
        }

        var totalAmount = subtotal - effectiveDiscount;
        var depositAmount = command.Items.Sum(i => i.AmountCollected ?? (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice);
        if (depositAmount > totalAmount)
            throw new ValidationException("Số tiền thu không được vượt quá tổng tiền hóa đơn.");
        if (depositAmount <= 0)
            throw new ValidationException("Số tiền thu phải lớn hơn 0.");

        // Cho phép nhiều hóa đơn/buổi, nhưng chặn xuất vượt tổng tiền của mỗi liệu trình
        // (tránh xuất trùng dịch vụ đã có hóa đơn trước đó).
        if (lineByPlan.Count > 0)
        {
            var billedMap = await invoiceQuery.GetPlanBilledMapAsync(lineByPlan.Keys.ToList(), ct);
            foreach (var p in plans)
            {
                var total = p.UnitPrice * Math.Max(1, p.Quantity);
                var remainingToBill = total - billedMap.GetValueOrDefault(p.Id, 0m);
                if (lineByPlan[p.Id] > remainingToBill + 1m)
                    throw new ValidationException("Số tiền xuất hóa đơn vượt quá phần chưa xuất của dịch vụ.");
            }
        }

        var paymentMethod = ParsePaymentMethod(command.PaymentMethod);

        // Không tự sinh + Add + Save riêng lẻ ở đây nữa: IssueWithUniqueNumberAsync tự sinh số VÀ lưu
        // trong cùng 1 bước, tự thử lại nếu đụng số do 2 yêu cầu xuất hóa đơn gần như đồng thời.
        var invoice = await invoiceRepository.IssueWithUniqueNumberAsync(invoiceNumber => Invoice.Issue(
            appointment.Id,
            invoiceNumber,
            command.Items.Select(i => (i.Name, i.Quantity, i.UnitPrice, i.TreatmentPlanId, i.AmountCollected)),
            effectiveDiscount,
            paymentMethod,
            command.Notes,
            command.PromotionId), ct);

        await NotifyInvoiceIssuedAsync(invoice, ct);

        return await invoiceQuery.GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>Tạo hóa đơn thu nốt phần còn lại cho một hóa đơn đặt cọc.</summary>
    private async Task<InvoiceDto> IssueRemainingAsync(Guid parentId, IssueInvoiceCommand command, CancellationToken ct)
    {
        var parent = await invoiceRepository.GetByIdAsync(parentId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn gốc.");

        if (parent.IsSettled || parent.RemainingAmount <= 0)
            throw new ValidationException("Hóa đơn này đã được thu đủ.");

        var alreadyHasChild = await invoiceRepository.HasChildInvoiceAsync(parentId, ct);
        if (alreadyHasChild)
            throw new ConflictException("Đã tạo hóa đơn thu phần còn lại cho hóa đơn này.");

        var paymentMethod = ParsePaymentMethod(command.PaymentMethod);

        var invoice = await invoiceRepository.IssueWithUniqueNumberAsync(invoiceNumber => Invoice.IssueRemaining(
            parent.AppointmentId,
            invoiceNumber,
            parent.Id,
            $"Phần còn lại - HĐ {parent.InvoiceNumber}",
            parent.RemainingAmount,
            paymentMethod,
            command.Notes), ct);

        await NotifyInvoiceIssuedAsync(invoice, ct);

        return await invoiceQuery.GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>Tạo một đợt thu của liệu trình điều trị (số tiền tùy ý, không vượt công nợ còn lại).</summary>
    private async Task<InvoiceDto> IssuePlanInstallmentAsync(Guid treatmentPlanId, IssueInvoiceCommand command, CancellationToken ct)
    {
        var plan = await invoiceRepository.GetTreatmentPlanWithServiceAsync(treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình.");

        if (plan.Status != TreatmentPlanStatus.InProgress)
            throw new ValidationException("Chỉ có thể thu đợt cho liệu trình đang điều trị.");

        var appointment = await invoiceRepository.GetAppointmentWithInvoicesAsync(command.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.PatientId != plan.PatientId)
            throw new ValidationException("Lịch hẹn không thuộc bệnh nhân của liệu trình này.");

        if (appointment.Status != AppointmentStatus.PendingPayment)
            throw new ValidationException("Chỉ có thể thu khi buổi hẹn đã kết thúc điều trị (chờ thanh toán).");

        if (appointment.Invoices.Any())
            throw new ConflictException("Buổi hẹn này đã có đợt thu.");

        var amount = command.Items?.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice) ?? 0;
        if (amount <= 0)
            throw new ValidationException("Số tiền thu phải lớn hơn 0.");

        var paid = await invoiceQuery.GetPlanPaidAsync(treatmentPlanId, ct);
        var remaining = plan.TotalCost - paid;
        if (amount > remaining)
            throw new ValidationException($"Số tiền thu vượt quá công nợ còn lại ({remaining:#,##0}đ).");

        var paymentMethod = ParsePaymentMethod(command.PaymentMethod);

        var invoice = await invoiceRepository.IssueWithUniqueNumberAsync(invoiceNumber => Invoice.IssuePlanInstallment(
            appointment.Id,
            treatmentPlanId,
            invoiceNumber,
            $"Đợt thu - {BuildPlanName(plan)}",
            amount,
            paymentMethod,
            command.Notes), ct);

        await NotifyInvoiceIssuedAsync(invoice, ct);

        return await invoiceQuery.GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>Báo cho bệnh nhân (nếu có tài khoản liên kết) khi có hóa đơn mới cần thanh toán.</summary>
    private async Task NotifyInvoiceIssuedAsync(Invoice invoice, CancellationToken ct)
    {
        if (!invoice.AppointmentId.HasValue) return;
        var patientUserId = await invoiceRepository.GetPatientUserIdByAppointmentIdAsync(invoice.AppointmentId.Value, ct);
        if (patientUserId is not Guid userId) return;

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: userId,
            Type: NotificationType.Invoice,
            Priority: NotificationPriority.Medium,
            Title: "Hóa đơn mới",
            Body: $"Bạn có hóa đơn {invoice.InvoiceNumber} cần thanh toán ({invoice.DepositAmount:#,##0}đ).",
            RelatedEntityType: "Invoice",
            RelatedEntityId: invoice.Id.ToString()), ct);
    }
}
