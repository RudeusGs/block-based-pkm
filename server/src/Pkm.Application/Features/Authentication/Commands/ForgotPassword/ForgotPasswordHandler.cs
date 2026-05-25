using System.Net;
using System.Security.Cryptography;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Email;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Users;

namespace Pkm.Application.Features.Authentication.Commands.ForgotPassword;

public sealed class ForgotPasswordHandler : ICommandHandler<ForgotPasswordCommand>
{
    private const int TemporaryPasswordLength = 8;
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@$%*?";
    private const string All = Upper + Lower + Digits + Symbols;

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ForgotPasswordCommandValidator _validator;

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IClock clock,
        ForgotPasswordCommandValidator validator)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
            return Result.Failure(AuthenticationErrors.InvalidForgotPasswordRequest(validationErrors));

        User? user;
        try
        {
            user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);
        }
        catch (DomainException)
        {
            return Result.Failure(AuthenticationErrors.InvalidForgotPasswordRequest(new[] { "Email không đúng định dạng." }));
        }

        // Không tiết lộ email có tồn tại hay không. Nếu không có tài khoản thì vẫn trả Success.
        if (user is null || !user.IsActive())
            return Result.Success();

        var now = _clock.UtcNow;
        var temporaryPassword = GenerateStrongPassword();
        var temporaryPasswordHash = _passwordHasher.HashPassword(temporaryPassword);

        user.ChangePassword(temporaryPasswordHash, now);
        user.SetAuthenticated(false, now);
        _userRepository.Update(user);

        var activeTokens = await _refreshTokenRepository.ListActiveByUserForUpdateAsync(user.Id, now, cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke(now, request.IpAddress);
            _refreshTokenRepository.Update(token);
        }

        await _emailSender.SendAsync(
            BuildForgotPasswordEmail(user, temporaryPassword),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static EmailMessage BuildForgotPasswordEmail(User user, string temporaryPassword)
    {
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName);
        var safePassword = WebUtility.HtmlEncode(temporaryPassword);

        var plainText = $"""
Xin chào {user.FullName},

Mật khẩu tạm thời của bạn là: {temporaryPassword}

Vui lòng đăng nhập và đổi mật khẩu ngay trong phần Profile để bảo vệ tài khoản.
Nếu bạn không yêu cầu đặt lại mật khẩu, hãy đổi mật khẩu ngay sau khi đăng nhập.
""";

        var html = $"""
<p>Xin chào <strong>{safeName}</strong>,</p>
<p>Mật khẩu tạm thời của bạn là:</p>
<p style="font-size:20px;font-weight:700;letter-spacing:2px">{safePassword}</p>
<p>Vui lòng đăng nhập và đổi mật khẩu ngay trong phần Profile để bảo vệ tài khoản.</p>
<p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy đổi mật khẩu ngay sau khi đăng nhập.</p>
""";

        return new EmailMessage(
            user.Email,
            "Mật khẩu tạm thời cho tài khoản PKM",
            plainText,
            html);
    }

    private static string GenerateStrongPassword()
    {
        var chars = new[]
        {
            Pick(Upper),
            Pick(Lower),
            Pick(Digits),
            Pick(Symbols),
            Pick(All),
            Pick(All),
            Pick(All),
            Pick(All)
        };

        Shuffle(chars);
        return new string(chars);
    }

    private static char Pick(string alphabet)
        => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

    private static void Shuffle(char[] chars)
    {
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
