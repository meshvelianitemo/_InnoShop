using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Models.Data;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using Xunit;
using static UserManagement.Exceptions.CustomException;

namespace UserManagement.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly UserDbContext _context;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new UserDbContext(options);

            _mockUserService = new Mock<IUserService>();
            _mockEmailSender = new Mock<IEmailSender>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            var configValues = new Dictionary<string, string>
            {
                {"JwtSettings:Key", "SuperSecretKeyForTesting1234567890!!"},
                {"JwtSettings:validIssuer", "TestIssuer"},
                {"JwtSettings:validAudience", "TestAudience"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues!)
                .Build();

            _authService = new AuthService(
                _configuration,
                _context,
                _mockLogger.Object,
                _mockUserService.Object,
                _mockEmailSender.Object
            );
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
        {
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                IsActive = true
            };

            // hash password properly
            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, "Password123!");

            _mockUserService
                .Setup(x => x.GetUserByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            // seed role
            _context.Roles.Add(new Role { RoleId = 1, RoleName = "Client" });
            _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
            await _context.SaveChangesAsync();

            var dto = new LoginDTO
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var token = await _authService.LoginAsync(dto);

            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task LoginAsync_Throws_WhenUserDoesNotExist()
        {
            _mockUserService
                .Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var dto = new LoginDTO
            {
                Email = "fake@example.com",
                Password = "Password123!"
            };

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => _authService.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_Throws_WhenPasswordIsInvalid()
        {
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe"
            };

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, "CorrectPassword");

            _mockUserService
                .Setup(x => x.GetUserByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            var dto = new LoginDTO
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => _authService.LoginAsync(dto));
        }


        [Fact]
        public async Task RegisterAsync_CallsCreateUser_AndSendsEmail()
        {
            var dto = new RegisterDTO
            {
                Email = "new@test.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "Password123!"
            };

            _mockUserService
                .Setup(x => x.GetUserByEmailAsync(dto.Email))
                .ReturnsAsync((User?)null);

            _mockUserService
                .Setup(x => x.CreateUserAsync(dto))
                .ReturnsAsync(true);

            _mockUserService
                .Setup(x => x.AddEmailVerificationRecord(It.IsAny<string>(), dto.Email))
                .Returns(Task.CompletedTask);

            await _authService.RegisterAsync(dto);

            _mockUserService.Verify(x => x.CreateUserAsync(dto), Times.Once);
            _mockEmailSender.Verify(
                x => x.SendVerificationEmailAsync(dto.Email, It.IsAny<string>()),
                Times.Once);
        }


        [Fact]
        public async Task RegisterAsync_Throws_WhenEmailAlreadyExists()
        {
            var dto = new RegisterDTO
            {
                Email = "existing@test.com"
            };

            _mockUserService
                .Setup(x => x.GetUserByEmailAsync(dto.Email))
                .ReturnsAsync(new User());

            await Assert.ThrowsAsync<EmailAlreadyExistsException>(
                () => _authService.RegisterAsync(dto));
        }


        [Fact]
        public async Task VerifyEmailAsync_ActivatesUser_WhenCodeIsValid()
        {
            var user = new User
            {
                UserId = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var verification = new EmailVerification
            {
                Email = "test@example.com",
                VerificationCode = "123456",
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                IsVerified = false
            };

            await _context.Users.AddAsync(user);
            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            var dto = new EmailVerificationDTO
            {
                Email = "test@example.com",
                VerificationCode = "123456"
            };

            await _authService.VerifyEmailAsync(dto);

            var updatedUser = await _context.Users.FindAsync(1);
            Assert.True(updatedUser!.IsActive);
        }


        [Fact]
        public async Task VerifyEmailAsync_Throws_WhenCodeInvalid()
        {
            var dto = new EmailVerificationDTO
            {
                Email = "test@example.com",
                VerificationCode = "999999"
            };

            await Assert.ThrowsAsync<VerificationCodeException>(
                () => _authService.VerifyEmailAsync(dto));
        }



    }
}
