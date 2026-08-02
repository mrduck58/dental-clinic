using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Invoices;

public class PaymentConfirmationService(
    AppDbContext dbContext,
    INotificationService notificationService,
    IUserRepository userRepository,
    InvoiceQueryHelper invoiceQuery) : IPaymentConfirmationService
{
    public async Task ConfirmTransactionSuccessAsync(
        PaymentTransaction transaction, string? gatewayTransactionId, string rawPayload, CancellationToken ct)
    {
        transaction.MarkSuccess(gatewayTransactionId, rawPayload);
        await dbContext.SaveChangesAsync(ct);

        var paymentMethod = transaction.Gateway == PaymentGateway.PayOS && transaction.Invoice.PaymentMethod == PaymentMethod.BankTransfer
            ? PaymentMethod.BankTransfer
            : PaymentMethod.OnlinePayment;

        // ConfirmInvoicePaymentAsync tự đóng mọi giao dịch Pending còn sót lại của hóa đơn này (kể cả các giao
        // dịch trùng do race trước khi sửa) — không cần lặp lại việc đó ở đây.
        await ConfirmInvoicePaymentAsync(transaction.Invoice, paymentMethod, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task ConfirmInvoicePaymentAsync(Invoice invoice, PaymentMethod paymentMethod, CancellationToken ct)
    {
        if (invoice.Status == PaymentStatus.Paid) return;

        invoice.MarkAsPaid(paymentMethod);

        // Đóng mọi giao dịch cổng thanh toán còn Pending của hóa đơn này (nếu có) — hóa đơn vừa được xác nhận
        // thanh toán (thủ công, qua webhook, hay qua đối soát dự phòng — hàm này dùng chung cho cả 3 đường), các
        // giao dịch Pending khác chắc chắn sẽ không bao giờ Paid được nữa (CreatePaymentRequestAsync chặn tạo mới
        // khi hóa đơn đã Paid). Nếu không dọn, dữ liệu sẽ có invoice=Paid nhưng transaction=Pending mãi mãi — gây
        // nhiễu khi tra soát và tốn lệnh gọi PayOS mỗi lượt poll nếu còn client đang mở màn hình hóa đơn đó.
        var pendingTxns = await dbContext.PaymentTransactions
            .Where(t => t.InvoiceId == invoice.Id && t.Status == TransactionStatus.Pending)
            .ToListAsync(ct);
        foreach (var t in pendingTxns)
            t.MarkFailed("Hóa đơn đã được xác nhận thanh toán.", null);

        // Nếu đây là hóa đơn thu phần còn lại → tất toán hóa đơn cọc gốc.
        Invoice? parent = null;
        if (invoice.ParentInvoiceId is Guid parentId)
        {
            parent = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == parentId, ct);
            parent?.Settle();
        }

        // Credit công nợ cho từng liệu trình theo SỐ THU CỦA TỪNG DÒNG.
        // - Hóa đơn thường: mỗi dòng credit đúng AmountCollected của nó (toàn bộ hoặc cọc của dòng).
        // - Hóa đơn thu phần còn lại: lấy dòng của hóa đơn cọc gốc, credit nốt phần còn nợ của từng dòng.
        var creditSource = parent ?? invoice;
        var creditRemaining = parent != null; // true → thu nốt phần còn lại của các dòng gốc

        var lines = await dbContext.InvoiceItems
            .Where(it => it.InvoiceId == creditSource.Id && it.TreatmentPlanId != null)
            .Select(it => new
            {
                PlanId = it.TreatmentPlanId!.Value,
                Collected = it.AmountCollected,
                Line = it.Quantity * it.UnitPrice
            })
            .ToListAsync(ct);

        var creditByPlan = new Dictionary<Guid, decimal>();
        foreach (var l in lines)
        {
            var credit = creditRemaining ? Math.Max(0, l.Line - l.Collected) : l.Collected;
            creditByPlan[l.PlanId] = creditByPlan.GetValueOrDefault(l.PlanId, 0m) + credit;
        }
        // Tương thích ngược: hóa đơn "đợt thu" cũ gắn liệu trình ở cấp hóa đơn.
        if ((parent?.TreatmentPlanId ?? invoice.TreatmentPlanId) is Guid headerPlanId)
        {
            var headerCredit = creditRemaining ? (parent?.RemainingAmount ?? 0m) : invoice.DepositAmount;
            creditByPlan[headerPlanId] = creditByPlan.GetValueOrDefault(headerPlanId, 0m) + headerCredit;
        }

        foreach (var (pid, thisCredit) in creditByPlan)
        {
            var plan = await dbContext.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == pid, ct);
            if (plan != null && plan.Status == TreatmentPlanStatus.InProgress)
            {
                // GetPlanPaidAsync đọc từ DB (chưa gồm thay đổi lần này) → cộng thêm số vừa thu.
                var paidBefore = await invoiceQuery.GetPlanPaidAsync(pid, ct);
                if (paidBefore + thisCredit >= plan.TotalCost - 1m) // dung sai làm tròn
                    plan.SetStatus(TreatmentPlanStatus.Completed);
            }
        }

        // Hoàn tất lịch hẹn CHỈ khi mọi dịch vụ trong chuỗi đã xuất hóa đơn hết
        // (cho phép còn dịch vụ khác của buổi chưa xuất → giữ buổi ở "chờ thanh toán").
        if (invoice.Appointment.Status == AppointmentStatus.PendingPayment)
        {
            var chain = await invoiceQuery.GetChainAsync(invoice.AppointmentId, ct);
            var chainPlans = await dbContext.TreatmentPlans
                .Where(tp => tp.AppointmentId != null && chain.Contains(tp.AppointmentId.Value)
                             && tp.Status != TreatmentPlanStatus.Cancelled)
                .Select(tp => new { tp.Id, tp.UnitPrice, tp.Quantity })
                .ToListAsync(ct);
            var billedMap = await invoiceQuery.GetPlanBilledMapAsync(chainPlans.Select(p => p.Id).ToList(), ct);
            var anyUnbilled = chainPlans.Any(p =>
                (p.UnitPrice * Math.Max(1, p.Quantity)) - billedMap.GetValueOrDefault(p.Id, 0m) > 1m);
            if (!anyUnbilled)
                invoice.Appointment.Complete();
        }

        await NotifyPaymentConfirmedAsync(invoice, paymentMethod, ct);
    }

    /// <summary>Báo cho bệnh nhân (nếu có tài khoản) và toàn bộ Staff khi một hóa đơn được xác nhận đã thanh toán.</summary>
    private async Task NotifyPaymentConfirmedAsync(Invoice invoice, PaymentMethod paymentMethod, CancellationToken ct)
    {
        var patient = await dbContext.Appointments
            .Where(a => a.Id == invoice.AppointmentId)
            .Select(a => a.Patient)
            .FirstOrDefaultAsync(ct);

        if (patient?.UserId is Guid patientUserId)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId,
                Type: NotificationType.Invoice,
                Priority: NotificationPriority.Medium,
                Title: "Thanh toán thành công",
                Body: $"Hóa đơn {invoice.InvoiceNumber} đã được thanh toán ({invoice.DepositAmount:#,##0}đ) qua {DescribePaymentMethod(paymentMethod)}.",
                RelatedEntityType: "Invoice",
                RelatedEntityId: invoice.Id.ToString()), ct);
        }

        var staffIds = await userRepository.GetUserIdsByRoleAsync("Staff", ct);
        var staffTemplate = new CreateNotificationRequest(
            UserId: Guid.Empty,
            Type: NotificationType.Invoice,
            Priority: NotificationPriority.Low,
            Title: "Đã nhận thanh toán",
            Body: $"Hóa đơn {invoice.InvoiceNumber} đã được thanh toán qua {DescribePaymentMethod(paymentMethod)}.",
            RelatedEntityType: "Invoice",
            RelatedEntityId: invoice.Id.ToString());
        await notificationService.CreateForMultipleUsersAsync(staffIds, staffTemplate, ct);
    }

    private static string DescribePaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "tiền mặt",
        PaymentMethod.BankTransfer => "chuyển khoản",
        PaymentMethod.OnlinePayment => "thanh toán online",
        _ => method.ToString()
    };
}
