using System.Net;
using Pkm.Application.Common.Abstractions.Email;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

internal static class WorkspaceInvitationEmailFactory
{
    private const string ProductName = "Block Paged";

    public static EmailMessage Create(
        string toEmail,
        string workspaceName,
        string inviterDisplayName,
        WorkspaceRole role,
        string acceptLink,
        DateTimeOffset expiresAtUtc)
    {
        var roleDisplayName = ToWorkspaceRoleDisplayName(role);
        var expiresAtText = expiresAtUtc.ToString("dd/MM/yyyy HH:mm 'UTC'");
        var subject = $"Lời mời tham gia workspace {workspaceName}";

        var plainText = BuildPlainTextInvitationEmail(
            toEmail,
            workspaceName,
            inviterDisplayName,
            roleDisplayName,
            acceptLink,
            expiresAtText);

        var html = BuildHtmlInvitationEmail(
            subject,
            toEmail,
            workspaceName,
            inviterDisplayName,
            roleDisplayName,
            acceptLink,
            expiresAtText);

        return new EmailMessage(
            ToEmail: toEmail,
            Subject: subject,
            PlainTextBody: plainText,
            HtmlBody: html);
    }

    private static string BuildPlainTextInvitationEmail(
        string toEmail,
        string workspaceName,
        string inviterDisplayName,
        string roleDisplayName,
        string acceptLink,
        string expiresAtText)
        => $"""
        Xin chào,

        {inviterDisplayName} đã mời bạn tham gia workspace "{workspaceName}" trên {ProductName}.

        Thông tin lời mời:
        - Workspace: {workspaceName}
        - Người mời: {inviterDisplayName}
        - Vai trò: {roleDisplayName}
        - Hết hạn: {expiresAtText}

        Bấm vào link dưới đây để xác nhận tham gia:
        {acceptLink}

        Lưu ý:
        Vui lòng đăng nhập bằng đúng email {toEmail} để xác nhận lời mời.
        Nếu bạn không mong đợi lời mời này, bạn có thể bỏ qua email này.

        Trân trọng,
        {ProductName}
        """;

    private static string BuildHtmlInvitationEmail(
        string subject,
        string toEmail,
        string workspaceName,
        string inviterDisplayName,
        string roleDisplayName,
        string acceptLink,
        string expiresAtText)
    {
        var safeSubject = Html(subject);
        var safeToEmail = Html(toEmail);
        var safeWorkspaceName = Html(workspaceName);
        var safeInviter = Html(inviterDisplayName);
        var safeRole = Html(roleDisplayName);
        var safeLink = Html(acceptLink);
        var safeExpiresAt = Html(expiresAtText);

        return $"""
        <!doctype html>
        <html lang="vi">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{safeSubject}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;color:#111827;">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">
                {safeInviter} đã mời bạn tham gia workspace {safeWorkspaceName} trên {ProductName}.
            </div>

            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f3f4f6;margin:0;padding:32px 12px;">
                <tr>
                    <td align="center">
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:640px;background-color:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e5e7eb;">
                            <tr>
                                <td style="padding:28px 32px;background-color:#111827;color:#ffffff;">
                                    <div style="font-size:14px;letter-spacing:0.08em;text-transform:uppercase;color:#d1d5db;font-weight:700;">
                                        {ProductName}
                                    </div>
                                    <h1 style="margin:12px 0 0;font-size:24px;line-height:1.35;font-weight:700;">
                                        Bạn được mời tham gia workspace
                                    </h1>
                                </td>
                            </tr>

                            <tr>
                                <td style="padding:32px;">
                                    <p style="margin:0 0 16px;font-size:16px;line-height:1.65;color:#374151;">
                                        Xin chào,
                                    </p>

                                    <p style="margin:0 0 24px;font-size:16px;line-height:1.65;color:#374151;">
                                        <strong style="color:#111827;">{safeInviter}</strong> đã mời bạn tham gia workspace
                                        <strong style="color:#111827;">{safeWorkspaceName}</strong> trên {ProductName}.
                                    </p>

                                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:0 0 28px;background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:12px;">
                                        <tr>
                                            <td style="padding:18px 20px;">
                                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                                                    <tr>
                                                        <td style="padding:8px 0;font-size:14px;color:#6b7280;width:140px;">Workspace</td>
                                                        <td style="padding:8px 0;font-size:14px;color:#111827;font-weight:700;">{safeWorkspaceName}</td>
                                                    </tr>
                                                    <tr>
                                                        <td style="padding:8px 0;font-size:14px;color:#6b7280;width:140px;">Người mời</td>
                                                        <td style="padding:8px 0;font-size:14px;color:#111827;">{safeInviter}</td>
                                                    </tr>
                                                    <tr>
                                                        <td style="padding:8px 0;font-size:14px;color:#6b7280;width:140px;">Vai trò</td>
                                                        <td style="padding:8px 0;font-size:14px;color:#111827;">{safeRole}</td>
                                                    </tr>
                                                    <tr>
                                                        <td style="padding:8px 0;font-size:14px;color:#6b7280;width:140px;">Hết hạn</td>
                                                        <td style="padding:8px 0;font-size:14px;color:#111827;">{safeExpiresAt}</td>
                                                    </tr>
                                                </table>
                                            </td>
                                        </tr>
                                    </table>

                                    <table role="presentation" cellspacing="0" cellpadding="0" style="margin:0 0 28px;">
                                        <tr>
                                            <td bgcolor="#2563eb" style="border-radius:10px;">
                                                <a href="{safeLink}" style="display:inline-block;padding:14px 22px;font-size:15px;line-height:1.2;color:#ffffff;text-decoration:none;font-weight:700;border-radius:10px;">
                                                    Xác nhận tham gia workspace
                                                </a>
                                            </td>
                                        </tr>
                                    </table>

                                    <p style="margin:0 0 12px;font-size:14px;line-height:1.6;color:#6b7280;">
                                        Nếu nút trên không hoạt động, hãy copy và mở đường dẫn sau trong trình duyệt:
                                    </p>

                                    <p style="margin:0 0 24px;font-size:13px;line-height:1.6;word-break:break-all;color:#2563eb;">
                                        <a href="{safeLink}" style="color:#2563eb;text-decoration:underline;">{safeLink}</a>
                                    </p>

                                    <div style="padding:16px 18px;background-color:#fffbeb;border:1px solid #fde68a;border-radius:12px;margin:0 0 24px;">
                                        <p style="margin:0;font-size:14px;line-height:1.6;color:#92400e;">
                                            Vui lòng đăng nhập bằng đúng email
                                            <strong>{safeToEmail}</strong> để xác nhận lời mời.
                                        </p>
                                    </div>

                                    <p style="margin:0;font-size:14px;line-height:1.6;color:#6b7280;">
                                        Nếu bạn không mong đợi lời mời này, bạn có thể bỏ qua email này.
                                    </p>
                                </td>
                            </tr>

                            <tr>
                                <td style="padding:22px 32px;background-color:#f9fafb;border-top:1px solid #e5e7eb;">
                                    <p style="margin:0;font-size:13px;line-height:1.6;color:#6b7280;">
                                        Email này được gửi tự động bởi {ProductName}. Vui lòng không trả lời trực tiếp email này.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string ToWorkspaceRoleDisplayName(WorkspaceRole role)
        => role switch
        {
            WorkspaceRole.Manager => "Quản lý",
            WorkspaceRole.Member => "Thành viên",
            WorkspaceRole.Viewer => "Người xem",
            WorkspaceRole.Owner => "Chủ sở hữu",
            _ => role.ToString()
        };

    private static string Html(string value)
        => WebUtility.HtmlEncode(value);
}
