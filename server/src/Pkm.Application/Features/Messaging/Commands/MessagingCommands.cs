using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Messaging.Services;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record CreateDirectConversationCommand(Guid RecipientUserId)
    : ICommand<ConversationDto>;

public sealed class CreateDirectConversationHandler
    : ICommandHandler<CreateDirectConversationCommand, ConversationDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public CreateDirectConversationHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<ConversationDto>> HandleAsync(
        CreateDirectConversationCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.CreateOrGetDirectConversationAsync(
            command.RecipientUserId,
            cancellationToken);
}

public sealed record SendTextMessageCommand(Guid ConversationId, string Body)
    : ICommand<MessageDto>;

public sealed class SendTextMessageHandler
    : ICommandHandler<SendTextMessageCommand, MessageDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public SendTextMessageHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<MessageDto>> HandleAsync(
        SendTextMessageCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.SendTextMessageAsync(
            command.ConversationId,
            command.Body,
            cancellationToken);
}

public sealed record SendImageMessageCommand(
    Guid ConversationId,
    string? Caption,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<MessageDto>;

public sealed class SendImageMessageHandler
    : ICommandHandler<SendImageMessageCommand, MessageDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public SendImageMessageHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<MessageDto>> HandleAsync(
        SendImageMessageCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.SendImageMessageAsync(
            command.ConversationId,
            command.Caption,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            command.Content,
            cancellationToken);
}

public sealed record SendWorkspaceShareMessageCommand(
    Guid ConversationId,
    Guid WorkspaceId,
    string Role) : ICommand<MessageDto>;

public sealed class SendWorkspaceShareMessageHandler
    : ICommandHandler<SendWorkspaceShareMessageCommand, MessageDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public SendWorkspaceShareMessageHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<MessageDto>> HandleAsync(
        SendWorkspaceShareMessageCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.SendWorkspaceShareMessageAsync(
            command.ConversationId,
            command.WorkspaceId,
            command.Role,
            cancellationToken);
}

public sealed record AcceptWorkspaceShareCommand(Guid MessageId)
    : ICommand<WorkspaceDto>;

public sealed class AcceptWorkspaceShareHandler
    : ICommandHandler<AcceptWorkspaceShareCommand, WorkspaceDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public AcceptWorkspaceShareHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<WorkspaceDto>> HandleAsync(
        AcceptWorkspaceShareCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.AcceptWorkspaceShareAsync(
            command.MessageId,
            cancellationToken);
}

public sealed record DeleteMessageForEveryoneCommand(Guid MessageId)
    : ICommand;

public sealed class DeleteMessageForEveryoneHandler
    : ICommandHandler<DeleteMessageForEveryoneCommand>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public DeleteMessageForEveryoneHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result> HandleAsync(
        DeleteMessageForEveryoneCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.DeleteMessageForEveryoneAsync(
            command.MessageId,
            cancellationToken);
}

public sealed record ToggleMessageReactionCommand(Guid MessageId, string Emoji)
    : ICommand<MessageDto>;

public sealed class ToggleMessageReactionHandler
    : ICommandHandler<ToggleMessageReactionCommand, MessageDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public ToggleMessageReactionHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<MessageDto>> HandleAsync(
        ToggleMessageReactionCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.ToggleMessageReactionAsync(
            command.MessageId,
            command.Emoji,
            cancellationToken);
}

public sealed record PinMessageCommand(Guid MessageId)
    : ICommand<MessageDto>;

public sealed class PinMessageHandler
    : ICommandHandler<PinMessageCommand, MessageDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public PinMessageHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<MessageDto>> HandleAsync(
        PinMessageCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.PinMessageAsync(
            command.MessageId,
            cancellationToken);
}

public sealed record UnpinMessageCommand(Guid MessageId)
    : ICommand<MessageDto>;

public sealed class UnpinMessageHandler
    : ICommandHandler<UnpinMessageCommand, MessageDto>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public UnpinMessageHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result<MessageDto>> HandleAsync(
        UnpinMessageCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.UnpinMessageAsync(
            command.MessageId,
            cancellationToken);
}

public sealed record MarkConversationReadCommand(Guid ConversationId)
    : ICommand;

public sealed class MarkConversationReadHandler
    : ICommandHandler<MarkConversationReadCommand>
{
    private readonly IMessagingCommandService _messagingCommandService;

    public MarkConversationReadHandler(IMessagingCommandService messagingCommandService)
    {
        _messagingCommandService = messagingCommandService;
    }

    public Task<Result> HandleAsync(
        MarkConversationReadCommand command,
        CancellationToken cancellationToken = default)
        => _messagingCommandService.MarkConversationReadAsync(
            command.ConversationId,
            cancellationToken);
}
