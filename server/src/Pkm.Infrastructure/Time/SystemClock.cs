using Pkm.Application.Abstractions.Time;

namespace Pkm.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}