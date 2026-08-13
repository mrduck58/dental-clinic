namespace DentalClinic.API.Domain.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);
