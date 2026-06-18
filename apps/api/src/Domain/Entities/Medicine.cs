namespace DentalClinic.API.Domain.Entities;

public class Medicine
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string GenericName { get; private set; } = string.Empty;
    public string Manufacturer { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Medicine() { }

    public static Medicine Create(
        string name,
        string genericName,
        string manufacturer,
        string unit,
        string description)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            GenericName = genericName,
            Manufacturer = manufacturer,
            Unit = unit,
            Description = description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(
        string name,
        string genericName,
        string manufacturer,
        string unit,
        string description)
    {
        Name = name;
        GenericName = genericName;
        Manufacturer = manufacturer;
        Unit = unit;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
