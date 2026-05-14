using Pkm.Domain.Common;

namespace Pkm.Domain.Users;

public sealed class RefreshToken : EntityBase
{
    public const int MaxTokenHashLength = 128;
    public const int MaxIpAddressLength = 128;

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset now,
        string? createdByIp)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Refresh token hash không được để trống.");

        if (expiresAtUtc <= now)
            throw new DomainException("Refresh token expiry không hợp lệ.");

        UserId = userId;
        TokenHash = TextRules.NormalizeRequired(
            tokenHash,
            MaxTokenHashLength,
            nameof(TokenHash));

        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = TextRules.NormalizeOptional(
            createdByIp,
            MaxIpAddressLength,
            nameof(CreatedByIp));
    }

    public static RefreshToken Create(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset now,
        string? createdByIp)
        => new(
            id,
            userId,
            tokenHash,
            expiresAtUtc,
            now,
            createdByIp);

    public void Revoke(
        DateTimeOffset now,
        string? revokedByIp = null,
        string? replacedByTokenHash = null)
    {
        ThrowIfDeleted();

        if (IsRevoked)
            return;

        RevokedAtUtc = now;
        RevokedByIp = TextRules.NormalizeOptional(
            revokedByIp,
            MaxIpAddressLength,
            nameof(RevokedByIp));

        ReplacedByTokenHash = TextRules.NormalizeOptional(
            replacedByTokenHash,
            MaxTokenHashLength,
            nameof(ReplacedByTokenHash));

        Touch(now);
    }
}