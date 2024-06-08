namespace SendEmails
{
    public interface IEmailSender
    {
        Task SendEmail(string email, string Subject, string message);
    }
}