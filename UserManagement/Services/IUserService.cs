    using UserManagement.Models.Data;
    using UserManagement.Models.DTOs;

    namespace UserManagement.Services
    {
        public interface IUserService
        {
            Task<List<User>> GetAllUsersAsync();
            Task<User?> GetUserByEmailAsync(string email);  
            Task DeactivateUserAsync(int userId);
            Task SendPasswordRecoveryMailAsync(string email);
            Task<bool> VerifyPasswordCodeAsync(string email, string code);
            Task<bool> ResetPasswordAsync(PasswordRecoveryDTO recoveryRequest);
            Task<bool> CreateUserAsync(RegisterDTO registerRequest);

            Task AddEmailVerificationRecord(string code, string email);
        }
    }
