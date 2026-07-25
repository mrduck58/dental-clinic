namespace DentalClinic.API.Domain.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string UserName { get; }
    string UserRole { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
