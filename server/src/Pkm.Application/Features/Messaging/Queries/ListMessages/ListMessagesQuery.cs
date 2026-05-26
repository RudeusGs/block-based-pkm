using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Queries;

public sealed record ListMessagesQuery(
    Guid ConversationId,
    int PageNumber,
    int PageSize) : IQuery<MessagePagedResultDto>;
