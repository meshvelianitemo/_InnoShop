namespace UserManagement.Models.DTOs
{
    public class PasswordRecoveryDTO
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
