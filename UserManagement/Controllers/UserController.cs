using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models.Data;
using UserManagement.Models.DTOs;
using UserManagement.Services;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpGet("Admin/Users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("Verify/Reset-Password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordRecoveryDTO recoveryRequest)
        {
            if (recoveryRequest == null)
                return BadRequest(new { message = "Invalid request." });


            await _userService.ResetPasswordAsync(recoveryRequest);
            return Ok(new { message = "Password reset successful." });
        }

        [HttpPatch("Recover-Password")]
        public async Task<IActionResult> PasswordRecovery([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            await _userService.SendPasswordRecoveryMailAsync(email);

            return Ok(new { message = "Password recovery email sent." });

        }


        [HttpPatch("Recover-Password/Verify")]
        public async Task<IActionResult> VerifyPasswordVerificationCode([FromQuery] string email, [FromQuery] string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                return BadRequest(new { message = "Email and code are required." });
            }
            var result = await _userService.VerifyPasswordCodeAsync(email, code);
            if (!result)
            {
                return BadRequest(new { message = "Invalid verification code."});
            }
            return Ok(new { message = "Verification successful." });
        }

        [HttpPatch("Admin/Deactivate-User")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "Invalid User ID." });
            }
            await _userService.DeactivateUserAsync(userId);
            return Ok(new {message = "User deactivated successfully."});
        }



    }
}
