namespace DentalClinic.API.Domain.Entities;

public class StaffMember
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;

    public User User { get; private set; } = null!;

    private StaffMember() { }

    public static StaffMember Create(Guid userId, string fullName) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FullName = fullName,
    };
}
