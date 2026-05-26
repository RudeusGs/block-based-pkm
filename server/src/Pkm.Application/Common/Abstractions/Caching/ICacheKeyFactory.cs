namespace Pkm.Application.Common.Abstractions.Caching;

public interface ICacheKeyFactory
{
    string Build(params string[] segments);
}
