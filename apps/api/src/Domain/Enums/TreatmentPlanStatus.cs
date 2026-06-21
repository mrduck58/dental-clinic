using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Enums;

public enum TreatmentPlanStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3
}

public enum TreatmentStepStatus
{
    Pending = 1,
    Completed = 2
}
