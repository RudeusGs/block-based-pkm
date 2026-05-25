namespace Pkm.Application.Common.Abstractions.Caching;

public interface IRedisKeyFactory
{
    string Build(params string[] segments);
}
