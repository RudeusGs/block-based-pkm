using Pkm.Application.Common.Results;
using Pkm.Domain.Blocks;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Documents.Services;

public sealed class BlockOrderPlanner : IBlockOrderPlanner
{
    private const int RebalanceOrderKeyLengthThreshold = 90;

    private readonly IOrderKeyGenerator _orderKeyGenerator;

    public BlockOrderPlanner(IOrderKeyGenerator orderKeyGenerator)
    {
        _orderKeyGenerator = orderKeyGenerator;
    }

    public Result<string> CreateOrderKeyOrRebalance(
        IReadOnlyList<Block> siblings,
        Guid? previousBlockId,
        Guid? nextBlockId,
        Guid? parentBlockId,
        Guid actorId,
        DateTimeOffset now,
        Guid newBlockId)
    {
        var position = ResolveInsertionPosition(siblings, previousBlockId, nextBlockId);
        if (position.Error is not null)
            return Result.Failure<string>(position.Error);

        try
        {
            var candidate = _orderKeyGenerator.CreateBetween(
                position.Previous?.OrderKey,
                position.Next?.OrderKey);

            if (!NeedsRebalance(candidate, siblings))
                return Result.Success(candidate);
        }
        catch (DomainException)
        {
        }

        RebalanceSiblings(
            siblings,
            position.InsertIndex,
            parentBlockId,
            actorId,
            now);

        return Result.Success(CreateRebalancedOrderKey(position.InsertIndex, newBlockId));
    }

    private static bool NeedsRebalance(
        string candidate,
        IReadOnlyList<Block> siblings)
    {
        return string.IsNullOrWhiteSpace(candidate) ||
               candidate.Length > RebalanceOrderKeyLengthThreshold ||
               siblings.Any(x => string.Equals(x.OrderKey, candidate, StringComparison.Ordinal));
    }

    private static void RebalanceSiblings(
        IReadOnlyList<Block> siblings,
        int insertIndex,
        Guid? parentBlockId,
        Guid actorId,
        DateTimeOffset now)
    {
        for (var index = 0; index < siblings.Count; index++)
        {
            var finalIndex = index < insertIndex
                ? index
                : index + 1;

            var newOrderKey = CreateRebalancedOrderKey(finalIndex, siblings[index].Id);

            if (string.Equals(siblings[index].OrderKey, newOrderKey, StringComparison.Ordinal))
                continue;

            siblings[index].MoveTo(parentBlockId, newOrderKey, actorId, now);
        }
    }

    private static string CreateRebalancedOrderKey(int index, Guid stableId)
        => $"M{index + 1:D12}{stableId:N}";

    private static InsertionPosition ResolveInsertionPosition(
        IReadOnlyList<Block> siblings,
        Guid? previousBlockId,
        Guid? nextBlockId)
    {
        var ordered = siblings
            .OrderBy(x => x.OrderKey, StringComparer.Ordinal)
            .ToArray();

        if (previousBlockId.HasValue &&
            nextBlockId.HasValue &&
            previousBlockId.Value == nextBlockId.Value)
        {
            return InsertionPosition.Failure(DocumentErrors.InvalidBlockPosition);
        }

        var previousIndex = -1;
        var nextIndex = -1;
        Block? previous = null;
        Block? next = null;

        if (previousBlockId.HasValue)
        {
            previousIndex = Array.FindIndex(ordered, x => x.Id == previousBlockId.Value);

            if (previousIndex < 0)
                return InsertionPosition.Failure(DocumentErrors.BlockNotFound);

            previous = ordered[previousIndex];
        }

        if (nextBlockId.HasValue)
        {
            nextIndex = Array.FindIndex(ordered, x => x.Id == nextBlockId.Value);

            if (nextIndex < 0)
                return InsertionPosition.Failure(DocumentErrors.BlockNotFound);

            next = ordered[nextIndex];
        }

        if (previous is not null && next is not null)
        {
            if (previousIndex + 1 != nextIndex)
                return InsertionPosition.Failure(DocumentErrors.InvalidBlockPosition);

            return new InsertionPosition(
                previous,
                next,
                nextIndex,
                Error: null);
        }

        if (previous is not null)
        {
            var insertIndex = previousIndex + 1;
            next = insertIndex < ordered.Length ? ordered[insertIndex] : null;

            return new InsertionPosition(
                previous,
                next,
                insertIndex,
                Error: null);
        }

        if (next is not null)
        {
            var insertIndex = nextIndex;
            previous = insertIndex > 0 ? ordered[insertIndex - 1] : null;

            return new InsertionPosition(
                previous,
                next,
                insertIndex,
                Error: null);
        }

        return new InsertionPosition(
            ordered.LastOrDefault(),
            Next: null,
            InsertIndex: ordered.Length,
            Error: null);
    }

    private sealed record InsertionPosition(
        Block? Previous,
        Block? Next,
        int InsertIndex,
        Error? Error)
    {
        public static InsertionPosition Failure(Error error)
            => new(
                Previous: null,
                Next: null,
                InsertIndex: 0,
                Error: error);
    }
}
