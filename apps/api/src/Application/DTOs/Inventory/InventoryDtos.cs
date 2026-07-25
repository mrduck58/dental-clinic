namespace DentalClinic.API.Application.DTOs.Inventory;

public record SupplyItemDto(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string Unit,
    int Quantity,
    int MinQuantity,
    bool IsLow,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record SupplyTransactionDto(
    Guid Id,
    Guid SupplyItemId,
    string ItemName,
    string Type,
    int Quantity,
    string? Note,
    string CreatedBy,
    DateTimeOffset CreatedAt);

public record CreateSupplyTransactionRequest(
    Guid SupplyItemId,
    string Type,
    int Quantity,
    string? Note);

public record CreateSupplyItemRequest(
    string Code,
    string Name,
    string Category,
    string Unit,
    int Quantity,
    int MinQuantity);

public record StockImportRequest(
    string Name,
    string Unit,
    string Category,
    int Quantity,
    string? Note);
