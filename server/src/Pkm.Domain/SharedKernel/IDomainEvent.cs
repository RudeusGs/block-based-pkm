namespace Pkm.Domain.SharedKernel;

public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
