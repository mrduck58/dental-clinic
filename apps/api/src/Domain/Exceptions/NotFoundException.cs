namespace DentalClinic.API.Domain.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
