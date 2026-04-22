namespace Pkm.Application.Abstractions.Caching;

public interface IRedisKeyFactory
{
    string Build(params string[] segments);
}