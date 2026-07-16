namespace DentalClinic.API.Domain.Entities;

public class Dentist
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FullName => User?.FullName ?? string.Empty;
    public string Specialization { get; private set; } = string.Empty;
    public int ExperienceYears { get; private set; }
    public string? Biography { get; private set; }
    public string Shift { get; private set; } = "morning"; // "morning" | "afternoon"

    // Navigation properties
    public User User { get; private set; } = null!;
    public ICollection<Appointment> Appointments { get; private set; } = [];

    private Dentist() { }

    public static Dentist Create(Guid userId, string specialization, int experienceYears)
    {
        return new Dentist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Specialization = specialization,
            ExperienceYears = experienceYears
        };
    }
}
