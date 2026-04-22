namespace Pkm.Application.Abstractions.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}