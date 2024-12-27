namespace WebApiMariaMC.IServicies
{
    public interface ISendMailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, string attachmentPath);
    }
}
