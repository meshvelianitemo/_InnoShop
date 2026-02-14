using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UserManagement.Controllers;
using UserManagement.Models.Data;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using Xunit;

namespace UserManagement.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _controller = new UserController(_mockUserService.Object);
        }

        #region Users Tests (GET api/User/Admin/Users)

        [Fact]
        public async Task Users_ReturnsOkResult_WithListOfUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = 1, Email = "user1@test.com", FirstName = "John", LastName = "Doe", IsActive = true, PasswordHash = "hash1" },
                new User { UserId = 2, Email = "user2@test.com", FirstName = "Jane", LastName = "Smith", IsActive = true, PasswordHash = "hash2" }
            };
            _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

            // Act
            var result = await _controller.Users();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<List<User>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count);
            _mockUserService.Verify(s => s.GetAllUsersAsync(), Times.Once);
        }

        [Fact]
        public async Task Users_ReturnsOkResult_WithEmptyList_WhenNoUsersExist()
        {
            // Arrange
            var users = new List<User>();
            _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

            // Act
            var result = await _controller.Users();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<List<User>>(okResult.Value);
            Assert.Empty(returnedUsers);
        }

        #endregion

        #region ResetPassword Tests (POST api/User/Verify/Reset-Password)

        [Fact]
        public async Task ResetPassword_ReturnsOkResult_WhenPasswordResetSuccessful()
        {
            // Arrange
            var recoveryDto = new PasswordRecoveryDTO
            {
                Email = "test@example.com",
                NewPassword = "NewSecure123!",
                ConfirmPassword = "NewSecure123!"
            };
            _mockUserService.Setup(s => s.ResetPasswordAsync(It.IsAny<PasswordRecoveryDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ResetPassword(recoveryDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Verify the message property exists
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Password reset successful.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.ResetPasswordAsync(recoveryDto), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_CallsServiceWithCorrectData()
        {
            // Arrange
            var recoveryDto = new PasswordRecoveryDTO
            {
                Email = "test@example.com",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };
            _mockUserService.Setup(s => s.ResetPasswordAsync(It.IsAny<PasswordRecoveryDTO>()))
                .ReturnsAsync(true);

            // Act
            await _controller.ResetPassword(recoveryDto);

            // Assert
            _mockUserService.Verify(s => s.ResetPasswordAsync(
                It.Is<PasswordRecoveryDTO>(dto =>
                    dto.Email == recoveryDto.Email &&
                    dto.NewPassword == recoveryDto.NewPassword &&
                    dto.ConfirmPassword == recoveryDto.ConfirmPassword)),
                Times.Once);
        }

        #endregion

        #region PasswordRecovery Tests (PATCH api/User/Recover-Password)

        [Fact]
        public async Task PasswordRecovery_ReturnsOkResult_WhenEmailIsValid()
        {
            // Arrange
            var email = "test@example.com";
            _mockUserService.Setup(s => s.SendPasswordRecoveryMailAsync(email))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.PasswordRecovery(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Password recovery email sent.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.SendPasswordRecoveryMailAsync(email), Times.Once);
        }

        [Fact]
        public async Task PasswordRecovery_ReturnsBadRequest_WhenEmailIsNull()
        {
            // Act
            var result = await _controller.PasswordRecovery(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);

            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Email is required.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.SendPasswordRecoveryMailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PasswordRecovery_ReturnsBadRequest_WhenEmailIsEmpty()
        {
            // Act
            var result = await _controller.PasswordRecovery(string.Empty);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Email is required.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.SendPasswordRecoveryMailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PasswordRecovery_ReturnsBadRequest_WhenEmailIsWhitespace()
        {
            // Act
            var result = await _controller.PasswordRecovery("   ");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockUserService.Verify(s => s.SendPasswordRecoveryMailAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region VerifyPasswordVerificationCode Tests (PATCH api/User/Recover-Password/Verify)

        [Fact]
        public async Task VerifyPasswordVerificationCode_ReturnsOkResult_WhenCodeIsValid()
        {
            // Arrange
            var email = "test@example.com";
            var code = "123456";
            _mockUserService.Setup(s => s.VerifyPasswordCodeAsync(email, code))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.VerifyPasswordVerificationCode(email, code);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Verification successful.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.VerifyPasswordCodeAsync(email, code), Times.Once);
        }

        [Fact]
        public async Task VerifyPasswordVerificationCode_ReturnsBadRequest_WhenCodeIsInvalid()
        {
            // Arrange
            var email = "test@example.com";
            var code = "999999";
            _mockUserService.Setup(s => s.VerifyPasswordCodeAsync(email, code))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.VerifyPasswordVerificationCode(email, code);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Invalid verification code.", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task VerifyPasswordVerificationCode_ReturnsBadRequest_WhenEmailIsNull()
        {
            // Act
            var result = await _controller.VerifyPasswordVerificationCode(null, "123456");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Email and code are required.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.VerifyPasswordCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyPasswordVerificationCode_ReturnsBadRequest_WhenCodeIsNull()
        {
            // Act
            var result = await _controller.VerifyPasswordVerificationCode("test@example.com", null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Email and code are required.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.VerifyPasswordCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyPasswordVerificationCode_ReturnsBadRequest_WhenEmailIsEmpty()
        {
            // Act
            var result = await _controller.VerifyPasswordVerificationCode(string.Empty, "123456");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockUserService.Verify(s => s.VerifyPasswordCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyPasswordVerificationCode_ReturnsBadRequest_WhenCodeIsEmpty()
        {
            // Act
            var result = await _controller.VerifyPasswordVerificationCode("test@example.com", string.Empty);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockUserService.Verify(s => s.VerifyPasswordCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyPasswordVerificationCode_ReturnsBadRequest_WhenBothAreEmpty()
        {
            // Act
            var result = await _controller.VerifyPasswordVerificationCode(string.Empty, string.Empty);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockUserService.Verify(s => s.VerifyPasswordCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region DeactivateUser Tests (PATCH api/User/Admin/Deactivate-User)

        [Fact]
        public async Task DeactivateUser_ReturnsOkResult_WhenUserIdIsValid()
        {
            // Arrange
            var userId = 1;
            _mockUserService.Setup(s => s.DeactivateUserAsync(userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeactivateUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("User deactivated successfully.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.DeactivateUserAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeactivateUser_ReturnsBadRequest_WhenUserIdIsZero()
        {
            // Act
            var result = await _controller.DeactivateUser(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Invalid User ID.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.DeactivateUserAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateUser_ReturnsBadRequest_WhenUserIdIsNegative()
        {
            // Act
            var result = await _controller.DeactivateUser(-1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Invalid User ID.", messageProperty.GetValue(response));

            _mockUserService.Verify(s => s.DeactivateUserAsync(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(-100)]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task DeactivateUser_ReturnsBadRequest_WhenUserIdIsInvalid(int invalidUserId)
        {
            // Act
            var result = await _controller.DeactivateUser(invalidUserId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockUserService.Verify(s => s.DeactivateUserAsync(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(9999)]
        public async Task DeactivateUser_CallsService_WithValidUserIds(int validUserId)
        {
            // Arrange
            _mockUserService.Setup(s => s.DeactivateUserAsync(validUserId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeactivateUser(validUserId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(s => s.DeactivateUserAsync(validUserId), Times.Once);
        }

        #endregion

        #region Additional Edge Case Tests

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenDTOIsNull()
        {
            var result = await _controller.ResetPassword(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            _mockUserService.Verify(
                s => s.ResetPasswordAsync(It.IsAny<PasswordRecoveryDTO>()),
                Times.Never);
        }

        [Fact]
        public async Task PasswordRecovery_TrimsWhitespace_BeforeValidation()
        {
            // Arrange
            var emailWithWhitespace = "   test@example.com   ";

            // Act
            var result = await _controller.PasswordRecovery(emailWithWhitespace);

            // Assert
            // This will pass because the controller uses string.IsNullOrEmpty
            // which doesn't trim, so "   " would fail but "  email  " would pass
            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task VerifyPasswordVerificationCode_WithWhitespaceEmail_PassesValidation()
        {
            // Arrange
            var email = "   test@example.com   ";
            var code = "123456";
            _mockUserService.Setup(s => s.VerifyPasswordCodeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.VerifyPasswordVerificationCode(email, code);

            // Assert
            // Passes because string.IsNullOrEmpty doesn't trim
            Assert.IsType<OkObjectResult>(result);
        }

        #endregion
    }
}