using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Models.Data;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using Xunit;
using static UserManagement.Exceptions.CustomException;
namespace UserManagement.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly UserDbContext _context;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new UserDbContext(options);
            _mockEmailSender = new Mock<IEmailSender>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _userService = new UserService(_context, _mockEmailSender.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetAllUsersAsync Tests

        [Fact]
        public async Task GetAllUsersAsync_ReturnsOnlyActiveUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = 1, Email = "active1@test.com", IsActive = true, FirstName = "John", LastName = "Doe", PasswordHash = "hash1" },
                new User { UserId = 2, Email = "active2@test.com", IsActive = true, FirstName = "Jane", LastName = "Smith", PasswordHash = "hash2" },
                new User { UserId = 3, Email = "inactive@test.com", IsActive = false, FirstName = "Bob", LastName = "Brown", PasswordHash = "hash3" }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, user => Assert.True(user.IsActive));
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsEmptyList_WhenNoActiveUsers()
        {
            // Arrange
            var user = new User { UserId = 1, Email = "inactive@test.com", IsActive = false, FirstName = "Test", LastName = "User", PasswordHash = "hash" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.Empty(result);
        }

        #endregion


        #region GetUserByEmailAsync Tests

        [Fact]
        public async Task GetUserByEmailAsync_ReturnsUser_WhenUserExistsAndIsActive()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserByEmailAsync("test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ReturnsNull_WhenUserIsInactive()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = false,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserByEmailAsync("test@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Act
            var result = await _userService.GetUserByEmailAsync("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeactivateUserAsync Tests

        [Fact]
        public async Task DeactivateUserAsync_DeactivatesUser_WhenUserExists()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _userService.DeactivateUserAsync(1);

            // Assert
            var deactivatedUser = await _context.Users.FindAsync(1);
            Assert.NotNull(deactivatedUser);
            Assert.False(deactivatedUser.IsActive);
        }

        [Fact]
        public async Task DeactivateUserAsync_ThrowsException_WhenUserNotFound()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserNotFoundException>(
                () => _userService.DeactivateUserAsync(999));

            Assert.Equal("User not found or already deactivated.", exception.Message);
        }

        [Fact]
        public async Task DeactivateUserAsync_ThrowsException_WhenUserAlreadyDeactivated()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = false,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserNotFoundException>(
                () => _userService.DeactivateUserAsync(1));

            Assert.Equal("User not found or already deactivated.", exception.Message);
        }

        #endregion

        #region SendPasswordRecoveryMailAsync Tests

        [Fact]
        public async Task SendPasswordRecoveryMailAsync_SendsEmail_WhenUserExists()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _userService.SendPasswordRecoveryMailAsync("test@example.com");

            // Assert
            _mockEmailSender.Verify(
                x => x.SendVerificationEmailAsync("test@example.com", It.IsAny<string>()),
                Times.Once);

            var verification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == "test@example.com");
            Assert.NotNull(verification);
            Assert.False(verification.IsVerified);
            Assert.True(verification.ExpirationTime > DateTime.UtcNow);
        }

        [Fact]
        public async Task SendPasswordRecoveryMailAsync_ThrowsException_WhenUserNotFound()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserNotFoundException>(
                () => _userService.SendPasswordRecoveryMailAsync("nonexistent@example.com"));

            Assert.Equal("User not found.", exception.Message);
            _mockEmailSender.Verify(
                x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task SendPasswordRecoveryMailAsync_GeneratesSixDigitCode()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _userService.SendPasswordRecoveryMailAsync("test@example.com");

            // Assert
            var verification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == "test@example.com");
            Assert.NotNull(verification);
            Assert.Equal(6, verification.VerificationCode.Length);
            Assert.True(int.TryParse(verification.VerificationCode, out _));
        }

        #endregion

        #region VerifyPasswordCodeAsync Tests

        [Fact]
        public async Task VerifyPasswordCodeAsync_ReturnsTrue_WhenCodeIsValid()
        {
            // Arrange
            var verification = new EmailVerification
            {
                Email = "test@example.com",
                VerificationCode = "123456",
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                IsVerified = false
            };
            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.VerifyPasswordCodeAsync("test@example.com", "123456");

            // Assert
            Assert.True(result);
            var updatedVerification = await _context.EmailVerifications.FindAsync(verification.VerificationId);
            Assert.True(updatedVerification.IsVerified);
        }

        [Fact]
        public async Task VerifyPasswordCodeAsync_ReturnsFalse_WhenCodeDoesNotExist()
        {
            // Act
            var result = await _userService.VerifyPasswordCodeAsync("test@example.com", "999999");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyPasswordCodeAsync_ReturnsFalse_WhenCodeIsExpired()
        {
            // Arrange
            var verification = new EmailVerification
            {
                Email = "test@example.com",
                VerificationCode = "123456",
                ExpirationTime = DateTime.UtcNow.AddMinutes(-5), // Expired
                IsVerified = false
            };
            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.VerifyPasswordCodeAsync("test@example.com", "123456");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyPasswordCodeAsync_ReturnsFalse_WhenCodeAlreadyVerified()
        {
            // Arrange
            var verification = new EmailVerification
            {
                Email = "test@example.com",
                VerificationCode = "123456",
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                IsVerified = true // Already verified
            };
            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.VerifyPasswordCodeAsync("test@example.com", "123456");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CreateUserAsync Tests

        [Fact]
        public async Task CreateUserAsync_CreatesUser_WithCorrectData()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                FirstName = "  John  ",
                LastName = "  Doe  ",
                Email = "  john.doe@example.com  ",
                Password = "  SecurePass123!  "
            };

            // Act
            var result = await _userService.CreateUserAsync(registerDto);

            // Assert
            Assert.True(result);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@example.com");
            Assert.NotNull(user);
            Assert.Equal("john", user.FirstName); // Trimmed and lowercase
            Assert.Equal("doe", user.LastName);   // Trimmed and lowercase
            Assert.False(user.IsActive); // New users are inactive
            Assert.NotNull(user.PasswordHash);
            Assert.NotEqual("SecurePass123!", user.PasswordHash); // Password should be hashed
        }

        [Fact]
        public async Task CreateUserAsync_AssignsUserRole()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "SecurePass123!"
            };

            // Act
            await _userService.CreateUserAsync(registerDto);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@example.com");
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.UserId);

            Assert.NotNull(userRole);
            Assert.Equal(1, userRole.RoleId);
        }

        [Fact]
        public async Task CreateUserAsync_HashesPassword()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "PlainPassword123!"
            };

            // Act
            await _userService.CreateUserAsync(registerDto);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@example.com");
            Assert.NotNull(user);
            Assert.NotEqual("PlainPassword123!", user.PasswordHash);
            Assert.NotEmpty(user.PasswordHash);
        }

        #endregion

        #region AddEmailVerificationRecord Tests

        [Fact]
        public async Task AddEmailVerificationRecord_AddsRecord_WithCorrectData()
        {
            // Arrange
            var code = "123456";
            var email = "test@example.com";

            // Act
            await _userService.AddEmailVerificationRecord(code, email);

            // Assert
            var verification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == email && v.VerificationCode == code);

            Assert.NotNull(verification);
            Assert.Equal(code, verification.VerificationCode);
            Assert.Equal(email, verification.Email);
            Assert.False(verification.IsVerified);
            Assert.True(verification.ExpirationTime > DateTime.UtcNow);
            Assert.True(verification.ExpirationTime <= DateTime.UtcNow.AddMinutes(15));
        }

        #endregion

        #region ResetPasswordAsync Tests

        [Fact]
        public async Task ResetPasswordAsync_ResetsPassword_WhenVerificationIsValid()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                PasswordHash = "OldHashedPassword"
            };
            await _context.Users.AddAsync(user);

            var verification = new EmailVerification
            {
                Email = "test@example.com",
                VerificationCode = "123456",
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                IsVerified = true
            };
            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            var recoveryDto = new PasswordRecoveryDTO
            {
                Email = "test@example.com",
                NewPassword = "NewSecurePass123!",
                ConfirmPassword = "NewSecurePass123!"
            };

            // Act
            var result = await _userService.ResetPasswordAsync(recoveryDto);

            // Assert
            Assert.True(result);
            var updatedUser = await _context.Users.FindAsync(1);
            Assert.NotEqual("OldHashedPassword", updatedUser.PasswordHash);
        }

        [Fact]
        public async Task ResetPasswordAsync_ThrowsException_WhenVerificationNotFound()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                PasswordHash = "OldHashedPassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var recoveryDto = new PasswordRecoveryDTO
            {
                Email = "test@example.com",
                NewPassword = "NewSecurePass123!",
                ConfirmPassword = "NewSecurePass123!"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _userService.ResetPasswordAsync(recoveryDto));

            Assert.Equal("Password reset not verified.", exception.Message);
        }

        [Fact]
        public async Task ResetPasswordAsync_ThrowsException_WhenVerificationExpired()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                PasswordHash = "OldHashedPassword"
            };
            await _context.Users.AddAsync(user);

            var verification = new EmailVerification
            {
                Email = "test@example.com",
                VerificationCode = "123456",
                ExpirationTime = DateTime.UtcNow.AddMinutes(-5), // Expired
                IsVerified = true
            };
            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            var recoveryDto = new PasswordRecoveryDTO
            {
                Email = "test@example.com",
                NewPassword = "NewSecurePass123!",
                ConfirmPassword = "NewSecurePass123!"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _userService.ResetPasswordAsync(recoveryDto));

            Assert.Equal("Password reset not verified.", exception.Message);
        }

        #endregion

    }
}