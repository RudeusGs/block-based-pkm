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
        ValidateOptions();
        ValidateMessage(message);

        using var smtpClient = CreateSmtpClient();
        using var mailMessage = CreateMailMessage(message);

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("Email:Smtp:Host chưa được cấu hình.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("Email:Smtp:FromEmail chưa được cấu hình.");
        }
    }

    private static void ValidateMessage(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.ToEmail))
        {
            throw new InvalidOperationException("Email người nhận không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            throw new InvalidOperationException("Tiêu đề email không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(message.PlainTextBody)
            && string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            throw new InvalidOperationException("Nội dung email không được để trống.");
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.UserName,
                _options.Password);
        }

        return smtpClient;
    }

    private MailMessage CreateMailMessage(EmailMessage message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName, Encoding.UTF8),
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
        };

        mailMessage.To.Add(new MailAddress(message.ToEmail));

        AddBody(mailMessage, message);

        return mailMessage;
    }

    private static void AddBody(MailMessage mailMessage, EmailMessage message)
    {
        var plainTextBody = string.IsNullOrWhiteSpace(message.PlainTextBody)
            ? ConvertHtmlToSimplePlainText(message.HtmlBody!)
            : message.PlainTextBody;

        if (string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mailMessage.Body = plainTextBody;
            mailMessage.IsBodyHtml = false;
            return;
        }

        mailMessage.Body = message.HtmlBody;
        mailMessage.IsBodyHtml = true;
        var plainTextView = AlternateView.CreateAlternateViewFromString(
            plainTextBody,
            Encoding.UTF8,
            "text/plain");

        var htmlView = AlternateView.CreateAlternateViewFromString(
            message.HtmlBody,
            Encoding.UTF8,
            "text/html");

        mailMessage.AlternateViews.Add(plainTextView);
        mailMessage.AlternateViews.Add(htmlView);
    }

    private static string ConvertHtmlToSimplePlainText(string html)
        => html
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase);
}
