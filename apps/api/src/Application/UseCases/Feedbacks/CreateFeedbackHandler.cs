using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public class CreateFeedbackHandler(IFeedbackRepository feedbackRepository)
{
    public async Task<FeedbackDto> HandleAsync(CreateFeedbackRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ValidationException("Đánh giá phải từ 1 đến 5 sao.");

        var feedback = Feedback.Create(
            request.CustomerName,
            request.Rating,
            request.Comment);

        await feedbackRepository.AddAsync(feedback, ct);
        return GetFeedbacksHandler.ToDto(feedback);
    }
}
