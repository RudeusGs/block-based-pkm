using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Email;

namespace Pkm.Infrastructure.Email;

internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("Email:Smtp:Host chưa được cấu hình.");

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Email:Smtp:FromEmail chưa được cấu hình.");

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.UserName,
                _options.Password);
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody ?? message.PlainTextBody,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
        };

        mailMessage.To.Add(new MailAddress(message.ToEmail));

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mailMessage.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    message.PlainTextBody,
                    Encoding.UTF8,
                    "text/plain"));
        }

        // SmtpClient chưa hỗ trợ CancellationToken trực tiếp.
        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage);
    }
}
