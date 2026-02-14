namespace UserManagement.Models.DTOs
{
    public class EmailVerificationDTO
    {
        public string Email { get; set; }
        public string VerificationCode { get; set; }
    }
}
