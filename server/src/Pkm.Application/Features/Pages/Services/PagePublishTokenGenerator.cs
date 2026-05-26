using System.Security.Cryptography;

namespace Pkm.Application.Features.Pages.Services;

public sealed class PagePublishTokenGenerator : IPagePublishTokenGenerator
{
    private const int TokenByteLength = 32;

    public string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[TokenByteLength];
        RandomNumberGenerator.Fill(bytes);

        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
