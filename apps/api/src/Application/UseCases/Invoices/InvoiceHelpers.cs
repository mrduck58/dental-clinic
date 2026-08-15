using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.Invoices;

/// <summary>Hàm mapping/parse thuần túy dùng chung giữa các handler Invoice — không phụ thuộc DbContext.</summary>
internal static class InvoiceHelpers
{
    /// <summary>Tên hiển thị của một liệu trình: tên dịch vụ + răng điều trị (nếu có).</summary>
    public static string BuildPlanName(TreatmentPlan tp) =>
        string.IsNullOrWhiteSpace(tp.Teeth) ? tp.Service.Name : $"{tp.Service.Name} - Răng {tp.Teeth}";

    public static string BuildAppointmentCode(Appointment a) =>
        $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}";

    public static PaymentMethod ParsePaymentMethod(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "cash" or "tienmat" => PaymentMethod.Cash,
        "transfer" or "banktransfer" or "bank_transfer" => PaymentMethod.BankTransfer,
        "app" or "online" or "onlinepayment" or "online_payment" => PaymentMethod.OnlinePayment,
        _ when Enum.TryParse<PaymentMethod>(value, ignoreCase: true, out var m) => m,
        _ => throw new ValidationException("Phương thức thanh toán không hợp lệ.")
    };

    public static PaymentType ParsePaymentType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "full" or "toanbo" => PaymentType.Full,
        "deposit" or "datcoc" => PaymentType.Deposit,
        _ when Enum.TryParse<PaymentType>(value, ignoreCase: true, out var t) => t,
        _ => throw new ValidationException("Loại thanh toán không hợp lệ.")
    };

    public static InvoiceDto ToDto(Invoice invoice)
    {
        var appointment = invoice.Appointment;
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            AppointmentId = invoice.AppointmentId,
            PatientName = appointment.Patient.FullName,
            PatientPhone = appointment.Patient.User?.PhoneNumber,
            Gender = appointment.Patient.Gender,
            DentistName = appointment.Dentist.FullName,
            AppointmentDate = appointment.AppointmentDate,
            Items = invoice.Items
                .Select(i => new InvoiceItemDto(i.Name, i.Quantity, i.UnitPrice, i.TreatmentPlanId))
                .ToList(),
            Subtotal = invoice.Subtotal,
            Discount = invoice.Discount,
            PromotionId = invoice.PromotionId,
            PromotionCode = invoice.Promotion?.Code,
            PromotionName = invoice.Promotion?.Name,
            TotalAmount = invoice.TotalAmount,
            PaymentType = invoice.PaymentType.ToString(),
            DepositAmount = invoice.DepositAmount,
            RemainingAmount = invoice.RemainingAmount,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            Status = invoice.Status.ToString(),
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            PaymentDate = invoice.PaymentDate,
            ParentInvoiceId = invoice.ParentInvoiceId,
            IsSettled = invoice.IsSettled,
            CollectingRemaining = invoice.CollectingRemaining
        };
    }
}
