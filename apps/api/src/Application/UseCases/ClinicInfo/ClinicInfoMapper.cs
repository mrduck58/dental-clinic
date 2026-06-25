using System.Text.Json;
using DentalClinic.API.Application.DTOs.ClinicInfo;
using Entity = DentalClinic.API.Domain.Entities.ClinicInfo;

namespace DentalClinic.API.Application.UseCases.ClinicInfo;

/// <summary>
/// (De)serialize các danh sách JSON của entity ClinicInfo và ánh xạ sang DTO.
/// Tập trung ở một nơi để Get/Update handler dùng chung.
/// </summary>
internal static class ClinicInfoMapper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize<T>(IEnumerable<T>? items)
        => JsonSerializer.Serialize(items ?? [], Options);

    private static List<T> Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static ClinicInfoDto ToDto(Entity e) => new(
        e.Id,
        e.AboutTitle,
        e.AboutDescription,
        e.FoundedYear,
        e.AboutImageUrl,
        e.Phone,
        e.Email,
        e.Address,
        Deserialize<MilestoneDto>(e.MilestonesJson),
        Deserialize<string>(e.CertificationsJson),
        Deserialize<FeatureDto>(e.FeaturesJson),
        Deserialize<TreatmentStepDto>(e.TreatmentStepsJson),
        Deserialize<StatisticDto>(e.StatisticsJson),
        e.CreatedAt,
        e.UpdatedAt);
}
