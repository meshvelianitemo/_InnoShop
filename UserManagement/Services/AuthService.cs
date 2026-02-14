
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserManagement.Exceptions;
using UserManagement.Models.Data;
using UserManagement.Models.DTOs;
using static UserManagement.Exceptions.CustomException;

namespace UserManagement.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly UserDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
      
        public AuthService(IConfiguration config, UserDbContext context, ILogger<AuthService> logger, IUserService userService, IEmailSender emailSender)
        {
            _config = config;
            _context = context;
            _logger = logger;
            _userService = userService;
            _emailSender = emailSender;
            
        }
        public async Task<string> GenerateWebToken(User user)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var roleId = await _context.UserRoles.FirstOrDefaultAsync(r => r.UserId == user.UserId);
                if (roleId == null)
                {
                    _logger.LogError($"User role not found for UserId: {user.UserId}");
                    throw new RoleNotFoundException("User role not assigned");
                }
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId.RoleId);
                if (role == null)
                {
                    _logger.LogError($"Role not found for RoleId: {roleId.RoleId}");
                    throw new RoleNotFoundException("Role information unavailable");
                }

                var claim = new Claim[]
                {
               new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
               new Claim(ClaimTypes.Email, user.Email),
               new Claim(ClaimTypes.Name, user.FirstName +" "+ user.LastName),
               new Claim(ClaimTypes.Role, role.RoleName)

                };

                var token = new JwtSecurityToken(

                   issuer: _config["JwtSettings:validIssuer"],
                   audience: _config["JwtSettings:validAudience"],
                   claims: claim,
                   expires: DateTime.UtcNow.AddMinutes(30),
                   signingCredentials: creds

                   );
                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                return await Task.FromResult(jwt);
            }
            catch (Exception ex) when (!(ex is ApplicationException))
            {
                _logger.LogError(ex, "Error generating JWT token");
                throw new CustomException("Failed to generate authentication token", 
                    "TOKEN_GENERATION_FAILED",500,ex);
            }

        }

        public async Task<string> LoginAsync(LoginDTO dto)
        {
            var existingUser = await _userService.GetUserByEmailAsync(dto.Email);
            if (existingUser == null)
            {
                _logger.LogWarning($"Login attempt with non-existent email: {dto.Email}");
                throw new InvalidCredentialsException();
            }

            // verifying password
            var isValid = VerifyLoginPassword(existingUser, dto);

            if (!isValid)
            {
                _logger.LogWarning($"Failed login attempt for user: {dto.Email}");
                throw new InvalidCredentialsException();
            }

            //generating JWT 
            var token = await GenerateWebToken(existingUser);
            return token;
        }

        public async Task RegisterAsync(RegisterDTO dto)
        {
            try
            {
                var existingUser = await _userService.GetUserByEmailAsync(dto.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email: {Email}", dto.Email);
                    throw new EmailAlreadyExistsException(dto.Email);
                }

                // Create user
                var user = await _userService.CreateUserAsync(dto);

                // Generate verification code
                var verificationCode = GenerateVerificationCode();

                // Save verification record
                await _userService.AddEmailVerificationRecord(verificationCode, dto.Email);

                // Send email
                await _emailSender.SendVerificationEmailAsync(dto.Email, verificationCode);

                _logger.LogInformation("New user registered: {Email}", dto.Email);
            }
            catch (EmailSendingException)
            {
                throw;
            }
            catch(EmailAlreadyExistsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", dto.Email);
                throw new DatabaseOperationException("Registration failed.");
            }
        }

        public async Task VerifyEmailAsync(EmailVerificationDTO verificationRequest)
        {
            var existingVerification = await _context.EmailVerifications
                .FirstOrDefaultAsync(ev => ev.VerificationCode == verificationRequest.VerificationCode && ev.Email == verificationRequest.Email);

            if (existingVerification == null)
            {
                _logger.LogWarning($"Invalid verification code attempt for: {verificationRequest.Email}");
                throw new VerificationCodeException();
            }
            if (existingVerification.ExpirationTime < DateTime.UtcNow)
            {
                throw new VerificationCodeException("Verification code has expired");
            }

            try
            {
                existingVerification.IsVerified = true;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == verificationRequest.Email);
                if (user == null)
                {
                    throw new UserNotFoundException("User not found during verification");
                }

                user.IsActive = true;

                _context.EmailVerifications.Update(existingVerification);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Email verified for user: {verificationRequest.Email}");
            }
            catch (Exception ex) when (!(ex is ApplicationException))
            {
                _logger.LogError(ex, $"Error verifying email for: {verificationRequest.Email}");
                throw new DatabaseOperationException("Email verification failed");
            }
        }

        public bool VerifyLoginPassword(User user,LoginDTO dto )
        {
            var hasher = new PasswordHasher<User>();
            var verificationResult = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning($"Failed login attempt for user: {user.Email}");
                return false;
            }
            return true;
        }

        private string GenerateVerificationCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }
    }
}
