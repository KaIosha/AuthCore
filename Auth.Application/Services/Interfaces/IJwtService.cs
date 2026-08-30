using System;
using System.Threading.Tasks;
using Auth.Application.Dtos.AuthDtos;
using Auth.Domain.Entities;

namespace Auth.Application.Services.Interfaces
{
    public interface IJwtService
    {
        // Creates a new access token + stores a new refresh token in the DB
        Task<AuthResponseDto> CreateJwtTokenAsync(ApplicationUser user);

        // Validates + rotates a refresh token; returns null when the token is invalid
        //Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
    }
}
