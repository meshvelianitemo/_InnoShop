namespace UserManagement.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string destinationEmail, string subject, string body);
        Task SendVerificationEmailAsync(string destinationEmail, string verifiationCode);
    }
}
