namespace Pkm.Api.Extensions;

public static class CorsServiceCollection
{
    public const string PolicyName = "AllowFrontend";

    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Select(NormalizeOrigin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        if (allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins is required because credentials are enabled.");
        }

        ValidateOrigins(allowedOrigins);

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static string NormalizeOrigin(string? origin)
        => origin?.Trim().TrimEnd('/') ?? string.Empty;

    private static void ValidateOrigins(IReadOnlyCollection<string> origins)
    {
        var invalidOrigins = origins
            .Where(IsInvalidOrigin)
            .ToArray();

        if (invalidOrigins.Length > 0)
        {
            throw new InvalidOperationException(
                $"Invalid Cors:AllowedOrigins value(s): {string.Join(", ", invalidOrigins)}. " +
                "Use absolute http/https origins only, for example: https://app.example.com");
        }
    }

    private static bool IsInvalidOrigin(string origin)
    {
        if (origin == "*" || origin.Contains('*', StringComparison.Ordinal))
            return true;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return true;

        if (uri.Scheme is not ("http" or "https"))
            return true;

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            return true;

        return uri.AbsolutePath != "/";
    }
}
