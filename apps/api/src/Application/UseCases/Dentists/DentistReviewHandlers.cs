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
    DateTimeOffset CreatedAt);

public record DentistReviewsResultDto(
    double AverageRating,
    int ReviewCount,
    List<DentistReviewDto> Reviews);

/// <summary>Body của POST api/dentists/{id}/reviews — giữ nguyên hình dạng JSON.</summary>
public record CreateDentistReviewRequest(int Rating, string Comment, List<string>? Tags);

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
            reviews.Select(r => new DentistReviewDto(r.Id, r.Patient.FullName, r.Rating, r.Comment, r.Tags, r.CreatedAt)).ToList());
    }
}

/// <summary>Tạo mới hoặc cập nhật đánh giá của bệnh nhân hiện tại cho một nha sĩ.
/// Chỉ cho phép đánh giá nếu bệnh nhân đã có ít nhất 1 buổi khám hoàn tất với nha sĩ đó.</summary>
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

        var hasVisited = await appointmentRepository.HasCompletedVisitAsync(dentistId, patient.Id, ct);
        if (!hasVisited)
            throw new ValidationException("Bạn cần hoàn tất ít nhất 1 buổi khám với nha sĩ này trước khi đánh giá.");

        var existing = await dentistReviewRepository.GetByDentistAndPatientAsync(dentistId, patient.Id, ct);

        if (existing != null)
        {
            existing.Update(request.Rating, request.Comment, request.Tags);
            await dentistReviewRepository.UpdateAsync(existing, ct);
        }
        else
        {
            existing = DentistReview.Create(dentistId, patient.Id, request.Rating, request.Comment, request.Tags);
            await dentistReviewRepository.AddAsync(existing, ct);
        }

        return new DentistReviewDto(existing.Id, patient.FullName, existing.Rating, existing.Comment, existing.Tags, existing.CreatedAt);
    }
}
