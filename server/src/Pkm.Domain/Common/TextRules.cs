namespace Pkm.Domain.Common;

public static class TextRules
{
    public static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} không được để trống.");

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new DomainException($"{fieldName} không được vượt quá {maxLength} ký tự.");

        return trimmed;
    }

    public static string NormalizeOrDefault(string? value, string defaultValue, int maxLength, string fieldName)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();

        if (normalized.Length > maxLength)
            throw new DomainException($"{fieldName} không được vượt quá {maxLength} ký tự.");

        return normalized;
    }

    public static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new DomainException($"{fieldName} không được vượt quá {maxLength} ký tự.");

        return trimmed;
    }

    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email không được để trống.");

        var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!emailRegex.IsMatch(email))
            throw new DomainException("Email không đúng định dạng (yêu cầu chứa '@' và có dấu chấm).");
    }

    public static void ValidateStrongPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new DomainException("Mật khẩu không được để trống.");

        if (plainPassword.Length < 6)
            throw new DomainException("Mật khẩu phải dài ít nhất 6 ký tự.");

        if (!plainPassword.Any(char.IsUpper))
            throw new DomainException("Mật khẩu phải chứa ít nhất 1 chữ hoa.");

        if (!plainPassword.Any(char.IsLower))
            throw new DomainException("Mật khẩu phải chứa ít nhất 1 chữ thường.");

        if (!plainPassword.Any(char.IsDigit))
            throw new DomainException("Mật khẩu phải chứa ít nhất 1 chữ số.");

        if (!plainPassword.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new DomainException("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt.");
    }
}