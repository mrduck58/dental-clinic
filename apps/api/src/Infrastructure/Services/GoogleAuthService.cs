using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

public class GoogleAuthService(IOptions<GoogleAuthSettings> options) : IGoogleAuthService
{
    private readonly GoogleAuthSettings _settings = options.Value;

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_settings.ClientId],
            });

            return new GoogleUserInfo(payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Token Google không hợp lệ hoặc đã hết hạn.");
        }
    }
}
