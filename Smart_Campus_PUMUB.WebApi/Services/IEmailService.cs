namespace Smart_Campus_PUMUB.WebApi.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    Task<bool> SendBulkEmailAsync(List<(string Email, string Name)> recipients, string subject, string htmlBody);
}
