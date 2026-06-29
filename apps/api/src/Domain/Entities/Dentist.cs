namespace DentalClinic.API.Domain.Entities;

public class Dentist
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Specialization { get; private set; } = string.Empty;
    public int ExperienceYears { get; private set; }
    public string? Biography { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public ICollection<Appointment> Appointments { get; private set; } = [];

    private Dentist() { }

    public static Dentist Create(Guid userId, string fullName, string specialization, int experienceYears)
    {
        return new Dentist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = fullName,
            Specialization = specialization,
            ExperienceYears = experienceYears
        };
    }
}
