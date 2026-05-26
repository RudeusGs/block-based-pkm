namespace Pkm.Infrastructure.Persistence.Outbox;

internal interface IOutboxMessageSerializer
{
    string Serialize(object payload);
}
