namespace DentalClinic.API.Domain.Exceptions;

public sealed class ConflictException(string message) : Exception(message);
