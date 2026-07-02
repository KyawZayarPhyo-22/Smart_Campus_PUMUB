using System.Net;
using System.Net.Mail;

namespace Smart_Campus_PUMUB.WebApi.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    private string SmtpHost    => _config["Email:SmtpHost"]       ?? "smtp.gmail.com";
    private int    SmtpPort    => int.Parse(_config["Email:SmtpPort"] ?? "587");
    private bool   EnableSsl   => bool.Parse(_config["Email:EnableSsl"] ?? "true");
    private string SenderEmail => _config["Email:SenderEmail"]    ?? "";
    private string SenderName  => _config["Email:SenderName"]     ?? "Smart Campus PUMUB";
    private string SenderPwd   => _config["Email:SenderPassword"] ?? "";

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            using var client  = BuildSmtpClient();
            using var message = BuildMessage(subject, htmlBody);
            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendBulkEmailAsync(List<(string Email, string Name)> recipients, string subject, string htmlBody)
    {
        if (recipients == null || recipients.Count == 0)
            return false;

        int successCount = 0;
        foreach (var (email, name) in recipients)
        {
            var ok = await SendEmailAsync(email, name, subject, htmlBody);
            if (ok) successCount++;
        }

        _logger.LogInformation("Bulk email: {Success}/{Total} sent", successCount, recipients.Count);
        return successCount > 0;
    }

    // ─── Private Helpers ────────────────────────────────────────────────────────

    private SmtpClient BuildSmtpClient() =>
        new SmtpClient(SmtpHost, SmtpPort)
        {
            EnableSsl      = EnableSsl,
            Credentials    = new NetworkCredential(SenderEmail, SenderPwd),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

    private MailMessage BuildMessage(string subject, string htmlBody) =>
        new MailMessage
        {
            From       = new MailAddress(SenderEmail, SenderName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };
}
