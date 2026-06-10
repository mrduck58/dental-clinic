using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Invoice
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public DateTimeOffset? PaymentDate { get; private set; }

    // Navigation property
    public Appointment Appointment { get; private set; } = null!;

    private Invoice() { }

    public static Invoice Create(Guid appointmentId, decimal totalAmount, PaymentMethod paymentMethod)
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            TotalAmount = totalAmount,
            Status = PaymentStatus.Unpaid,
            PaymentMethod = paymentMethod
        };
    }

    public void MarkAsPaid()
    {
        Status = PaymentStatus.Paid;
        PaymentDate = DateTimeOffset.UtcNow;
    }

    public void Refund() => Status = PaymentStatus.Refunded;
}
