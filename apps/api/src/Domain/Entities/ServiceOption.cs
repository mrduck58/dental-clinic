namespace DentalClinic.API.Domain.Entities;

public class ServiceOption
{
    public Guid Id { get; private set; }
    public Guid ServiceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Unit { get; private set; } = "Răng";
    public int SortOrder { get; private set; }

    // Navigation
    public Service Service { get; private set; } = null!;

    private ServiceOption() { }

    public static ServiceOption Create(Guid serviceId, string name, decimal price, string unit, int sortOrder)
        => new()
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Name = name,
            Price = price,
            Unit = string.IsNullOrWhiteSpace(unit) ? "Răng" : unit.Trim(),
            SortOrder = sortOrder,
        };

    public void Update(string name, decimal price, string unit, int sortOrder)
    {
        Name = name;
        Price = price;
        Unit = string.IsNullOrWhiteSpace(unit) ? "Răng" : unit.Trim();
        SortOrder = sortOrder;
    }
}
