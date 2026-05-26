using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Workspaces;

/// <summary>
/// Email invitation to join a workspace.
/// Membership is created only after the recipient accepts the invitation link.
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
            throw new DomainException("Workspace invitation role is invalid.");

        if (role == WorkspaceRole.Owner)
            throw new DomainException("Workspace invitations cannot grant the Owner role.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash is required.");

        if (tokenHash.Trim().Length > MaxTokenHashLength)
            throw new DomainException($"Token hash must not exceed {MaxTokenHashLength} characters.");

        if (expiresAtUtc <= now)
            throw new DomainException("Workspace invitation expiry is invalid.");

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
            throw new DomainException("Workspace invitation has already been accepted.");

        if (IsExpired(now))
            throw new DomainException("Workspace invitation has expired.");

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
