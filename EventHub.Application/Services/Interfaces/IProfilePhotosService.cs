using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace EventHub.Application.Services.Interfaces
{
    public interface IProfilePhotosService
    {
        Task<string> UploadProfilePhotoAsync(IFormFile? photo);
    }
}
