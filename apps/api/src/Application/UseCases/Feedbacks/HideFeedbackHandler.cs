using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public record HideFeedbackCommand(Guid Id) : IRequest<FeedbackDto>;

public class HideFeedbackHandler(
    IFeedbackRepository feedbackRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<HideFeedbackCommand, FeedbackDto>
{
    public async Task<FeedbackDto> Handle(HideFeedbackCommand request, CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {request.Id}");

        feedback.Hide();
        await feedbackRepository.UpdateAsync(feedback, cancellationToken);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Reject,
            module: ActivityModule.Feedback,
            description: $"Ẩn phản hồi ID: {request.Id}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
            ct: cancellationToken);

        return GetFeedbacksHandler.ToDto(feedback);
    }
}
