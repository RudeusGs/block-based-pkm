namespace Pkm.Application.Common.Results;

public sealed class Error
{
    public string Code { get; }
    public string Message { get; }
    public ResultStatus Status { get; }
    public IReadOnlyList<string> Details { get; }

    public Error(
        string code,
        string message,
        ResultStatus status = ResultStatus.Failure,
        IReadOnlyList<string>? details = null)
    {
        Code = code;
        Message = message;
        Status = status;
        Details = details ?? Array.Empty<string>();
    }

    public static readonly Error None = new(string.Empty, string.Empty, ResultStatus.None);
    public static readonly Error NullValue = new(
        "Error.NullValue",
        "The specified result value is null.",
        ResultStatus.Unprocessable);
}
