using System;
using System.Threading.Tasks;
using EventHub.Application.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> RegisterUserAsync(UserRegisterDto dto);
        Task<AuthResponseDTO> LoginUserAsync(UserLoginInfo dto);
        Task<AuthResponseDTO> ConfirmCodeAsync(string email, string code);
    }
}
