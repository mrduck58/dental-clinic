using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public record CreateFeedbackCommand(string CustomerName, int Rating, string Comment) : IRequest<FeedbackDto>;

public class CreateFeedbackHandler(IFeedbackRepository feedbackRepository)
    : IRequestHandler<CreateFeedbackCommand, FeedbackDto>
{
    public async Task<FeedbackDto> Handle(CreateFeedbackCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ValidationException("Đánh giá phải từ 1 đến 5 sao.");

        var feedback = Feedback.Create(
            request.CustomerName,
            request.Rating,
            request.Comment);

        await feedbackRepository.AddAsync(feedback, cancellationToken);
        return GetFeedbacksHandler.ToDto(feedback);
    }
}

public record ClinicFeedbackEligibilityDto(bool CanReview, string Reason, bool HasCompletedFirstVisit);

public record GetClinicFeedbackEligibilityQuery(Guid UserId) : IRequest<ClinicFeedbackEligibilityDto>;

public class GetClinicFeedbackEligibilityHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository)
    : IRequestHandler<GetClinicFeedbackEligibilityQuery, ClinicFeedbackEligibilityDto>
{
    public async Task<ClinicFeedbackEligibilityDto> Handle(GetClinicFeedbackEligibilityQuery query, CancellationToken ct)
    {
        var patient = await patientRepository.GetByUserIdAsync(query.UserId, ct);
        if (patient == null)
        {
            return new ClinicFeedbackEligibilityDto(false, "Không tìm thấy hồ sơ bệnh nhân.", false);
        }

        var totalVisits = await appointmentRepository.CountOverallCompletedVisitsAsync(patient.Id, ct);
        if (totalVisits == 0)
        {
            return new ClinicFeedbackEligibilityDto(false, "Bạn cần hoàn thành lần khám hoặc điều trị đầu tiên tại phòng khám để gửi đánh giá.", false);
        }

        return new ClinicFeedbackEligibilityDto(true, "Đủ điều kiện gửi đánh giá phòng khám.", true);
    }
}
