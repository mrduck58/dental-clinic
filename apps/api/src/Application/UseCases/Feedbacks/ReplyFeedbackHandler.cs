using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public record ReplyFeedbackCommand(Guid Id, string ReplyText) : IRequest<FeedbackDto>;

public class ReplyFeedbackHandler(IFeedbackRepository feedbackRepository)
    : IRequestHandler<ReplyFeedbackCommand, FeedbackDto>
{
    public async Task<FeedbackDto> Handle(ReplyFeedbackCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReplyText))
            throw new ValidationException("Nội dung phản hồi không được để trống.");

        var feedback = await feedbackRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {request.Id}");

        feedback.Reply(request.ReplyText);
        await feedbackRepository.UpdateAsync(feedback, cancellationToken);
        return GetFeedbacksHandler.ToDto(feedback);
    }
}
