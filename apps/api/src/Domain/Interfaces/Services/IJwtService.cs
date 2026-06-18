using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
