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
