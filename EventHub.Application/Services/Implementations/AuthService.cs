using System;
using System.Linq;
using System.Threading.Tasks;
using EventHub.Application.DTOs.AuthDTOs;
using EventHub.Application.Helper;
using EventHub.Application.Interfaces;
using EventHub.Application.Services.Interfaces;
using EventHub.Domain.Constants;
using EventHub.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IValidator<UserRegisterDto> _userValidator;
        private readonly IEmailService _emailService;
        private readonly IProfilePhotosService _profilePhotosService;

        public AuthService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            IValidator<UserRegisterDto> userValidator,
            IEmailService emailService,
            IProfilePhotosService profilePhotosService)

        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _jwtService = jwtService;
            _userValidator = userValidator;
            _emailService = emailService;
            _profilePhotosService = profilePhotosService;
        }
       
        public async Task<AuthResponseDTO> RegisterUserAsync(UserRegisterDto dto)
        {
            // Step 1: Validate the DTO (FluentValidation rules from UserRegisterValidation)
            var validationResult = await _userValidator.ValidateAsync(dto);
            if (!validationResult.IsValid) 
            {
                return new AuthResponseDTO
                {
                    IsAuthenticated = false,
                    Message = "Validation failed: " + string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
                };
            }
            // Step 2: Make sure the email is not already registered
            var exsitingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (exsitingUser is not null)
            { 
                return new AuthResponseDTO
                {
                    IsAuthenticated = false,
                    Message = "Email is already registered."
                };
            }
            var userPhotoPath = await _profilePhotosService.UploadProfilePhotoAsync(dto.ProfilePhoto);
            // Step 3: Map the DTO into a new ApplicationUser
            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Username,
                Email = dto.Email,
                EmailConfirmed = false,
                ProfilePhoto = userPhotoPath
            };
            // Step 4: Create the user in Identity (hashes the password + saves it)
            var NewUser = await _userManager.CreateAsync(user, dto.Password);

            // Step 5: If creation failed, return the Identity errors to the client
            if (!NewUser.Succeeded)
            {
                return new AuthResponseDTO
                {
                    IsAuthenticated = false,
                   Message = $"User creation failed:{string.Join(',',NewUser.Errors.Select(e => e.Description)) }"
                };
            }
            // Step 6: Assign the default "User" role to the new account
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
            if (!roleResult.Succeeded) 
            {
                return new AuthResponseDTO
                {
                    IsAuthenticated = false,
                    Message = $"Failed to assign role: {string.Join(',', roleResult.Errors.Select(e => e.Description))}"
                };
            }
            // Step 7: Generate a 6-digit confirmation code and save it on the user
            var confirmationCode = Random.Shared.Next(100000, 1000000).ToString();
            user.EmailConfirmationCode = confirmationCode;
            user.EmailConfirmationCodeExpiresAt = DateTime.UtcNow.AddMinutes(10); // code is valid for 10 minutes
            // Step 8: Persist the code + expiry on the user row
            await _userManager.UpdateAsync(user);

            // Step 9: EMAIL the code to the user's real Gmail (NOT returned in the response)
            var htmlBody = $"""
              <h2>Welcome to EventHub, {user.FirstName}!</h2>
              <p>Your email confirmation code is:</p>
              <h1 style="letter-spacing: 5px;">{confirmationCode}</h1>
              <p>Enter it in the app to confirm your email. It expires in 15 minutes.</p>
              """;
            await _emailService.SendAsync(user.Email, "EventHub - Your confirmation code", htmlBody);
            // Step 10: Return the response WITHOUT the code (it only lives in the email now)
            return new AuthResponseDTO
            {
                IsAuthenticated = false,
                Message = "User registered successfully. Please check your email for the confirmation code.",
                UserName = user.UserName,
                Email = user.Email
            };
        }
        public async Task<AuthResponseDTO> ConfirmCodeAsync(string email, string code)
        {
            // Step 1: Find the user by email
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return new AuthResponseDTO
                {
                    IsAuthenticated = false,
                    Message = "Invalid email or confirmation code."
                };
            }
            // Step 2: If there is no code saved or it has expired, reject it
            if (user.EmailConfirmationCode is null || user.EmailConfirmationCodeExpiresAt is null || user.EmailConfirmationCodeExpiresAt < DateTime.UtcNow)
            {
                return new AuthResponseDTO
                {
                    IsAuthenticated = false,
                    Message = "Confirmation code has expired or is invalid."
                };
            }
            // Step 3: Compare the entered code with the stored one (exact match)
            if (user.EmailConfirmationCode != code) 
            {
                return new AuthResponseDTO
                {
                    IsAuthenticated = false,
                    Message = "Invalid confirmation code."
                };
            }
            // Step 4: Code is correct -> confirm the email, clear the code + expiry
            user.EmailConfirmed = true;
            user.EmailConfirmationCode = null;
            user.EmailConfirmationCodeExpiresAt = null;
            await _userManager.UpdateAsync(user);
            // Step 5: Continue the flow: load the user's role
            var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            // Step 6: Issue the JWT + refresh token (user is now logged in)
            var jwtToken = await _jwtService.CreateJwtTokenAsync(user);
            return new AuthResponseDTO
            {
                IsAuthenticated = true,
                Message = "Email confirmed successfully.",
                UserName = user.UserName,
                Email = user.Email,
                Role = userRole,
                Token = jwtToken.Token,
                RefreshToken = jwtToken.RefreshToken,
                ExpireAt = jwtToken.ExpireAt
            };
        }

        // login service
        public async

    }
}
