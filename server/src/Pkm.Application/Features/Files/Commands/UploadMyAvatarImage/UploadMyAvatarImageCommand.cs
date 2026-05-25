using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Models;

namespace Pkm.Application.Features.Files.Commands.UploadMyAvatarImage;

public sealed record UploadMyAvatarImageCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<UserProfileDto>;
