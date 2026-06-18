namespace DentalClinic.API.Domain.Interfaces.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
}
