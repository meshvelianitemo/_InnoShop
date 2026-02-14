using MailKit.Net.Smtp;
using MimeKit;
using System.Security.Cryptography;
using System.Threading.Tasks;
namespace UserManagement.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendEmailAsync(string destinationEmail, string subject, string message)
        {
            var ownEmail = _config["EmailSettings:Address"];
            var ownEmailPassword = _config["EmailSettings:Password"];
            var smtpHost = _config["EmailSettings:SmtpHost"];
            var port = int.Parse(_config["EmailSettings:Port"]);


            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("intern@innowise", ownEmail));
            emailMessage.To.Add(new MailboxAddress("", destinationEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = message };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(smtpHost, port, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(ownEmail, ownEmailPassword);
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
                }
            }
            
        }

        public async Task SendVerificationEmailAsync(string destinationEmail, string verificationCode) 
        {
            var subject = "Your Email Verification Code";
            var message = BuildVerificationEmailHtml(verificationCode);
            await SendEmailAsync(destinationEmail, subject, message);
        }

        private static string BuildVerificationEmailHtml(string verificationCode)
        {

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                <div style='max-width: 500px; margin: auto; background: #ffffff; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #333;'>Email Verification</h2>
                    <p>Your verification code is:</p>
                    <div style='font-size: 28px; font-weight: bold; letter-spacing: 4px; margin: 20px 0;'>
                        {verificationCode}
                    </div>
                    <p>This code expires in 10 minutes.</p>
                    <p style='color: #777; font-size: 12px;'>
                        If you didn’t request this, ignore this email.
                    </p>
                </div>
            </body>
            </html>";
        }

        private static string BuildPasswordRecoveryEmailHtml(string passwordRecoveryCode)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                <div style='max-width: 500px; margin: auto; background: #ffffff; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #333;'>Password Recovery</h2>
                    <p>Paste the code from below to reset your password:</p>
                    <a href='{passwordRecoveryCode}' style='display: inline-block; padding: 10px 20px; color: #fff; background-color: #007BFF; text-decoration: none; border-radius: 4px;'>Reset Password</a>
                    <p style='margin-top: 20px;'>If you didn’t request this, ignore this email.</p>
                </div>
            </body>
            </html>";
        }

    }
}
