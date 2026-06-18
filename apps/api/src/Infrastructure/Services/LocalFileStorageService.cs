using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DentalClinic.API.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadsRoot;

    public LocalFileStorageService(IHostEnvironment env, IConfiguration configuration)
    {
        _uploadsRoot = configuration["FileStorage:UploadsPath"]
            ?? Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_uploadsRoot);

        var ext = Path.GetExtension(fileName);
        var safeFileName = $"{System.Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_uploadsRoot, safeFileName);

        using var target = File.Create(fullPath);
        await content.CopyToAsync(target, ct);

        return $"/uploads/{safeFileName}";
    }
}
