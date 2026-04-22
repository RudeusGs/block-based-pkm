using Pkm.Domain.Common;

namespace Pkm.Domain.Users;

public sealed class User : EntityBase
{
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 255;
    public const int MaxUrlLength = 2000;
    public const int MaxStatusLength = 50;

    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsAuthenticated { get; private set; }

    private User() { }

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
        UserName = TextRules.NormalizeRequired(userName, MaxNameLength, nameof(UserName));
        TextRules.ValidateEmail(email);
        Email = TextRules.NormalizeRequired(email, MaxEmailLength, nameof(Email)); 
        FullName = TextRules.NormalizeRequired(fullName, MaxNameLength, nameof(FullName));
        AvatarUrl = TextRules.NormalizeOptional(avatarUrl, MaxUrlLength, nameof(AvatarUrl));
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? string.Empty : passwordHash;
        Status = "Active";
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
        TextRules.ValidateStrongPassword(rawPassword);
        string hashed = passwordHasher.HashPassword(rawPassword);
        return new User(id, userName, email, fullName, avatarUrl, hashed, now);
    }

    public void ChangePassword(string newRawPassword, IPasswordHasher passwordHasher, DateTimeOffset now)
    {
        ThrowIfDeleted();

        TextRules.ValidateStrongPassword(newRawPassword);
        PasswordHash = passwordHasher.HashPassword(newRawPassword);
        Touch(now);
    }

    public void UpdateProfile(string fullName, string? avatarUrl, DateTimeOffset now)
    {
        ThrowIfDeleted();

        FullName = TextRules.NormalizeRequired(fullName, MaxNameLength, nameof(FullName));
        AvatarUrl = TextRules.NormalizeOptional(avatarUrl, MaxUrlLength, nameof(AvatarUrl));
        Touch(now);
    }

    public void ChangeStatus(string newStatus, DateTimeOffset now)
    {
        ThrowIfDeleted();

        Status = TextRules.NormalizeRequired(newStatus, MaxStatusLength, nameof(Status));
        Touch(now);
    }

    public void SetAuthenticated(bool isAuthenticated, DateTimeOffset now)
    {
        ThrowIfDeleted();

        IsAuthenticated = isAuthenticated;
        Touch(now);
    }
}
