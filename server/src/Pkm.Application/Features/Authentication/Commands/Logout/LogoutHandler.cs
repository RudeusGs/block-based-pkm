using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly LogoutCommandValidator _validator;

    public LogoutHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork,
        IClock clock,
        LogoutCommandValidator validator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure(
                AuthenticationErrors.InvalidLogoutRequest(validationErrors));
        }

        var tokenHash = _refreshTokenService.Hash(request.RefreshToken);

        var refreshToken = await _refreshTokenRepository.GetByTokenHashForUpdateAsync(
            tokenHash,
            cancellationToken);

        if (refreshToken is null)
            return Result.Success();

        refreshToken.Revoke(_clock.UtcNow, request.IpAddress);
        _refreshTokenRepository.Update(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
