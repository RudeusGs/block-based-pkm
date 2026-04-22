namespace Pkm.Infrastructure.Cache;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Connection { get; init; } = string.Empty;

    public string InstanceName { get; init; } = "pkm";

    public int DefaultDatabase { get; init; } = 0;

    public int DefaultTtlMinutes { get; init; } = 30;
}