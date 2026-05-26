using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record SendWorkspaceShareMessageCommand(
    Guid ConversationId,
    Guid WorkspaceId,
    string Role) : ICommand<MessageDto>;
