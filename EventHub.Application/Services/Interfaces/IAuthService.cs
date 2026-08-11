using System;
using System.Threading.Tasks;
using EventHub.Application.Dtos.AuthDtos;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterUserAsync(UserRegisterDto dto);
        Task<AuthResponseDto> LoginUserAsync(LoginDto dto);
        Task<AuthResponseDto> ConfirmCodeAsync(string email, string code);
        Task<AuthResponseDto> ResendConfirmationCodeAsync(string email);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto);
        Task<AuthResponseDto> LogoutAsync(string refreshToken);
        Task<AuthResponseDto> RegisterOrganizationAsync(OrganizationRegisterDto dto);
    }
}
