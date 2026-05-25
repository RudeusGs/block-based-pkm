using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Workspaces;

/// <summary>
/// WorkspaceInvitation: lời mời tham gia workspace qua email.
/// Member chỉ được tạo sau khi người nhận bấm link xác nhận trong email.
/// </summary>
public sealed class WorkspaceInvitation : EntityBase
{
    public const int MaxEmailLength = 255;
    public const int MaxTokenHashLength = 128;

    public Guid WorkspaceId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public WorkspaceRole Role { get; private set; }

    public Guid InvitedByUserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }

    private WorkspaceInvitation() { }

    private WorkspaceInvitation(
        Guid id,
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        Guid invitedByUserId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset now) : base(id, now)
    {
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));
        DomainGuard.AgainstEmpty(invitedByUserId, nameof(invitedByUserId));

        TextRules.ValidateEmail(email);

        if (!Enum.IsDefined(typeof(WorkspaceRole), role))
            throw new DomainException("Vai trò không hợp lệ.");

        if (role == WorkspaceRole.Owner)
            throw new DomainException("Không thể mời người khác với vai trò Owner.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("TokenHash không được để trống.");

        if (tokenHash.Trim().Length > MaxTokenHashLength)
            throw new DomainException($"TokenHash không được vượt quá {MaxTokenHashLength} ký tự.");

        if (expiresAtUtc <= now)
            throw new DomainException("Thời hạn lời mời không hợp lệ.");

        WorkspaceId = workspaceId;
        Email = email.Trim();
        NormalizedEmail = NormalizeEmail(email);
        Role = role;
        InvitedByUserId = invitedByUserId;
        TokenHash = tokenHash.Trim();
        ExpiresAtUtc = expiresAtUtc;
    }

    public static WorkspaceInvitation Create(
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        Guid invitedByUserId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset now)
        => new(
            Guid.NewGuid(),
            workspaceId,
            email,
            role,
            invitedByUserId,
            tokenHash,
            expiresAtUtc,
            now);

    public bool IsAccepted => AcceptedAtUtc.HasValue;

    public bool IsExpired(DateTimeOffset utcNow)
        => utcNow >= ExpiresAtUtc;

    public bool IsPendingAt(DateTimeOffset utcNow)
        => !IsAccepted && !IsExpired(utcNow);

    public void Accept(Guid acceptedByUserId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(acceptedByUserId, nameof(acceptedByUserId));

        if (IsAccepted)
            throw new DomainException("Lời mời đã được xác nhận.");

        if (IsExpired(now))
            throw new DomainException("Lời mời đã hết hạn.");

        AcceptedByUserId = acceptedByUserId;
        AcceptedAtUtc = now;
        Touch(now);
    }

    public static string NormalizeEmail(string email)
    {
        TextRules.ValidateEmail(email);
        return email.Trim().ToUpperInvariant();
    }
}
