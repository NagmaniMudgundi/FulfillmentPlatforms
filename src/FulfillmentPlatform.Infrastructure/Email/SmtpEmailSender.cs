using FulfillmentPlatform.Application.Common.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace FulfillmentPlatform.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        // 1. WRITE THE EMAIL
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["Email:From"]!));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        // 2. CONNECT to the SMTP server (encryption only if configured, e.g. for Gmail later)
        using var client = new SmtpClient();
        var useStartTls = bool.Parse(_config["Email:UseStartTls"] ?? "false");
        await client.ConnectAsync(
            _config["Email:Host"]!,
            int.Parse(_config["Email:Port"]!),
            useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        // 3. LOG IN only if a username is configured (Mailpit: no, Gmail later: yes)
        var username = _config["Email:Username"];
        if (!string.IsNullOrEmpty(username))
        {
            await client.AuthenticateAsync(username, _config["Email:Password"]!);
        }

        // 4. SEND and disconnect
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}