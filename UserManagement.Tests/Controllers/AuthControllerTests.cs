using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Controllers;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using static UserManagement.Exceptions.CustomException;

namespace UserManagement.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockLogger.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Login Tests

        [Fact]
        public async Task Login_ReturnsOk_AndSetsCookie()
        {
            var dto = new LoginDTO
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            _mockAuthService
                .Setup(x => x.LoginAsync(dto))
                .ReturnsAsync("fake-jwt-token");

            var result = await _controller.Login(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);

            Assert.True(
                _controller.Response.Headers["Set-Cookie"]
                .ToString()
                .Contains("JwtToken"));
        }


        [Fact]
        public async Task Login_Throws_WhenServiceThrows()
        {
            var dto = new LoginDTO
            {
                Email = "wrong@test.com",
                Password = "wrong"
            };

            _mockAuthService
                .Setup(x => x.LoginAsync(dto))
                .ThrowsAsync(new InvalidCredentialsException());

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => _controller.Login(dto));
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_ReturnsOk_WhenSuccessful()
        {
            var dto = new RegisterDTO
            {
                Email = "new@test.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            _mockAuthService
                .Setup(x => x.RegisterAsync(dto))
                .Returns(Task.CompletedTask);

            var result = await _controller.Register(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task Register_Throws_WhenServiceFails()
        {
            var dto = new RegisterDTO
            {
                Email = "existing@test.com"
            };

            _mockAuthService
                .Setup(x => x.RegisterAsync(dto))
                .ThrowsAsync(new EmailAlreadyExistsException(dto.Email));

            await Assert.ThrowsAsync<EmailAlreadyExistsException>(
                () => _controller.Register(dto));
        }
        #endregion

        #region VerifyEmail Tests
        [Fact]
        public async Task VerifyEmail_ReturnsOk_WhenSuccessful()
        {
            var dto = new EmailVerificationDTO
            {
                Email = "test@test.com",
                VerificationCode = "123456"
            };

            _mockAuthService
                .Setup(x => x.VerifyEmailAsync(dto))
                .Returns(Task.CompletedTask);

            var result = await _controller.VerifyEmail(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task VerifyEmail_Throws_WhenInvalidCode()
        {
            var dto = new EmailVerificationDTO
            {
                Email = "test@test.com",
                VerificationCode = "999999"
            };

            _mockAuthService
                .Setup(x => x.VerifyEmailAsync(dto))
                .ThrowsAsync(new VerificationCodeException());

            await Assert.ThrowsAsync<VerificationCodeException>(
                () => _controller.VerifyEmail(dto));
        }


        #endregion

        #region Logout Tests
        [Fact]
        public void Logout_ReturnsOk_AndDeletesCookie()
        {
            var result = _controller.Logout();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);

            Assert.True(
                _controller.Response.Headers["Set-Cookie"]
                .ToString()
                .Contains("JwtToken"));
        }

        #endregion
    }
}
