namespace Pkm.Api.Contracts.Requests.Auth;

public sealed record LoginRequest(
    string UserName,
    string Password);