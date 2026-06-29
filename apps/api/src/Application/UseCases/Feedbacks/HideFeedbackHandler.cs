using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public class HideFeedbackHandler(
    IFeedbackRepository feedbackRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<FeedbackDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var feedback = await feedbackRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {id}");

        feedback.Hide();
        await feedbackRepository.UpdateAsync(feedback, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Reject,
            module: ActivityModule.Feedback,
            description: $"Ẩn phản hồi ID: {id}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return GetFeedbacksHandler.ToDto(feedback);
    }
}
