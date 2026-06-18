using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public class GetFeedbackByIdHandler(IFeedbackRepository feedbackRepository)
{
    public async Task<FeedbackDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var feedback = await feedbackRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {id}");

        return GetFeedbacksHandler.ToDto(feedback);
    }
}
