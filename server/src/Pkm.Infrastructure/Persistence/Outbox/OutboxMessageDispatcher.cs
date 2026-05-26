using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Domain.SharedKernel;

namespace Pkm.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessageDispatcher : IOutboxMessageDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxMessageSerializer _serializer;
    private readonly ILogger<OutboxMessageDispatcher> _logger;
    private readonly IReadOnlyDictionary<string, Type> _domainEventTypes;

    public OutboxMessageDispatcher(
        IServiceProvider serviceProvider,
        IOutboxMessageSerializer serializer,
        ILogger<OutboxMessageDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _serializer = serializer;
        _logger = logger;
        _domainEventTypes = LoadDomainEventTypes();
    }

    public async Task DispatchAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!_domainEventTypes.TryGetValue(message.EventType, out var eventType))
        {
            _logger.LogWarning(
                "No domain event type registered for outbox message {OutboxMessageId} with type {EventType}.",
                message.Id,
                message.EventType);

            return;
        }

        var domainEvent = _serializer.Deserialize(message.PayloadJson, eventType);
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType).ToArray();

        if (handlers.Length == 0)
        {
            _logger.LogWarning(
                "No domain event handler registered for outbox message {OutboxMessageId} with type {EventType}.",
                message.Id,
                message.EventType);

            return;
        }

        foreach (var handler in handlers)
        {
            var task = (Task?)handlerType
                .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
                ?.Invoke(handler, new[] { domainEvent, cancellationToken });

            if (task is not null)
                await task;
        }
    }

    private static IReadOnlyDictionary<string, Type> LoadDomainEventTypes()
    {
        return typeof(IDomainEvent)
            .Assembly
            .DefinedTypes
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                typeof(IDomainEvent).IsAssignableFrom(type))
            .Select(type => type.AsType())
            .GroupBy(type => type.FullName ?? type.Name)
            .ToDictionary(group => group.Key, group => group.First());
    }
}
