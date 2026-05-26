using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record SendTextMessageCommand(Guid ConversationId, string Body)
    : ICommand<MessageDto>;
