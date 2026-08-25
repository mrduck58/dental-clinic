using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

public class GoogleAuthService(IOptions<GoogleAuthSettings> options) : IGoogleAuthService
{
    private const string DefaultClientId = "170456318656-4ovvd8apq0a3pd9cglhetctojc4873sb.apps.googleusercontent.com";
    private readonly GoogleAuthSettings _settings = options.Value;

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var clientId = !string.IsNullOrWhiteSpace(_settings.ClientId) ? _settings.ClientId : DefaultClientId;
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            return new GoogleUserInfo(payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            // Fallback: Nếu audience giữa web/app lệch nhau, thử validate signature của Google mà không ép Audience
            try
            {
                var fallbackPayload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                return new GoogleUserInfo(fallbackPayload.Email, fallbackPayload.Name, fallbackPayload.Picture);
            }
            catch
            {
                throw new UnauthorizedAccessException("Token Google không hợp lệ hoặc đã hết hạn.");
            }
        }
    }
}
