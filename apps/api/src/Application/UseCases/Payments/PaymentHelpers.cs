using DentalClinic.API.Application.DTOs.Payments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Payments;

internal static class PaymentHelpers
{
    public static PaymentTransactionDto ToDto(PaymentTransaction t) => new(
        t.Id, t.InvoiceId, t.Gateway.ToString(), t.Status.ToString(), t.GatewayOrderCode, t.Amount,
        t.CheckoutUrl, t.QrCode, t.CreatedAt, t.ExpiresAt);

    /// <summary>
    /// Khẳng định hóa đơn thuộc phạm vi của <paramref name="userId"/> — hồ sơ chính chủ hoặc một
    /// thành viên gia đình dưới tài khoản đó (bệnh nhân trả tiền hộ vợ/con là luồng bình thường).
    /// Chỉ gọi khi người yêu cầu là Patient; nhân viên phòng khám thao tác trên mọi hóa đơn.
    ///
    /// Hóa đơn ngoài phạm vi trả 404 chứ không 403: chỉ cần đoán invoiceId mà nhận được 403 là đã
    /// biết hóa đơn đó có thật, đủ để dò số lượng/khoảng id hóa đơn của phòng khám.
    /// </summary>
    public static async Task EnsureInvoiceBelongsToUserAsync(
        Invoice invoice,
        Guid userId,
        IPatientRepository patientRepository,
        CancellationToken ct)
    {
        var primaryPatient = await patientRepository.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        var patientId = invoice.Appointment.PatientId;
        if (patientId == primaryPatient.Id) return;

        // Nav property Patient.FamilyMembers KHÔNG được Include trong GetByUserIdAsync — đọc nó ở đây
        // sẽ luôn rỗng và chặn nhầm việc trả hộ người nhà. Phải hỏi repository tường minh.
        var familyMembers = await patientRepository.GetFamilyMembersAsync(primaryPatient.Id, ct);
        if (familyMembers.Any(m => m.Id == patientId)) return;

        throw new NotFoundException("Không tìm thấy hóa đơn.");
    }
}
