using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Room
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Floor { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public RoomStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? ClinicInfoId { get; private set; }
    public ClinicInfo? ClinicInfo { get; private set; }

    private Room() { }

    public static Room Create(
        string code,
        string name,
        string floor,
        string type,
        string description)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            Name = name,
            Floor = floor,
            Type = type,
            Status = RoomStatus.Trong,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(string code, string name, string floor, string type, string description)
    {
        Code = code.ToUpperInvariant();
        Name = name;
        Floor = floor;
        Type = type;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeStatus(RoomStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
