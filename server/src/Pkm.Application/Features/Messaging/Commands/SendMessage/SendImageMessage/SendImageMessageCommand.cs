using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record SendImageMessageCommand(
    Guid ConversationId,
    string? Caption,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<MessageDto>;
