using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record PinMessageCommand(Guid MessageId)
    : ICommand<MessageDto>;
