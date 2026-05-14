namespace Pkm.Api.Contracts.Requests.Account;

public sealed record ChangeMyPasswordRequest(
    string CurrentPassword,
    string NewPassword);