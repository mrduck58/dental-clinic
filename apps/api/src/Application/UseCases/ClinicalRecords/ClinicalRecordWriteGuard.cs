using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

/// <summary>
/// Bác sĩ chỉ được GHI (bắt đầu khám, chẩn đoán, liệu trình, đơn thuốc) lên ca khám được phân công
/// cho mình. ĐỌC không bị chặn: tra cứu bệnh án và lịch sử điều trị do đồng nghiệp thực hiện là nhu
/// cầu điều trị liên tục bình thường.
///
/// Staff/Admin/Owner không bị guard này giới hạn — họ vận hành trên mọi ca theo đúng vai trò.
///
/// Ném <see cref="ForbiddenException"/> (403) chứ không phải 404 như các guard quyền sở hữu khác
/// trong dự án: ở đây bác sĩ ĐƯỢC PHÉP đọc chính bản ghi đó, nên giấu sự tồn tại của nó là vô nghĩa
/// và chỉ gây khó hiểu ("không tìm thấy" trong khi màn hình đang hiển thị nó).
/// </summary>
public class ClinicalRecordWriteGuard(
    ICurrentUserService currentUser,
    IDentistRepository dentistRepository,
    IAppointmentRepository appointmentRepository,
    IDiagnosisRepository diagnosisRepository,
    IAppointmentPhotoRepository appointmentPhotoRepository,
    ITreatmentPlanRepository treatmentPlanRepository,
    IPrescriptionRepository prescriptionRepository,
    IPrescriptionItemRepository prescriptionItemRepository)
{
    private const string DeniedMessage = "Bạn chỉ được cập nhật bệnh án của ca khám được phân công cho mình.";

    public async Task EnsureCanWriteAppointmentAsync(Guid appointmentId, CancellationToken ct)
    {
        if (await CurrentDentistIdAsync(ct) is not Guid dentistId) return;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        Assert(appointment.DentistId == dentistId);
    }

    public async Task EnsureCanWriteDiagnosisAsync(Guid diagnosisId, CancellationToken ct)
    {
        if (await CurrentDentistIdAsync(ct) is null) return;

        var diagnosis = await diagnosisRepository.GetByIdAsync(diagnosisId, ct)
            ?? throw new NotFoundException("Không tìm thấy chẩn đoán.");

        await EnsureCanWriteAppointmentAsync(diagnosis.AppointmentId, ct);
    }

    public async Task EnsureCanWritePhotoAsync(Guid photoId, CancellationToken ct)
    {
        if (await CurrentDentistIdAsync(ct) is null) return;

        var photo = await appointmentPhotoRepository.GetByIdAsync(photoId, ct)
            ?? throw new NotFoundException("Không tìm thấy ảnh.");

        await EnsureCanWriteAppointmentAsync(photo.AppointmentId, ct);
    }

    public async Task EnsureCanWriteTreatmentPlanAsync(Guid treatmentPlanId, CancellationToken ct)
    {
        if (await CurrentDentistIdAsync(ct) is not Guid dentistId) return;

        var plan = await treatmentPlanRepository.GetByIdAsync(treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        // Đối chiếu thẳng TreatmentPlan.DentistId thay vì đi vòng qua ca khám: AppointmentId của liệu
        // trình là nullable (SetNull khi ca khám bị xóa), đi vòng sẽ mất chốt chặn đúng lúc dữ liệu rối nhất.
        Assert(plan.DentistId == dentistId);
    }

    public async Task EnsureCanWritePrescriptionAsync(Guid prescriptionId, CancellationToken ct)
    {
        if (await CurrentDentistIdAsync(ct) is null) return;

        var prescription = await prescriptionRepository.GetByIdWithItemsAsync(prescriptionId, ct)
            ?? throw new NotFoundException("Không tìm thấy đơn thuốc.");

        await EnsureCanWriteAppointmentAsync(prescription.AppointmentId, ct);
    }

    public async Task EnsureCanWritePrescriptionItemAsync(Guid itemId, CancellationToken ct)
    {
        if (await CurrentDentistIdAsync(ct) is null) return;

        var item = await prescriptionItemRepository.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Không tìm thấy thuốc trong đơn.");

        await EnsureCanWritePrescriptionAsync(item.PrescriptionId, ct);
    }

    /// <summary>
    /// Id hồ sơ bác sĩ của người đang gọi, hoặc <c>null</c> nếu họ không phải bác sĩ (⇒ không giới hạn).
    /// Bác sĩ chưa có hồ sơ DentistProfile bị chặn thay vì cho qua — không xác định được ca nào là của họ
    /// thì không có cơ sở nào để cho ghi.
    /// </summary>
    private async Task<Guid?> CurrentDentistIdAsync(CancellationToken ct)
    {
        if (currentUser.UserRole != nameof(Domain.Enums.UserRole.Dentist)) return null;

        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");

        var profile = await dentistRepository.GetByUserIdAsync(userId, ct)
            ?? throw new ForbiddenException(DeniedMessage);

        return profile.Id;
    }

    private static void Assert(bool isOwnAppointment)
    {
        if (!isOwnAppointment) throw new ForbiddenException(DeniedMessage);
    }
}
