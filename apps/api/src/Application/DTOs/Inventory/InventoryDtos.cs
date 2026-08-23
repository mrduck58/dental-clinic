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
    string OrderType,
    decimal? Price,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record SupplyTransactionDto(
    Guid Id,
    Guid SupplyItemId,
    string ItemName,
    string Type,
    int Quantity,
    decimal? UnitPrice,
    string? Note,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string? RoomName = null);

public record CreateSupplyTransactionRequest(
    Guid SupplyItemId,
    string Type,
    int Quantity,
    string? Note,
    Guid? RoomId = null);

public record CreateSupplyItemRequest(
    string Code,
    string Name,
    string Category,
    string Unit,
    int Quantity,
    int MinQuantity,
    decimal? Price = null);

public record UpdateSupplyItemRequest(
    string Name,
    string Category,
    string Unit,
    int MinQuantity,
    decimal? Price);

public record StockImportRequest(
    string Name,
    string Unit,
    string Category,
    int Quantity,
    string? Note,
    decimal? UnitPrice = null);
