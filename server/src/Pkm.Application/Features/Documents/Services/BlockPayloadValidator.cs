using System.Text.Json;
using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Documents.Services;

public sealed class BlockPayloadValidator : IBlockPayloadValidator
{
    public Error? ValidatePropsJson(string? propsJson)
    {
        if (string.IsNullOrWhiteSpace(propsJson))
            return null;

        try
        {
            using var _ = JsonDocument.Parse(propsJson);
            return null;
        }
        catch (JsonException)
        {
            return new Error(
                "Document.InvalidPropsJson",
                "PropsJson phải là JSON hợp lệ.",
                ResultStatus.Validation);
        }
    }
}