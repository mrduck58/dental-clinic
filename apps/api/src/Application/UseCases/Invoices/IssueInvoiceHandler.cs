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
    Guid? TreatmentPlanId) : IRequest<InvoiceDto>;

/// <summary>Xuất hóa đơn từ liệu trình điều trị của một lịch hẹn (hoặc thu phần còn lại, hoặc một đợt thu liệu trình).</summary>
public class IssueInvoiceHandler(
    IInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    InvoiceQueryHelper invoiceQuery) : IRequestHandler<IssueInvoiceCommand, InvoiceDto>
{
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

        var totalAmount = command.Items.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice) - command.Discount;
        var depositAmount = command.Items.Sum(i => i.AmountCollected ?? (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice);
        if (depositAmount > totalAmount)
            throw new ValidationException("Số tiền thu không được vượt quá tổng tiền hóa đơn.");

        // Cho phép nhiều hóa đơn/buổi, nhưng chặn xuất vượt tổng tiền của mỗi liệu trình
        // (tránh xuất trùng dịch vụ đã có hóa đơn trước đó).
        var lineByPlan = command.Items
            .Where(i => i.TreatmentPlanId != null)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice));
        if (lineByPlan.Count > 0)
        {
            var billedMap = await invoiceQuery.GetPlanBilledMapAsync(lineByPlan.Keys.ToList(), ct);
            var plans = await invoiceRepository.GetTreatmentPlanBillingInfoAsync(lineByPlan.Keys.ToList(), ct);
            foreach (var p in plans)
            {
                var total = p.UnitPrice * Math.Max(1, p.Quantity);
                var remainingToBill = total - billedMap.GetValueOrDefault(p.Id, 0m);
                if (lineByPlan[p.Id] > remainingToBill + 1m)
                    throw new ValidationException("Số tiền xuất hóa đơn vượt quá phần chưa xuất của dịch vụ.");
            }
        }

        var paymentMethod = ParsePaymentMethod(command.PaymentMethod);

        // Thu ngay của mỗi dòng: mặc định = thành tiền (thu toàn bộ); nếu đặt cọc thì là số cọc của dòng.
        var invoiceNumber = await invoiceQuery.GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.Issue(
            appointment.Id,
            invoiceNumber,
            command.Items.Select(i => (i.Name, i.Quantity, i.UnitPrice, i.TreatmentPlanId, i.AmountCollected)),
            command.Discount,
            paymentMethod,
            command.Notes);

        if (invoice.DepositAmount <= 0)
            throw new ValidationException("Số tiền thu phải lớn hơn 0.");

        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(ct);
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
        var invoiceNumber = await invoiceQuery.GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.IssueRemaining(
            parent.AppointmentId,
            invoiceNumber,
            parent.Id,
            $"Phần còn lại - HĐ {parent.InvoiceNumber}",
            parent.RemainingAmount,
            paymentMethod,
            command.Notes);

        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(ct);
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
        var invoiceNumber = await invoiceQuery.GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.IssuePlanInstallment(
            appointment.Id,
            treatmentPlanId,
            invoiceNumber,
            $"Đợt thu - {BuildPlanName(plan)}",
            amount,
            paymentMethod,
            command.Notes);

        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(ct);
        await NotifyInvoiceIssuedAsync(invoice, ct);

        return await invoiceQuery.GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>Báo cho bệnh nhân (nếu có tài khoản liên kết) khi có hóa đơn mới cần thanh toán.</summary>
    private async Task NotifyInvoiceIssuedAsync(Invoice invoice, CancellationToken ct)
    {
        var patientUserId = await invoiceRepository.GetPatientUserIdByAppointmentIdAsync(invoice.AppointmentId, ct);
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
