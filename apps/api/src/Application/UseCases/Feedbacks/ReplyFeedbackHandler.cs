using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public class ReplyFeedbackHandler(IFeedbackRepository feedbackRepository)
{
    public async Task<FeedbackDto> HandleAsync(Guid id, ReplyFeedbackRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReplyText))
            throw new ValidationException("Nội dung phản hồi không được để trống.");

        var feedback = await feedbackRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {id}");

        feedback.Reply(request.ReplyText);
        await feedbackRepository.UpdateAsync(feedback, ct);
        return GetFeedbacksHandler.ToDto(feedback);
    }
}
