using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record AccountDto(
    Guid Id,
    string Username,
    string? FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record GetAccountsQuery : IRequest<IEnumerable<AccountDto>>;

public class GetAccountsHandler(IUserRepository userRepository) : IRequestHandler<GetAccountsQuery, IEnumerable<AccountDto>>
{
    public async Task<IEnumerable<AccountDto>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        var users = await userRepository.GetAllAsync(ct);
        return users
            .Where(u => u.HasAccount)
            .Select(u => new AccountDto(
                u.Id, u.Username!, u.FullName, u.Email,
                u.PhoneNumber, u.Role, u.IsActive, u.CreatedAt));
    }
}
