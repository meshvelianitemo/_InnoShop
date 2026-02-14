using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using UserManagement.Models.Data;
using UserManagement.Models.DTOs;
using static UserManagement.Exceptions.CustomException;

namespace UserManagement.Services
{
    public class UserService : IUserService
    {
        private readonly UserDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<UserService> _logger;

        public UserService(UserDbContext context, IEmailSender emailSender, ILogger<UserService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task DeactivateUserAsync(int userId)
        {
            var existingUser = await _context.Users.
                                    FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
            if (existingUser == null)
            {
                throw new UserNotFoundException("User not found or already deactivated.");
            }
            existingUser.IsActive = false;
            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            return users;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            return user;
        }

        public async Task SendPasswordRecoveryMailAsync(string email)
        {
            var existingUser = await _context.Users
                                        .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (existingUser == null)
            {
                throw new UserNotFoundException("User not found.");
            }

            //generate recovery code
            var recoveryCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            EmailVerification emailVerification = new EmailVerification
            {
                VerificationCode = recoveryCode,
                ExpirationTime = DateTime.UtcNow.AddMinutes(15),
                IsVerified = false,
                Email = email
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            //send an email
            await _emailSender.SendVerificationEmailAsync(email, recoveryCode);

        }

        public async Task<bool> VerifyPasswordCodeAsync(string email, string code)
        {
            var verificationRecord = await _context.EmailVerifications
                                            .Where(ev => ev.Email == email && ev.VerificationCode == code)
                                            .OrderByDescending(ev => ev.ExpirationTime)
                                            .FirstOrDefaultAsync();
            if (verificationRecord == null || verificationRecord.IsVerified || verificationRecord.ExpirationTime < DateTime.UtcNow)
            {
                return false;
                
            }
            verificationRecord.IsVerified = true;
            _context.EmailVerifications.Update(verificationRecord);
            await _context.SaveChangesAsync();
            return true;


        }

        public async Task<bool> ResetPasswordAsync(PasswordRecoveryDTO recoveryRequest)
        {
            var verification = await _context.EmailVerifications
                .OrderByDescending(v => v.ExpirationTime)
                .FirstOrDefaultAsync(v => v.Email == recoveryRequest.Email && v.IsVerified);

            if (verification == null || verification.ExpirationTime < DateTime.UtcNow)
            {
                _logger.LogWarning($"Password reset attempt with invalid or expired code for: {recoveryRequest.Email}");
                throw new Exception("Password reset not verified.");
            }

           
            if (recoveryRequest.NewPassword != recoveryRequest.ConfirmPassword)
            {
                _logger.LogWarning($"Password reset attempt with mismatched passwords for: {recoveryRequest.Email}");
                throw new InvalidOperationException("Passwords do not match.");

            }

            var user = await GetUserByEmailAsync(recoveryRequest.Email);
            if (user == null)
            {
                throw new UserNotFoundException("User not found.");
            }
            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(user, recoveryRequest.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateUserAsync(RegisterDTO registerRequest)
        {
            // Hash password and create user
            var PasswordHasher = new PasswordHasher<User>();
            var user = new User
            {
                FirstName = registerRequest.FirstName.Trim().ToLower(),
                LastName = registerRequest.LastName.Trim().ToLower(),
                IsActive = false,
                Email = registerRequest.Email.Trim()
            };
            var hashedPassword = PasswordHasher.HashPassword(user, registerRequest.Password.Trim());
            user.PasswordHash = hashedPassword;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Add new User Role
            var userRole = new UserRole
            {
                RoleId = 1,
                UserId = user.UserId
            };

            
            await _context.UserRoles.AddAsync(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddEmailVerificationRecord(string code, string email)
        {
            EmailVerification emailVerification = new EmailVerification
            {
                VerificationCode = code,
                ExpirationTime = DateTime.UtcNow.AddMinutes(15),
                IsVerified = false,
                Email = email
            };
            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();
        }
    }
}
