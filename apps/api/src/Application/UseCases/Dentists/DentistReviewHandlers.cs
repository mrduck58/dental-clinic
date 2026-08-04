using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dentists;

public record DentistReviewDto(
    Guid Id,
    string PatientName,
    int Rating,
    string Comment,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    string? ServiceName = null);

public record DentistReviewsResultDto(
    double AverageRating,
    int ReviewCount,
    List<DentistReviewDto> Reviews);

public static class NameMasker
{
    public static string MaskName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "***";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var word = parts[0];
            if (word.Length <= 2) return word[0] + "*";
            return word[0] + new string('*', word.Length - 2) + word[^1];
        }

        return string.Join(" ", parts.Select(w =>
        {
            if (w.Length <= 2) return w[0] + "*";
            return w[0] + new string('*', w.Length - 2) + w[^1];
        }));
    }
}

/// <summary>Body của POST api/dentists/{id}/reviews — giữ nguyên hình dạng JSON.</summary>
public record CreateDentistReviewRequest(int Rating, string Comment, List<string>? Tags, Guid? AppointmentId = null);

public record GetDentistReviewsQuery(Guid DentistId) : IRequest<DentistReviewsResultDto>;

public record UpsertDentistReviewCommand(Guid DentistId, Guid UserId, CreateDentistReviewRequest Request)
    : IRequest<DentistReviewDto>;

public class GetDentistReviewsHandler(IDentistReviewRepository dentistReviewRepository)
    : IRequestHandler<GetDentistReviewsQuery, DentistReviewsResultDto>
{
    public async Task<DentistReviewsResultDto> Handle(GetDentistReviewsQuery request, CancellationToken ct)
    {
        var reviews = await dentistReviewRepository.GetByDentistIdAsync(request.DentistId, ct);

        var avg = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1);

        return new DentistReviewsResultDto(
            avg,
            reviews.Count,
            reviews.Select(r => new DentistReviewDto(
                r.Id,
                NameMasker.MaskName(r.Patient.FullName),
                r.Rating,
                r.Comment,
                r.Tags,
                r.CreatedAt,
                r.Appointment?.Service?.Name)).ToList());
    }
}

/// <summary>Tạo mới đánh giá của bệnh nhân cho một lượt khám/điều trị với nha sĩ.
/// Một lượt khám chỉ được đánh giá 1 lần duy nhất, sau khi đánh giá sẽ không thể sửa lại.</summary>
public class UpsertDentistReviewHandler(
    IDentistReviewRepository dentistReviewRepository,
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository)
    : IRequestHandler<UpsertDentistReviewCommand, DentistReviewDto>
{
    public async Task<DentistReviewDto> Handle(UpsertDentistReviewCommand command, CancellationToken ct)
    {
        var dentistId = command.DentistId;
        var userId = command.UserId;
        var request = command.Request;

        if (request.Rating < 1 || request.Rating > 5)
            throw new ValidationException("Đánh giá phải từ 1 đến 5 sao.");
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ValidationException("Vui lòng nhập nội dung đánh giá.");

        var patient = await patientRepository.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ bệnh nhân.");

        if (request.AppointmentId.HasValue)
        {
            var appt = await appointmentRepository.GetByIdAsync(request.AppointmentId.Value, ct);
            if (appt == null)
                throw new NotFoundException("Không tìm thấy lịch hẹn.");
            if (appt.Status != DentalClinic.API.Domain.Enums.AppointmentStatus.Completed &&
                appt.Status != DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment)
            {
                throw new ValidationException("Buổi khám này chưa hoàn tất.");
            }

            var reviewForAppt = await dentistReviewRepository.GetByAppointmentIdAsync(request.AppointmentId.Value, ct);
            if (reviewForAppt != null)
            {
                throw new ValidationException("Bạn đã gửi đánh giá cho lượt khám này rồi và không thể chỉnh sửa.");
            }

            var newApptReview = DentistReview.Create(dentistId, patient.Id, request.Rating, request.Comment, request.Tags, request.AppointmentId);
            await dentistReviewRepository.AddAsync(newApptReview, ct);

            return new DentistReviewDto(newApptReview.Id, NameMasker.MaskName(patient.FullName), newApptReview.Rating, newApptReview.Comment, newApptReview.Tags, newApptReview.CreatedAt, appt.Service?.Name);
        }

        var completedVisits = await appointmentRepository.CountCompletedVisitsForPatientAsync(dentistId, patient.Id, ct);
        if (completedVisits == 0)
            throw new ValidationException("Bạn cần hoàn tất ít nhất 1 buổi khám với nha sĩ này trước khi đánh giá.");

        var existingCount = await dentistReviewRepository.CountByDentistAndPatientAsync(dentistId, patient.Id, ct);
        if (existingCount >= completedVisits)
            throw new ValidationException("Bạn đã gửi đánh giá cho đợt khám vừa rồi với nha sĩ này. Hãy hoàn tất buổi khám tiếp theo để gửi thêm đánh giá.");

        var newReview = DentistReview.Create(dentistId, patient.Id, request.Rating, request.Comment, request.Tags, request.AppointmentId);
        await dentistReviewRepository.AddAsync(newReview, ct);

        return new DentistReviewDto(newReview.Id, NameMasker.MaskName(patient.FullName), newReview.Rating, newReview.Comment, newReview.Tags, newReview.CreatedAt);
    }
}

public record ReviewEligibilityDto(bool CanReview, string Reason, DentistReviewDto? MyReview);

public record GetDentistReviewEligibilityQuery(Guid DentistId, Guid UserId, Guid? AppointmentId = null) : IRequest<ReviewEligibilityDto>;

public class GetDentistReviewEligibilityHandler(
    IDentistReviewRepository dentistReviewRepository,
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository)
    : IRequestHandler<GetDentistReviewEligibilityQuery, ReviewEligibilityDto>
{
    public async Task<ReviewEligibilityDto> Handle(GetDentistReviewEligibilityQuery query, CancellationToken ct)
    {
        var patient = await patientRepository.GetByUserIdAsync(query.UserId, ct);
        if (patient == null)
        {
            return new ReviewEligibilityDto(false, "Không tìm thấy hồ sơ bệnh nhân.", null);
        }

        if (query.AppointmentId.HasValue)
        {
            var appt = await appointmentRepository.GetByIdAsync(query.AppointmentId.Value, ct);
            if (appt == null)
            {
                return new ReviewEligibilityDto(false, "Không tìm thấy lịch hẹn.", null);
            }
            if (appt.Status != DentalClinic.API.Domain.Enums.AppointmentStatus.Completed &&
                appt.Status != DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment)
            {
                return new ReviewEligibilityDto(false, "Buổi khám này chưa hoàn tất.", null);
            }

            var reviewForAppt = await dentistReviewRepository.GetByAppointmentIdAsync(query.AppointmentId.Value, ct);
            if (reviewForAppt != null)
            {
                var apptReviewDto = new DentistReviewDto(reviewForAppt.Id, NameMasker.MaskName(patient.FullName), reviewForAppt.Rating, reviewForAppt.Comment, reviewForAppt.Tags, reviewForAppt.CreatedAt, appt.Service?.Name);
                return new ReviewEligibilityDto(false, "Bạn đã gửi đánh giá cho lượt khám này rồi và không thể chỉnh sửa.", apptReviewDto);
            }

            return new ReviewEligibilityDto(true, "Đủ điều kiện đánh giá lượt khám này.", null);
        }

        var completedVisits = await appointmentRepository.CountCompletedVisitsForPatientAsync(query.DentistId, patient.Id, ct);
        if (completedVisits == 0)
        {
            return new ReviewEligibilityDto(false, "Bạn cần hoàn tất ít nhất 1 buổi khám với nha sĩ này trước khi đánh giá.", null);
        }

        var existingCount = await dentistReviewRepository.CountByDentistAndPatientAsync(query.DentistId, patient.Id, ct);
        if (existingCount >= completedVisits)
        {
            return new ReviewEligibilityDto(false, "Bạn đã gửi đánh giá cho đợt khám vừa rồi với nha sĩ này. Hãy hoàn tất buổi khám tiếp theo để tiếp tục gửi đánh giá.", null);
        }

        return new ReviewEligibilityDto(true, "Đủ điều kiện đánh giá nha sĩ.", null);
    }
}
