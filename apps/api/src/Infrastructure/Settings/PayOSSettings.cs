namespace DentalClinic.API.Infrastructure.Settings;

public record PayOSSettings
{
    public string ClientId { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ChecksumKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api-merchant.payos.vn";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ChecksumKey);
}
