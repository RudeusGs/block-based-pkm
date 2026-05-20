using System.Net;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Email;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Application.Features.Workspaces.Services;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public sealed class AddWorkspaceMemberHandler
{
    private const string ProductName = "Block Paged";
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceInvitationRepository _workspaceInvitationRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly AddWorkspaceMemberCommandValidator _validator;
    private readonly IEmailSender _emailSender;
    private readonly IWorkspaceInvitationLinkFactory _workspaceInvitationLinkFactory;
    private readonly INotificationService _notificationService;

    public AddWorkspaceMemberHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceInvitationRepository workspaceInvitationRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        AddWorkspaceMemberCommandValidator validator,
        IEmailSender emailSender,
        IWorkspaceInvitationLinkFactory workspaceInvitationLinkFactory,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceInvitationRepository = workspaceInvitationRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
        _emailSender = emailSender;
        _workspaceInvitationLinkFactory = workspaceInvitationLinkFactory;
        _notificationService = notificationService;
    }

    public async Task<Result<WorkspaceInvitationDto>> HandleAsync(
        AddWorkspaceMemberCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.InvalidAddMemberRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanManageMembers)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceManageMembersForbidden);
        }

        if (request.Role == WorkspaceRole.Owner)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.CannotAssignOwnerRole);
        }

        var normalizedEmail = WorkspaceInvitation.NormalizeEmail(request.Email);

        var targetUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (targetUser is null)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.TargetUserNotFoundByEmail(normalizedEmail));
        }

        if (!targetUser.IsActive())
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.TargetUserAccountNotActive(normalizedEmail));
        }

        if (targetUser.Id == currentUserId)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.CannotManageYourself);
        }

        var isAlreadyMember = await _workspaceMemberRepository.ExistsAsync(
            request.WorkspaceId,
            targetUser.Id,
            cancellationToken);

        if (isAlreadyMember)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceMemberAlreadyExists);
        }

        var now = _clock.UtcNow;

        var pendingInvitation = await _workspaceInvitationRepository.GetPendingByWorkspaceAndEmailAsync(
            request.WorkspaceId,
            normalizedEmail,
            now,
            cancellationToken);

        if (pendingInvitation is not null)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceInvitationAlreadyPending);
        }

        var workspace = await _workspaceRepository.GetDetailAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        var rawToken = WorkspaceInvitationToken.Create();
        var tokenHash = WorkspaceInvitationToken.Hash(rawToken);
        var expiresAtUtc = now.Add(InvitationLifetime);

        var invitation = WorkspaceInvitation.Create(
            request.WorkspaceId,
            normalizedEmail,
            request.Role,
            currentUserId,
            tokenHash,
            expiresAtUtc,
            now);

        _workspaceInvitationRepository.Add(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var acceptLink = _workspaceInvitationLinkFactory.CreateAcceptLink(rawToken);
        var inviterDisplayName = _currentUser.UserName
            ?? _currentUser.Email
            ?? "Một thành viên";

        try
        {
            var email = BuildInvitationEmail(
                invitation.Email,
                workspace.Name,
                inviterDisplayName,
                request.Role,
                acceptLink,
                expiresAtUtc);

            await _emailSender.SendAsync(email, cancellationToken);
        }
        catch
        {
            invitation.SoftDelete(_clock.UtcNow);
            _workspaceInvitationRepository.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceInvitationEmailFailed);
        }

        try
        {
            await _notificationService.NotifyAsync(
                targetUser.Id,
                NotificationTemplates.WorkspaceInvited(
                    actorUserId: currentUserId,
                    actorDisplayName: inviterDisplayName,
                    workspaceId: workspace.Id,
                    workspaceName: workspace.Name,
                    role: request.Role),
                cancellationToken);
        }
        catch
        {

        }

        return Result.Success(ToDto(invitation));
    }

    private static EmailMessage BuildInvitationEmail(
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

    private static WorkspaceInvitationDto ToDto(WorkspaceInvitation invitation)
        => new(
            invitation.Id,
            invitation.WorkspaceId,
            invitation.Email,
            invitation.Role,
            invitation.InvitedByUserId,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.CreatedDate,
            invitation.UpdatedDate);
}