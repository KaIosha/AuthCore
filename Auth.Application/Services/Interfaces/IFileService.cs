using Microsoft.AspNetCore.Http;

namespace Auth.Application.Services.Interfaces;

public interface IFileService
{
    Task<string> SaveAsync(
        IFormFile file,
        string[] allowedExtensions,
        string folder,
        long maxSizeBytes,
        CancellationToken cancellationToken = default);


    Task DeleteAsync(string? url, CancellationToken ct = default);
}
