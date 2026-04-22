using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Documents.Services;

public interface IBlockPayloadValidator
{
    Error? ValidatePropsJson(string? propsJson);
}