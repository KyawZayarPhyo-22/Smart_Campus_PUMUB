using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/mail")]
public class MailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public MailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    // ─── POST api/mail/send ──────────────────────────────────────────────────
    /// <summary>
    /// Send a single email to one recipient.
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMailRequestModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var body = request.IsHtml
            ? request.Body!
            : $"<pre>{System.Net.WebUtility.HtmlEncode(request.Body)}</pre>";

        var ok = await _emailService.SendEmailAsync(
            request.ToEmail!,
            request.ToName ?? request.ToEmail!,
            request.Subject!,
            body
        );

        return ok
            ? Ok(new ActionResponseModel { IsSuccess = true, Message = "Email ပို့ပြီးပါပြီ။" })
            : StatusCode(500, new ActionResponseModel { IsSuccess = false, Message = "Email ပို့ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့ပါသည်။" });
    }

    // ─── POST api/mail/send-bulk ─────────────────────────────────────────────
    /// <summary>
    /// Send the same email to multiple recipients.
    /// </summary>
    [HttpPost("send-bulk")]
    public async Task<IActionResult> SendBulk([FromBody] SendBulkMailRequestModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var body = request.IsHtml
            ? request.Body!
            : $"<pre>{System.Net.WebUtility.HtmlEncode(request.Body)}</pre>";

        var recipients = request.Recipients
            .Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .Select(r => (r.Email!, r.Name ?? r.Email!))
            .ToList();

        if (recipients.Count == 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှန်ကန်သော Recipients မရှိပါ။" });

        var ok = await _emailService.SendBulkEmailAsync(recipients, request.Subject!, body);

        return ok
            ? Ok(new ActionResponseModel { IsSuccess = true, Message = $"Email {recipients.Count} ဦးထံ ပို့ပြီးပါပြီ။" })
            : StatusCode(500, new ActionResponseModel { IsSuccess = false, Message = "Email တစ်ခုမှ မပို့နိုင်ပါ။" });
    }

    // ─── POST api/mail/test ──────────────────────────────────────────────────
    /// <summary>
    /// Quick SMTP connection test — sends a test email to the given address.
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> Test([FromQuery][System.ComponentModel.DataAnnotations.EmailAddress] string toEmail)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Email address ဖြည့်ပေးပါ။" });

        const string subject = "Smart Campus PUMUB — Mail Service Test";
        const string body =
            "<div style=\"font-family:sans-serif;padding:24px;background:#f5f5f5;\">" +
            "<h2 style=\"color:#1976d2;\">&#9989; Mail Service Test</h2>" +
            "<p>Smart Campus PUMUB Mail API မှ ဤ Email ကို Test အနေဖြင့် ပို့ပေးနေပါသည်။</p>" +
            "<p>SMTP Connection <strong>အောင်မြင်သည်</strong>။</p>" +
            "</div>";

        var ok = await _emailService.SendEmailAsync(toEmail, toEmail, subject, body);

        return ok
            ? Ok(new ActionResponseModel { IsSuccess = true, Message = "Test email ပို့ပြီးပါပြီ။" })
            : StatusCode(500, new ActionResponseModel { IsSuccess = false, Message = "Test email မပို့နိုင်ပါ။ SMTP setting စစ်ဆေးပေးပါ။" });
    }
}
