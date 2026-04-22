namespace Pkm.Infrastructure.Cache;

public interface IRedisSerializer
{
    string Serialize<T>(T value);
    T? Deserialize<T>(string value);
}