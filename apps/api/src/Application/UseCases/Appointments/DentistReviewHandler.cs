using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

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

public record CreateDentistReviewRequest(int Rating, string Comment, List<string>? Tags);

public class DentistReviewHandler(AppDbContext dbContext, IPatientRepository patientRepository)
{
    public async Task<DentistReviewsResultDto> GetForDentistAsync(Guid dentistId, CancellationToken ct = default)
    {
        var reviews = await dbContext.DentistReviews
            .AsNoTracking()
            .Include(r => r.Patient)
            .Where(r => r.DentistId == dentistId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var avg = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1);

        return new DentistReviewsResultDto(
            avg,
            reviews.Count,
            reviews.Select(r => new DentistReviewDto(r.Id, r.Patient.FullName, r.Rating, r.Comment, r.Tags, r.CreatedAt)).ToList());
    }

    /// <summary>Tạo mới hoặc cập nhật đánh giá của bệnh nhân hiện tại cho một nha sĩ.
    /// Chỉ cho phép đánh giá nếu bệnh nhân đã có ít nhất 1 buổi khám hoàn tất với nha sĩ đó.</summary>
    public async Task<DentistReviewDto> UpsertAsync(Guid dentistId, Guid userId, CreateDentistReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ValidationException("Đánh giá phải từ 1 đến 5 sao.");
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ValidationException("Vui lòng nhập nội dung đánh giá.");

        var patient = await patientRepository.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ bệnh nhân.");

        var hasVisited = await dbContext.Appointments.AnyAsync(a =>
            a.DentistId == dentistId && a.PatientId == patient.Id &&
            (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment), ct);
        if (!hasVisited)
            throw new ValidationException("Bạn cần hoàn tất ít nhất 1 buổi khám với nha sĩ này trước khi đánh giá.");

        var existing = await dbContext.DentistReviews
            .FirstOrDefaultAsync(r => r.DentistId == dentistId && r.PatientId == patient.Id, ct);

        if (existing != null)
        {
            existing.Update(request.Rating, request.Comment, request.Tags);
        }
        else
        {
            existing = DentistReview.Create(dentistId, patient.Id, request.Rating, request.Comment, request.Tags);
            dbContext.DentistReviews.Add(existing);
        }

        await dbContext.SaveChangesAsync(ct);
        return new DentistReviewDto(existing.Id, patient.FullName, existing.Rating, existing.Comment, existing.Tags, existing.CreatedAt);
    }
}

public record ReviewEligibilityDto(bool CanReview, string Reason, DentistReviewDto? MyReview);

public record GetDentistReviewEligibilityQuery(Guid DentistId, Guid UserId) : IRequest<ReviewEligibilityDto>;

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

        var hasVisited = await appointmentRepository.HasCompletedVisitAsync(query.DentistId, patient.Id, ct);
        if (!hasVisited)
        {
            return new ReviewEligibilityDto(false, "Bạn cần hoàn tất ít nhất 1 buổi khám với nha sĩ này trước khi đánh giá.", null);
        }

        var existing = await dentistReviewRepository.GetByDentistAndPatientAsync(query.DentistId, patient.Id, ct);
        DentistReviewDto? myReview = existing == null
            ? null
            : new DentistReviewDto(existing.Id, patient.FullName, existing.Rating, existing.Comment, existing.Tags, existing.CreatedAt);

        return new ReviewEligibilityDto(true, "Đủ điều kiện đánh giá nha sĩ.", myReview);
    }
}

