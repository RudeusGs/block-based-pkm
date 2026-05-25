namespace Pkm.Application.Common.Validation;

public sealed record ValidationResult(IReadOnlyList<ValidationFailure> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static ValidationResult Success { get; } = new(Array.Empty<ValidationFailure>());

    public static ValidationResult Failure(IEnumerable<ValidationFailure> errors)
        => new(errors.ToArray());
}
