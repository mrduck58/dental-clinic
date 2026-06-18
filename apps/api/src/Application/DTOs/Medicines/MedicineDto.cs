namespace DentalClinic.API.Application.DTOs.Medicines;

public record MedicineDto(
    Guid Id,
    string Name,
    string GenericName,
    string Manufacturer,
    string Unit,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateMedicineRequest(
    string Name,
    string GenericName,
    string Manufacturer,
    string Unit,
    string Description);

public record UpdateMedicineRequest(
    string Name,
    string GenericName,
    string Manufacturer,
    string Unit,
    string Description);
