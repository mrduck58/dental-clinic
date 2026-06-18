using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public class ApproveFeedbackHandler(IFeedbackRepository feedbackRepository)
{
    public async Task<FeedbackDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var feedback = await feedbackRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {id}");

        if (feedback.Status == FeedbackStatus.Featured)
            feedback.Unfeature();
        else
            feedback.Feature();

        await feedbackRepository.UpdateAsync(feedback, ct);
        return GetFeedbacksHandler.ToDto(feedback);
    }
}
