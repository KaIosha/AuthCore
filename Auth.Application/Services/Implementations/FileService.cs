using Auth.Application.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Auth.Application.Services.Implementations;

// One generic file-storage service for every file type in the app
// (profile photos, org logos, verification PDFs, ...).
// The CALLER decides what is allowed and where it goes, via the parameters.
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveAsync(
        IFormFile file,
        string[] allowedExtensions,
        string folder,
        long maxSizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            throw new InvalidOperationException("No file was uploaded.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}.");
        }

        if (file.Length > maxSizeBytes)
        {
            throw new InvalidOperationException("File is too large.");
        }

       
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // WebRootPath is the wwwroot folder - robust regardless of the launch working directory
        var uploadsFolder = Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            folder);

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // URL path stored in the DB - served by UseStaticFiles
        return $"/{folder}/{uniqueFileName}";
    }

    public Task DeleteAsync(string? url, CancellationToken ct = default)
    {
        if(string.IsNullOrEmpty(url)) return Task.CompletedTask;

        // Clean path and map back to physical file system
        string cleanPath = url.TrimStart('/');
        string fullPath = Path.Combine( _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), cleanPath);
       
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}
