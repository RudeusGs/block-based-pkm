namespace Pkm.Infrastructure.Realtime;

public sealed class RealtimeOptions
{
    public const string SectionName = "Realtime";

    public int PresenceTtlSeconds { get; init; } = 30;
    public int BlockLeaseTtlSeconds { get; init; } = 30;
}