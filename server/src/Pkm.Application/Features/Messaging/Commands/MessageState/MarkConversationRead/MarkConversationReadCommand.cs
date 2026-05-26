using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record MarkConversationReadCommand(Guid ConversationId)
    : ICommand;
