namespace Pkm.Domain.SharedKernel;

public static class TextRules
{
    public static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} is required.");

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new DomainException($"{fieldName} must not exceed {maxLength} characters.");

        return trimmed;
    }

    public static string NormalizeOrDefault(string? value, string defaultValue, int maxLength, string fieldName)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();

        if (normalized.Length > maxLength)
            throw new DomainException($"{fieldName} must not exceed {maxLength} characters.");

        return normalized;
    }

    public static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new DomainException($"{fieldName} must not exceed {maxLength} characters.");

        return trimmed;
    }

    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!emailRegex.IsMatch(email))
            throw new DomainException("Email format is invalid.");
    }

    public static void ValidateStrongPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new DomainException("Password is required.");

        if (plainPassword.Length < 6)
            throw new DomainException("Password must contain at least 6 characters.");

        if (!plainPassword.Any(char.IsUpper))
            throw new DomainException("Password must contain at least one uppercase letter.");

        if (!plainPassword.Any(char.IsLower))
            throw new DomainException("Password must contain at least one lowercase letter.");

        if (!plainPassword.Any(char.IsDigit))
            throw new DomainException("Password must contain at least one digit.");

        if (!plainPassword.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new DomainException("Password must contain at least one special character.");
    }
}
