
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SendEmails
{
    public class EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger) : IEmailSender
    {
        protected readonly EmailSettings _emailSettings = emailSettings.Value;
        protected readonly ILogger<EmailSender> _logger = logger;

        public async Task SendEmail(string email, string Subject, string message)
        {
            try
            {
                string? mail = _emailSettings.SenderEmail;
                string? pass = _emailSettings.Password;

                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(mail, pass),
                    Timeout = 30000
                })
                {
                    var mailMessage = new MailMessage(from: mail!, to: email, Subject, message);
                    await client.SendMailAsync(mailMessage);

                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                _logger.LogError($"Error sending email: {ex}");
                throw; // Rethrow the exception to propagate it
            }
        }
    }
}