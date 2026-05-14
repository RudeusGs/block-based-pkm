using Pkm.Domain.Common;

namespace Pkm.Domain.Users;

public sealed class User : EntityBase
{
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 255;
    public const int MaxUrlLength = 2000;

    public string UserName { get; private set; } = string.Empty;
    public string NormalizedUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }

    public UserStatus Status { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    // Transitional UI hint only. Real session state lives in RefreshTokens.
    public bool IsAuthenticated { get; private set; }

    private User()
    {
    }

    private User(
        Guid id,
        string userName,
        string email,
        string fullName,
        string? avatarUrl,
        string passwordHash,
        DateTimeOffset now)
        : base(id, now)
    {
        SetUserName(userName);
        SetEmail(email);

        FullName = TextRules.NormalizeRequired(fullName, MaxNameLength, nameof(FullName));
        AvatarUrl = TextRules.NormalizeOptional(avatarUrl, MaxUrlLength, nameof(AvatarUrl));
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? string.Empty : passwordHash;

        Status = UserStatus.Active;
        IsAuthenticated = false;
    }

    public static User Create(
        Guid id,
        string userName,
        string email,
        string fullName,
        string? avatarUrl,
        string rawPassword,
        IPasswordHasher passwordHasher,
        DateTimeOffset now)
    {
        if (passwordHasher is null)
            throw new DomainException("PasswordHasher không hợp lệ.");

        TextRules.ValidateStrongPassword(rawPassword);

        var hashedPassword = passwordHasher.HashPassword(rawPassword);

        return new User(
            id,
            userName,
            email,
            fullName,
            avatarUrl,
            hashedPassword,
            now);
    }

    public static string NormalizeUserName(string userName)
        => TextRules
            .NormalizeRequired(userName, MaxNameLength, nameof(UserName))
            .ToUpperInvariant();

    public static string NormalizeEmail(string email)
    {
        TextRules.ValidateEmail(email);

        return TextRules
            .NormalizeRequired(email, MaxEmailLength, nameof(Email))
            .ToUpperInvariant();
    }

    public bool IsActive() => Status == UserStatus.Active;

    public void ChangePassword(
        string newRawPassword,
        IPasswordHasher passwordHasher,
        DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (passwordHasher is null)
            throw new DomainException("PasswordHasher không hợp lệ.");

        TextRules.ValidateStrongPassword(newRawPassword);

        PasswordHash = passwordHasher.HashPassword(newRawPassword);
        Touch(now);
    }

    public void UpdateProfile(
        string fullName,
        string? avatarUrl,
        DateTimeOffset now)
    {
        ThrowIfDeleted();

        FullName = TextRules.NormalizeRequired(fullName, MaxNameLength, nameof(FullName));
        AvatarUrl = TextRules.NormalizeOptional(avatarUrl, MaxUrlLength, nameof(AvatarUrl));

        Touch(now);
    }

    public void Activate(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Status == UserStatus.Active)
            return;

        Status = UserStatus.Active;
        Touch(now);
    }

    public void Disable(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Status == UserStatus.Disabled)
            return;

        Status = UserStatus.Disabled;
        IsAuthenticated = false;

        Touch(now);
    }

    public void Lock(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Status == UserStatus.Locked)
            return;

        Status = UserStatus.Locked;
        IsAuthenticated = false;

        Touch(now);
    }

    public void SetAuthenticated(bool isAuthenticated, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (IsAuthenticated == isAuthenticated)
            return;

        IsAuthenticated = isAuthenticated;
        Touch(now);
    }

    private void SetUserName(string userName)
    {
        UserName = TextRules.NormalizeRequired(userName, MaxNameLength, nameof(UserName));
        NormalizedUserName = NormalizeUserName(UserName);
    }

    private void SetEmail(string email)
    {
        TextRules.ValidateEmail(email);

        Email = TextRules.NormalizeRequired(email, MaxEmailLength, nameof(Email));
        NormalizedEmail = NormalizeEmail(Email);
    }
}