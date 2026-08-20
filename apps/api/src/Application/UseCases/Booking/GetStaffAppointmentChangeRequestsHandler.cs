using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public class GetStaffAppointmentChangeRequestsHandler(
    IAppointmentChangeRequestRepository changeRequestRepository) : IRequestHandler<GetStaffAppointmentChangeRequestsQuery, IReadOnlyList<AppointmentChangeRequestDto>>
{
    public async Task<IReadOnlyList<AppointmentChangeRequestDto>> Handle(GetStaffAppointmentChangeRequestsQuery query, CancellationToken ct)
    {
        var requests = await changeRequestRepository.GetStaffChangeRequestsAsync(
            status: query.Status,
            date: query.Date,
            ct: ct);

        return requests.Select(AppointmentChangeRequestDto.FromEntity).ToList();
    }
}
