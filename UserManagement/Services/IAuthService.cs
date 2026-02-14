using Microsoft.AspNetCore.Identity.Data;
using UserManagement.Models.Data;
using UserManagement.Models.DTOs;

namespace UserManagement.Services
{
    public interface IAuthService
    {
        Task<String> GenerateWebToken(User user);
        bool VerifyLoginPassword(User user , LoginDTO dto);
        Task RegisterAsync(RegisterDTO dto);
        Task<string> LoginAsync(LoginDTO dto);
        Task VerifyEmailAsync(EmailVerificationDTO dto);
    }
}
