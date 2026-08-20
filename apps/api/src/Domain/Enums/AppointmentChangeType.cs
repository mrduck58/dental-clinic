namespace DentalClinic.API.Domain.Enums;

public enum AppointmentChangeType
{
    Cancel = 1,
    Reschedule = 2
}

public enum AppointmentChangeRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
