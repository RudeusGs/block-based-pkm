using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record UnpinMessageCommand(Guid MessageId)
    : ICommand<MessageDto>;
