using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Petly.Business.Services;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string recipientEmail, string code, int lifetimeMinutes, CancellationToken cancellationToken = default);
}

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;

    public EmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendPasswordResetCodeAsync(string recipientEmail, string code, int lifetimeMinutes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            string.IsNullOrWhiteSpace(_options.SenderEmail) ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("SMTP не налаштований. Заповніть секцію Email в appsettings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = "Відновлення пароля — Petly",
            Body = $"""
Ви отримали цей лист, оскільки було надіслано запит на відновлення пароля в системі Petly.

Ваш код підтвердження: {code}
Код дійсний протягом {lifetimeMinutes} хвилин.

Якщо ви не надсилали цей запит, просто проігноруйте лист.
""",
            IsBodyHtml = false
        };

        message.To.Add(recipientEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
