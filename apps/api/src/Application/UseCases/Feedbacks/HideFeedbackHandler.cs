using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public class HideFeedbackHandler(IFeedbackRepository feedbackRepository)
{
    public async Task<FeedbackDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var feedback = await feedbackRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {id}");

        feedback.Hide();
        await feedbackRepository.UpdateAsync(feedback, ct);
        return GetFeedbacksHandler.ToDto(feedback);
    }
}
