using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

/// <summary>
/// Chi tiết một đơn nghỉ. Lý do nghỉ là thông tin cá nhân (ốm đau, việc gia đình) nên chỉ chính chủ
/// và người có thẩm quyền duyệt mới được đọc — <paramref name="CanViewAll"/> do tầng Presentation
/// quyết định từ vai trò, handler không tự đọc claim.
/// </summary>
public record GetLeaveRequestByIdQuery(Guid Id, Guid RequesterId, bool CanViewAll) : IRequest<LeaveRequestDto>;

public class GetLeaveRequestByIdHandler(ILeaveRequestRepository leaveRequestRepository) : IRequestHandler<GetLeaveRequestByIdQuery, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(GetLeaveRequestByIdQuery query, CancellationToken ct)
    {
        var request = await leaveRequestRepository.GetByIdAsync(query.Id, ct);

        // Đơn của người khác trả 404 y như đơn không tồn tại: 403 sẽ xác nhận "id này có thật",
        // đủ để dò ra ai đang nghỉ mà không cần đọc được nội dung đơn.
        if (request is null || (!query.CanViewAll && request.UserId != query.RequesterId))
            throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {query.Id}");

        return GetLeaveRequestsHandler.ToDto(request);
    }
}
