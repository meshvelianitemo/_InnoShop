namespace UserManagement.Models.Data
{
    public class EmailVerification
    {
        public int VerificationId { get; set; }
        public string VerificationCode { get; set; }
        public string Email { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsVerified { get; set; }
    }
}
