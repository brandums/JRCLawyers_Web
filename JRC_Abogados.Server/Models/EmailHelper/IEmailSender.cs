namespace JRC_Abogados.Server.Models.EmailHelper
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string email, string subject, string message);
    }
}
