using System;
using System.Collections.Generic;
using System.Text;
using EventHub.Application.Services.Interfaces;
using EventHub.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventHub.Application.Services.Implementations
{
    public class ProfilePhotosService : IProfilePhotosService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfilePhotosService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
       
        }
        public async Task<string> UploadProfilePhotoAsync(IFormFile? photo)
        {

            if (photo == null || photo.Length <= 0)
            {
              return null;
            }

            // Validate allowed extensions
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(photo.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return await Task.FromResult<string>(null!); // Invalid file type


            // Create a unique filename to avoid overwriting existing files
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // Define the physical server path to save the file (wwwroot/uploads)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UsersPhotos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder); // Create folder if missing

            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the binary stream to the local file system
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
                stream.Close();
            }

            // Path format to store in DB (accessible via URL over HTTP)
            return await Task.FromResult<string>($"/uploads/{uniqueFileName}");

        }
    }
}
