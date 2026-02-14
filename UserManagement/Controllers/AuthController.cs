using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models.DTOs;
using UserManagement.Services;


namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;   
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO loginRequest)
        {
            var tokenResult = await _authService.LoginAsync(loginRequest);
            //grant token in cookie
            Response.Cookies.Append("JwtToken", tokenResult, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.Now.AddMinutes(30)
            });

            _logger.LogInformation($"User logged in: {loginRequest.Email}");
            return Ok(new { message = "Login successful" });
            
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDTO registerRequest)
        {
            
            await _authService.RegisterAsync(registerRequest);

            return Ok(new
            {
                message = "Registration successful. Check your email for verification code."
            });
            
        }


        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail(EmailVerificationDTO verificationRequest)
        {
            await _authService.VerifyEmailAsync(verificationRequest);
            return Ok(new
            {
                message = "Email verification successful. You can now log in."
            });
        }

        [HttpPost("Logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // Removing the JWT token from cookies 
            Response.Cookies.Delete("JwtToken");
            _logger.LogInformation("User logged out");
            return Ok(new { message = "Logout successful" });
            
        }

    }
}
