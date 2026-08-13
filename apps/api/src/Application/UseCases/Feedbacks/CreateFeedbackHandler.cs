using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public record CreateFeedbackCommand(Guid UserId, int Rating, string Comment) : IRequest<FeedbackDto>;

public class CreateFeedbackHandler(
    IFeedbackRepository feedbackRepository,
    IPatientRepository patientRepository,
    INotificationService notificationService,
    IUserRepository userRepository)
    : IRequestHandler<CreateFeedbackCommand, FeedbackDto>
{
    public async Task<FeedbackDto> Handle(CreateFeedbackCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ValidationException("Đánh giá phải từ 1 đến 5 sao.");

        var patient = await patientRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ bệnh nhân.");

        var feedback = Feedback.Create(
            patient.FullName,
            request.Rating,
            request.Comment,
            patient.Id);

        await feedbackRepository.AddAsync(feedback, cancellationToken);

        // Owner theo dõi đánh giá phòng khám ở /owner/feedback — báo cho họ mỗi khi có phản hồi mới.
        var ownerIds = await userRepository.GetUserIdsByRoleAsync("Owner", cancellationToken);
        var ownerTemplate = new CreateNotificationRequest(
            UserId: Guid.Empty,
            Type: NotificationType.Service,
            Priority: NotificationPriority.Medium,
            Title: "Phản hồi mới từ bệnh nhân",
            Body: $"{patient.FullName} vừa gửi đánh giá {request.Rating}/5 sao cho phòng khám.",
            RelatedEntityType: "Feedback",
            RelatedEntityId: feedback.Id.ToString());
        await notificationService.CreateForMultipleUsersAsync(ownerIds, ownerTemplate, cancellationToken);

        return GetFeedbacksHandler.ToDto(feedback);
    }
}

public record ClinicFeedbackEligibilityDto(
    bool CanReview,
    string Reason,
    bool HasCompletedFirstVisit,
    FeedbackDto? MyFeedback = null);

public record GetClinicFeedbackEligibilityQuery(Guid UserId) : IRequest<ClinicFeedbackEligibilityDto>;

public class GetClinicFeedbackEligibilityHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IFeedbackRepository feedbackRepository)
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

        var existing = await feedbackRepository.GetByPatientIdAsync(patient.Id, ct);
        if (existing != null)
        {
            var myFeedbackDto = GetFeedbacksHandler.ToDto(existing);

            return new ClinicFeedbackEligibilityDto(
                false,
                "Bạn đã gửi đánh giá cho phòng khám trước đó và không thể gửi thêm.",
                true,
                myFeedbackDto);
        }

        return new ClinicFeedbackEligibilityDto(true, "Đủ điều kiện gửi đánh giá phòng khám.", true, null);
    }
}
