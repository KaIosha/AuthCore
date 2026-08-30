using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Auth.Application.Dtos.AuthDtos;
using Auth.Application.Helper;
using Auth.Application.Interfaces;
using Auth.Application.Services.Implementations;
using Auth.Application.Services.Interfaces;
using Auth.Domain.Constants;
using Auth.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;


namespace Auth.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IValidator<UserRegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<OrganizationRegisterDto> _organizationValidator;
        private readonly IValidator<ForgetPasswordDto> _forgetPasswordValidator;
        private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
        private readonly IEmailService _emailService;
        private readonly IFileService _fileService;
        private readonly TokenValidationParameters _tokenValidationParameters;


        public AuthService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IJwtService jwtService,
            IValidator<UserRegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator,
            IEmailService emailService,
            IFileService fileService,
            IValidator<OrganizationRegisterDto> organizationValidator,
            IValidator<ForgetPasswordDto> forgetPasswordValidator,
            IValidator<ResetPasswordDto> resetPasswordValidator,
            TokenValidationParameters tokenValidationParameters)

        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _jwtService = jwtService;
            _registerValidator = registerValidator;
            _emailService = emailService;
            _fileService = fileService;
            _loginValidator = loginValidator;
            _organizationValidator = organizationValidator;
            _tokenValidationParameters = tokenValidationParameters;
            _forgetPasswordValidator = forgetPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
        }

        public async Task<AuthResponseDto> RegisterUserAsync(UserRegisterDto dto)
        {
            // Step 1: Validate the DTO (FluentValidation rules from UserRegisterValidation)
            var validationResult = await _registerValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Validation failed: " + string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
                };
            }
            // Step 2: Make sure the email is not already registered
            var exsitingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (exsitingUser is not null)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Email is already registered."
                };
            }

            // Step 3: Upload the profile photo FIRST, before anything touches the DB.
            //         If the upload fails, nothing was created yet - no orphan users.
            string? profilePhotoUrl = null;
            try
            {
                if (dto.ProfilePhoto is not null)
                {
                    profilePhotoUrl = await _fileService.SaveAsync(
                        dto.ProfilePhoto,
                        [".jpg", ".jpeg", ".png"],
                        "UsersPhotos",
                        5 * 1024 * 1024);
                }
            }
            catch (InvalidOperationException ex)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = $"Profile photo upload failed: {ex.Message}"
                };
            }

            // Step 4: Map the DTO into a new ApplicationUser (photo already set)
            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Username,
                Email = dto.Email,
                EmailConfirmed = false,
                ProfilePhoto = profilePhotoUrl,
            };
            // Step 5: Create the user in Identity (hashes the password + saves it)
            var NewUser = await _userManager.CreateAsync(user, dto.Password);

            // Step 6: If creation failed, return the Identity errors to the client
            if (!NewUser.Succeeded)
            {
                await DeleteUploadedFilesAsync(profilePhotoUrl, null, null);
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = $"User creation failed:{string.Join(',', NewUser.Errors.Select(e => e.Description))}"
                };
            }
            // Step 7: Assign the default "User" role to the new account
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
            if (!roleResult.Succeeded)
            {
                await DeleteUploadedFilesAsync(profilePhotoUrl, null, null);
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = $"Failed to assign role: {string.Join(',', roleResult.Errors.Select(e => e.Description))}"
                };
            }

            await GenerateAndSendCodeAsync(user);

            // Step 10: Return the response WITHOUT the code (it only lives in the email now)
            return new AuthResponseDto
            {
                IsSuccess = true,
                IsAuthenticated = false,
                Message = "User registered successfully. Please check your email for the confirmation code.",
                UserName = user.UserName,
                Email = user.Email
            };
        }
        public async Task<AuthResponseDto> ConfirmCodeAsync(string email, string code)
        {

            // Step 1: Find the user by email
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Invalid email or confirmation code."
                };
            }


            // Step 2: Reject if already locked out (too many failed attempts)
            if (user.EmailConfirmationCodeAttempts >= 5)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Too many failed confirmation attempts. Please request a new code."
                };
            }

            // Step 3: Wrong or expired code -> count the failed attempt, kill the code at 5
            if (user.EmailConfirmationCode != code || user.EmailConfirmationCodeExpiresAt is null || user.EmailConfirmationCodeExpiresAt < DateTime.UtcNow)
            {
                user.EmailConfirmationCodeAttempts++;

                if (user.EmailConfirmationCodeAttempts >= 5)
                {
                    user.EmailConfirmationCode = null;
                    user.EmailConfirmationCodeExpiresAt = null;
                }
                await _userManager.UpdateAsync(user);
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Confirmation code has expired or is invalid."
                };
            }


            // Step 4: Code is correct -> confirm the email, clear the code + expiry
            user.EmailConfirmed = true;
            user.EmailConfirmationCodeAttempts = 0;
            user.EmailConfirmationCode = null;
            user.EmailConfirmationCodeExpiresAt = null;
            await _userManager.UpdateAsync(user);
            // Step 5: Continue the flow: load the user's role
            var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            // Step 6: Issue the JWT + refresh token (user is now logged in)
            var jwtToken = await _jwtService.CreateJwtTokenAsync(user);
            return new AuthResponseDto
            {
                IsSuccess = true,
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
        public async Task<AuthResponseDto> ResendConfirmationCodeAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || user.EmailConfirmed)
            {
                return new AuthResponseDto
                {
                    IsSuccess = true,
                    IsAuthenticated = false,
                    Message = "If this email is already confirmed, please log in."
                };
            }

            await GenerateAndSendCodeAsync(user);
            return new AuthResponseDto
            {
                IsSuccess = true,
                IsAuthenticated = false,
                Message = "A new confirmation code has been sent to your email.",
                UserName = user.UserName,
                Email = user.Email
            };
        }
        public async Task<AuthResponseDto> LoginUserAsync(LoginDto dto)
        {
            var validationResult = await _loginValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Invalid login credentials."
                };
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Invalid email or password."
                };
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Invalid email or password."
                };
            }

            if (!user.EmailConfirmed)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Please confirm your email before logging in."
                };
            }



            var tokenResult = await _jwtService.CreateJwtTokenAsync(user);
            return new AuthResponseDto
            {
                IsSuccess = true,
                IsAuthenticated = true,
                Message = "Login successful.",
                Token = tokenResult.Token,
                RefreshToken = tokenResult.RefreshToken,
                ExpireAt = tokenResult.ExpireAt,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };

        }
        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto payload)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

            // Step 1: Look up the refresh token in the DB FIRST â€” the DB is the source of truth,
            //         not the access token's expiry state.
            var dbRefreshToken = await _unitOfWork.Repository<RefreshToken>()
                .FirstOrDefaultAsync(x => x.Token == payload.RefreshToken);

            if (dbRefreshToken is null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Invalid refresh token."
                };
            }

            // Step 2: The refresh token must be single-use, not revoked, and not expired.
            if (dbRefreshToken.IsUsed || dbRefreshToken.IsRevoked)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Refresh token is no longer valid. Please sign in again."
                };
            }

            if (dbRefreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Refresh token has expired. Please sign in again."
                };
            }

            // Step 3: Validate the JWT WITHOUT the lifetime check â€” an expired access token
            //         is the expected input here, so it must not throw.
            var validationParametersWithoutLifetime = _tokenValidationParameters.Clone();
            validationParametersWithoutLifetime.ValidateLifetime = false;

            ClaimsPrincipal tokenInVerification;
            try
            {
                tokenInVerification = jwtSecurityTokenHandler.ValidateToken(
                    payload.Token, validationParametersWithoutLifetime, out var validatedToken);

                // Step 4: Enforce the signing algorithm.
                if (validatedToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        IsAuthenticated = false,
                        Message = "Invalid token."
                    };
                }
            }
            catch (SecurityTokenException)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Invalid token."
                };
            }

            // Step 5: The access token must actually be expired â€” that's the only reason to refresh.
            var expClaim = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value;
            if (expClaim is null || !long.TryParse(expClaim, out var utcExpiryDate))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Invalid token."
                };
            }

            if (UnixTimeStampToDateTime(utcExpiryDate) > DateTime.UtcNow)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Token has not expired yet."
                };
            }

            // Step 6: The refresh token must belong to THIS access token (jti match).
            var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti) || dbRefreshToken.JwtId != jti)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Refresh token does not match the access token."
                };
            }

            // Step 7: Load the user this token belongs to.
            var user = await _userManager.FindByIdAsync(dbRefreshToken.UserId.ToString());
            if (user is null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "User no longer exists."
                };
            }

            // Step 8: Rotation â€” consume the old token so it can never be replayed.
            dbRefreshToken.IsUsed = true;
            dbRefreshToken.IsRevoked = true;
            await _unitOfWork.SaveChangesAsync();

            // Step 9: Issue a brand new token pair.
            var newTokenResponse = await _jwtService.CreateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                IsAuthenticated = true,
                Message = "Token refreshed successfully.",
                Token = newTokenResponse.Token,
                RefreshToken = newTokenResponse.RefreshToken,
                ExpireAt = newTokenResponse.ExpireAt
            };
        }
        public async Task<AuthResponseDto> LogoutAsync(string refreshToken)
        {
            var dbRefreshToken = await _unitOfWork.Repository<RefreshToken>().FirstOrDefaultAsync(x => x.Token == refreshToken);
            if (dbRefreshToken is null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    IsAuthenticated = false,
                    Message = "Refresh token doesn't exist in our DB."
                };
            }
            // Revoke the refresh token
            dbRefreshToken.IsRevoked = true;
            dbRefreshToken.ExpiresAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return new AuthResponseDto
            {
                IsSuccess = true,
                IsAuthenticated = false,
                Message = "Logged out successfully."
            };
        }
        public async Task<AuthResponseDto> RegisterOrganizationAsync(OrganizationRegisterDto dto)
        {
            // STEP 1 - DONE: Validate the DTO (rules from OrganizationRegisterValidation)
            var validationResult = await _organizationValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return new AuthResponseDto
                {
                    Message = "Validation failed: " + string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
                };
            }

            // STEP 2 - DONE: Owner email must not already be registered
            var existingEmal = await _userManager.FindByEmailAsync(dto.OrganizationOwnerEmail);
            if (existingEmal is not null)
            {
                return new AuthResponseDto
                {
                    Message = "Email is already registered."
                };
            }

            // STEP 3: Organization name must not be taken (case-insensitive) -
            var existingOrgName = await _unitOfWork.Repository<Organization>().AnyAsync(n => n.Name.Trim().ToLower() == dto.OrganizationName.Trim().ToLower());
            if (existingOrgName)
            {
                return new AuthResponseDto
                {
                    Message = $"{dto.OrganizationName} is already registered."
                };
            }
            // STEP 4-6: Upload the owner photo, logo, and verification PDF (IFileService).

            string? ownerPhotoUrl = null;
            string? organizationLogoUrl = null;
            string? verificationDocumentUrl = null;
            try
            {
                if (dto.ProfilePhoto is not null)
                {
                    ownerPhotoUrl = await _fileService.SaveAsync(dto.ProfilePhoto, [".jpg", ".jpeg", ".png"], "UsersPhotos", 5 * 1024 * 1024);
                }

                if (dto.Logo is not null)
                {
                    organizationLogoUrl = await _fileService.SaveAsync(dto.Logo, [".jpg", ".jpeg", ".png"], "OrganizationLogos", 5 * 1024 * 1024);
                }

                verificationDocumentUrl = await _fileService.SaveAsync(dto.VerificationDocument, [".pdf"], "OrganizationVerificationDocs", 10 * 1024 * 1024);
            }
            catch (InvalidOperationException ex)
            {
                await DeleteUploadedFilesAsync(ownerPhotoUrl, organizationLogoUrl, verificationDocumentUrl);
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = $"File upload failed: {ex.Message}"
                };
            }

            // STEP 7: Open the transaction -
            ApplicationUser? user = null;
            try
            {
                await using var tx = await _unitOfWork.BeginTransactionAsync();

                // STEP 8: Inside the tx - build the ApplicationUser with ProfilePhoto ALREADY set,
                user = new ApplicationUser
                {
                    FirstName = dto.OrganizationOwnerFirstName,
                    LastName = dto.OrganizationOwnerLastName,
                    Email = dto.OrganizationOwnerEmail,
                    UserName = dto.OrganizationOwnerUsername,
                    ProfilePhoto = ownerPhotoUrl,
                };
                var NewUser = await _userManager.CreateAsync(user, dto.Password);
                if (!NewUser.Succeeded)
                {
                    // delete the uploaded files before returning
                    await DeleteUploadedFilesAsync(ownerPhotoUrl, organizationLogoUrl, verificationDocumentUrl);
                    return new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = $"Creation failed:{string.Join(',', NewUser.Errors.Select(e => e.Description))}"
                    };
                }
                // STEP 9: Inside the tx - AddToRoleAsync(user, Roles.OrganizationAdmin). Check the result.
                var roleResult = await _userManager.AddToRoleAsync(user, Roles.OrganizationAdmin);
                if (!roleResult.Succeeded)
                {
                    // **COMPENSATION: DB write failed, delete the uploaded files before returning
                    await DeleteUploadedFilesAsync(ownerPhotoUrl, organizationLogoUrl, verificationDocumentUrl);
                    return new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = $"Failed to assign role: {string.Join(',', roleResult.Errors.Select(e => e.Description))}"
                    };
                }
                // STEP 10: Inside the tx - add the Organization row:

                var organization = new Organization
                {
                    OwnerId = user.Id,
                    Name = dto.OrganizationName.Trim(),
                    Description = dto.Description.Trim(),
                    LogoUrl = organizationLogoUrl,
                    VerificationDocumentUrl = verificationDocumentUrl,
                    Status = Domain.Enums.OrganizationStatus.Pending
                };
                await _unitOfWork.Repository<Organization>().AddAsync(organization);
                await _unitOfWork.SaveChangesAsync();

                // STEP 11: await tx.CommitAsync();
                // (if anything above threw, disposal rolls the whole thing back )
                await tx.CommitAsync();
            }
            catch (Exception)
            {

                //    delete the uploaded files + rethrow so the caller sees the failure
                await DeleteUploadedFilesAsync(ownerPhotoUrl, organizationLogoUrl, verificationDocumentUrl);
                throw;
            }

            // STEP 12: AFTER the commit - GenerateAndSendCodeAsync(user).
            await GenerateAndSendCodeAsync(user!);
            // STEP 13: Return AuthResponseDto { IsSuccess = true, IsAuthenticated = false,
            return new AuthResponseDto
            {
                IsSuccess = true,
                IsAuthenticated = false,
                Message = "You registered successfully. Please check your email for the confirmation code.",
                UserName = dto.OrganizationOwnerUsername,
            };
        }

        // Forget and Reset pass
        public async Task<AuthResponseDto> ForgetPasswordAsync(ForgetPasswordDto dto)
        {
            var validate = await _forgetPasswordValidator.ValidateAsync(dto);
            if (!validate.IsValid)
            {
                return new AuthResponseDto
                {
                    Message = "Validation failed: " + string.Join(", ", validate.Errors.Select(e => e.ErrorMessage))
                };
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "If the email is registered, a reset code has been sent to it."
                };
            }

            var ResetCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();//Random.Shared.Next(100000, 1000000).ToString();
            user.PasswordResetCode = ResetCode;
            user.PasswordResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(3);
            user.PasswordResetCodeAttempts = 0;
            await _userManager.UpdateAsync(user);


            var htmlBody = $"""
              <h2>Welcome to Auth, {user.FirstName}!</h2>
              <p>Your Reset code is:</p>
              <h1 style="letter-spacing: 5px;">{ResetCode}</h1>
              <p>Enter it in the app to reset your Password. It expires in 3 minutes.</p>
              """;

            await _emailService.SendAsync(user.Email, "Auth - Code to Reset your Password", htmlBody);
            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "If the email is registered, a reset code has been sent to it."
            };
        }
        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var validate = await _resetPasswordValidator.ValidateAsync(dto);
            if (!validate.IsValid)
            {
                return new AuthResponseDto
                {
                    Message = "Validation failed: " + string.Join(", ", validate.Errors.Select(e => e.ErrorMessage))
                };
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
            {
                return new AuthResponseDto
                {
                    Message = "Invalid email or Reset code."
                };
            }

            if (user.PasswordResetCodeAttempts >= 5)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Too many failed reset attempts. Please request a new code."
                };
            }

            if (dto.Code != user.PasswordResetCode || user.PasswordResetCodeExpiresAt < DateTime.UtcNow)
            {
                user.PasswordResetCodeAttempts++;

                if (user.PasswordResetCodeAttempts >= 5)
                {
                    user.PasswordResetCode = null;
                    user.PasswordResetCodeExpiresAt = null;
                }
                await _userManager.UpdateAsync(user);
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Reset code has expired or is invalid."
                };
            }


            await using var tx = await _unitOfWork.BeginTransactionAsync();

            var removePass = await _userManager.RemovePasswordAsync(user);
            if (!removePass.Succeeded)
            {
                return new AuthResponseDto
                {
                    Message = string.Join(',', removePass.Errors.Select(x => x.Description))
                };
            }
            var result = await _userManager.AddPasswordAsync(user, dto.NewPassword);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Message = string.Join(',', result.Errors.Select(x => x.Description))
                };
            }

            user.PasswordResetCodeAttempts = 0;
            user.PasswordResetCodeExpiresAt = null;
            user.PasswordResetCode = null;


            await _userManager.UpdateAsync(user);

            await tx.CommitAsync();


            var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            var jwtToken = await _jwtService.CreateJwtTokenAsync(user);
            return new AuthResponseDto
            {
                IsSuccess = true,
                IsAuthenticated = true,
                Message = "Password reset successfully.",
                UserName = user.UserName,
                Email = user.Email,
                Role = userRole,
                Token = jwtToken.Token,
                RefreshToken = jwtToken.RefreshToken,
                ExpireAt = jwtToken.ExpireAt
            };

        }



        public async Task<AuthResponseDto> GoogleResponseAsync(AuthenticateResult result)
        {
            // Read Google user claims
            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var firstName = result.Principal?.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastName = result.Principal?.FindFirst(ClaimTypes.Surname)?.Value;


            if (string.IsNullOrEmpty(email))
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Unable to retrieve email from Google."
                };
            }

            // Check if the user already exists
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {

                // Create a new Identity user if it doesn't exist
                user = new ApplicationUser
                {
                    UserName = email.Split('@')[0],
                   // UserName = email,
                    Email = email,
                    FirstName = firstName ?? "",
                    LastName = lastName ?? ""
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    return new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = string.Join(", ", createResult.Errors.Select(e => e.Description))
                    };
                }

                // Give the default role
                await _userManager.AddToRoleAsync(user, Roles.User);
            }

            // Generate JWT + Refresh Token
            var token = await _jwtService.CreateJwtTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            return new AuthResponseDto
            {
                IsAuthenticated = true,
                Message = "Login successful.",
                UserName = user.UserName,
                Email = user.Email,
                Token = token.Token,
                RefreshToken = token.RefreshToken,
                ExpireAt = token.ExpireAt,
                Role = roles.FirstOrDefault(),
                IsSuccess = true
            };
        }

        //-------------Private Mehtods
        private async Task GenerateAndSendCodeAsync(ApplicationUser user)
        {
            //Generate a 6 - digit confirmation code and save it on the user
            var confirmationCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();//Random.Shared.Next(100000, 1000000).ToString();
            user.EmailConfirmationCode = confirmationCode;
            user.EmailConfirmationCodeExpiresAt = DateTime.UtcNow.AddMinutes(3);
            //// Persist the code + expiry on the user row
            user.EmailConfirmationCodeAttempts = 0; // Reset attempts on new code generation
            await _userManager.UpdateAsync(user);


            //// EMAIL the code to the user's real Gmail (NOT returned in the response)
            var htmlBody = $"""
              <h2>Welcome to Auth, {user.FirstName}!</h2>
              <p>Your email confirmation code is:</p>
              <h1 style="letter-spacing: 5px;">{confirmationCode}</h1>
              <p>Enter it in the app to confirm your email. It expires in 3 minutes.</p>
              """;
            await _emailService.SendAsync(user.Email, "Auth - Your confirmation code", htmlBody);
        }
        //thats for converting unix timestamp to DateTime
        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dateTimeVal;
        }

        private async Task DeleteUploadedFilesAsync(string? ownerPhotoUrl, string? logoUrl, string? docUrl)
        {
            await _fileService.DeleteAsync(ownerPhotoUrl);
            await _fileService.DeleteAsync(logoUrl);
            await _fileService.DeleteAsync(docUrl);
        }
       
    }
}
