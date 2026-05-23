using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Services;

/// <summary>
/// Lightweight semantic normalizer for task titles.
/// This is intentionally deterministic and offline so the recommendation engine
/// stays predictable in production, even without an external LLM API.
/// </summary>
internal static class TaskSemanticKeyBuilder
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "task",
        "todo",
        "viec",
        "cong",
        "can",
        "phai",
        "nhe",
        "nha",
        "nua",
        "lai",
        "lan",
        "buoi",
        "hom",
        "nay",
        "mai",
        "mot",
        "cai",
        "phan",
        "part",
        "phase",
        "step",
        "v",
        "version"
    };

    private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["on"] = "hoc",
        ["onlai"] = "hoc",
        ["review"] = "hoc",
        ["study"] = "hoc",
        ["learn"] = "hoc",
        ["lam"] = "xu_ly",
        ["xu"] = "xu_ly",
        ["xuly"] = "xu_ly",
        ["fix"] = "sua",
        ["sua"] = "sua",
        ["bug"] = "loi",
        ["loi"] = "loi",
        ["deadline"] = "han",
        ["due"] = "han"
    };

    public static string BuildTitleKey(RecommendationCandidateReadModel candidate)
        => BuildTitleKey(candidate.Title);

    public static string BuildTitleKey(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var normalized = RemoveDiacritics(title)
            .ToLowerInvariant()
            .Replace('đ', 'd');

        normalized = Regex.Replace(normalized, @"(?<=\p{L})(?=\p{N})|(?<=\p{N})(?=\p{L})", " ");
        normalized = Regex.Replace(normalized, @"[^\p{L}\p{N}#\+]+", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeToken)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !IsNoiseToken(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return tokens.Length == 0
            ? string.Empty
            : string.Join(' ', tokens);
    }

    public static bool IsSimilar(string firstKey, string secondKey)
    {
        if (string.IsNullOrWhiteSpace(firstKey) || string.IsNullOrWhiteSpace(secondKey))
            return false;

        if (string.Equals(firstKey, secondKey, StringComparison.OrdinalIgnoreCase))
            return true;

        var firstTokens = firstKey.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var secondTokens = secondKey.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (firstTokens.Count == 0 || secondTokens.Count == 0)
            return false;

        var intersection = firstTokens.Intersect(secondTokens, StringComparer.OrdinalIgnoreCase).Count();
        var union = firstTokens.Union(secondTokens, StringComparer.OrdinalIgnoreCase).Count();
        var similarity = union == 0 ? 0m : (decimal)intersection / union;

        if (similarity >= 0.82m)
            return true;

        var smaller = Math.Min(firstTokens.Count, secondTokens.Count);
        return smaller >= 2 && intersection == smaller;
    }

    private static string NormalizeToken(string token)
    {
        var clean = token.Trim().Trim('_', '-', '.', ',', ':', ';');

        if (clean.Length == 0)
            return string.Empty;

        clean = Regex.Replace(clean, @"^(so|no|num|number)$", string.Empty);
        clean = Regex.Replace(clean, @"^(p|part|phan|chuong|chapter)\d+$", string.Empty);
        clean = Regex.Replace(clean, @"^v\d+$", string.Empty);

        if (Synonyms.TryGetValue(clean, out var mapped))
            return mapped;

        return clean;
    }

    private static bool IsNoiseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        if (StopWords.Contains(token))
            return true;

        if (token.All(char.IsDigit))
            return true;

        if (Regex.IsMatch(token, @"^\d+[a-z]?$", RegexOptions.IgnoreCase))
            return true;

        return false;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(capacity: normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
