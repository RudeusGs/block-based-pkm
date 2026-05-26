using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Files.Commands.UploadWorkspaceAvatarImage;

public sealed record UploadWorkspaceAvatarImageCommand(
    Guid WorkspaceId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<WorkspaceDto>;
