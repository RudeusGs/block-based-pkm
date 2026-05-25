namespace Pkm.Application.Common.Validation;

public sealed record ValidationFailure(
    string Code,
    string Message,
    string? PropertyName = null);
