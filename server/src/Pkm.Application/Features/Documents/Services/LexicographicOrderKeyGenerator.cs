using Pkm.Domain.Common;

namespace Pkm.Application.Features.Documents.Services;

public sealed class LexicographicOrderKeyGenerator : IOrderKeyGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const char MinChar = '0';
    private const char MaxChar = 'z';

    public string CreateFirst() => "U";

    public string CreateLast(string? previous)
    {
        if (string.IsNullOrWhiteSpace(previous))
            return CreateFirst();

        return previous + "U";
    }

    public string CreateBetween(string? previous, string? next)
    {
        if (string.IsNullOrWhiteSpace(previous) && string.IsNullOrWhiteSpace(next))
            return CreateFirst();

        if (string.IsNullOrWhiteSpace(previous))
            return PrefixBefore(next!);

        if (string.IsNullOrWhiteSpace(next))
            return CreateLast(previous);

        if (string.CompareOrdinal(previous, next) >= 0)
            throw new DomainException("OrderKey không hợp lệ: previous phải nhỏ hơn next.");

        return Mid(previous, next);
    }

    private static string PrefixBefore(string next)
    {
        if (string.IsNullOrWhiteSpace(next))
            return "U";

        return Mid(string.Empty, next);
    }

    private static string Mid(string left, string right)
    {
        var result = string.Empty;
        var index = 0;

        while (true)
        {
            var leftChar = index < left.Length ? left[index] : MinChar;
            var rightChar = index < right.Length ? right[index] : MaxChar;

            var leftPos = Alphabet.IndexOf(leftChar);
            var rightPos = Alphabet.IndexOf(rightChar);

            if (leftPos < 0 || rightPos < 0)
                throw new DomainException("OrderKey chứa ký tự không hợp lệ.");

            if (rightPos - leftPos > 1)
            {
                var middlePos = (leftPos + rightPos) / 2;
                return result + Alphabet[middlePos];
            }

            result += leftChar;
            index++;
        }
    }
}