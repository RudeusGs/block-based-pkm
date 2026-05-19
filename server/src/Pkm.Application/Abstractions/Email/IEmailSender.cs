namespace Pkm.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string PlainTextBody,
    string? HtmlBody = null);
