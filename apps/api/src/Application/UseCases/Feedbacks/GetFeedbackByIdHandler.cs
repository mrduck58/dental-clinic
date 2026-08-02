using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public record GetFeedbackByIdQuery(Guid Id) : IRequest<FeedbackDto>;

public class GetFeedbackByIdHandler(IFeedbackRepository feedbackRepository)
    : IRequestHandler<GetFeedbackByIdQuery, FeedbackDto>
{
    public async Task<FeedbackDto> Handle(GetFeedbackByIdQuery request, CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {request.Id}");

        return GetFeedbacksHandler.ToDto(feedback);
    }
}
