namespace DentalClinic.API.Infrastructure.Settings;

public record GoogleAuthSettings
{
    /// <summary>OAuth 2.0 Client ID (Google Cloud Console) — used as the expected audience when verifying ID tokens.</summary>
    public string ClientId { get; init; } = string.Empty;
}
