using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.IdentityModel.Tokens;

namespace DentalClinic.API.Infrastructure.Services;

public class JwtService(JwtSettings settings) : IJwtService
{
    private readonly JwtSettings _settings = settings;

    public string GenerateToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,               user.Role.ToString()),
            new Claim("username",                    user.Username ?? user.Email),
        };

        var token = new JwtSecurityToken(
            issuer:            _settings.Issuer,
            audience:          _settings.Audience,
            claims:            claims,
            expires:           DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
