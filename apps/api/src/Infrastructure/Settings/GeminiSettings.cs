namespace DentalClinic.API.Infrastructure.Settings;

public record GeminiSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gemini-3.1-flash-lite";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
