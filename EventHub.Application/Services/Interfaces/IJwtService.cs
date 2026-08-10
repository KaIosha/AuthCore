using System;
using System.Threading.Tasks;
using EventHub.Application.DTOs.AuthDTOs;
using EventHub.Domain.Entities;

namespace EventHub.Application.Services.Interfaces
{
    public interface IJwtService
    {
        // Creates a new access token + stores a new refresh token in the DB
        Task<AuthResponseDTO> CreateJwtTokenAsync(ApplicationUser user);

        // Validates + rotates a refresh token; returns null when the token is invalid
        Task<AuthResponseDTO?> RefreshTokenAsync(string refreshToken);
    }
}
