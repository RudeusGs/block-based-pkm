namespace Pkm.Infrastructure.Persistence.Outbox;

internal interface IOutboxMessageSerializer
{
    string Serialize(object payload);

    object Deserialize(string payloadJson, Type payloadType);
}
