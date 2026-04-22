namespace Pkm.Application.Common.Results;

public enum ResultStatus
{
    None = 0,
    Validation = 1,
    Unauthorized = 2,
    Forbidden = 3,
    NotFound = 4,
    Conflict = 5,
    Unprocessable = 6,
    Failure = 7
}
