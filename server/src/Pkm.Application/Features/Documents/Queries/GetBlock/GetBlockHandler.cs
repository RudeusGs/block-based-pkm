using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;

namespace Pkm.Application.Features.Documents.Queries.GetBlock;

public sealed class GetBlockHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IBlockRepository _blockRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public GetBlockHandler(
        ICurrentUser currentUser,
        IBlockRepository blockRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _currentUser = currentUser;
        _blockRepository = blockRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<BlockDto>> HandleAsync(
        GetBlockQuery request,
        CancellationToken cancellationToken)
    {
        if (request.BlockId == Guid.Empty)
            return Result.Failure<BlockDto>(DocumentErrors.InvalidBlockId);

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<BlockDto>(DocumentErrors.MissingUserContext);

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            request.BlockId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<BlockDto>(DocumentErrors.BlockNotFound);

        if (!access.CanRead)
            return Result.Failure<BlockDto>(DocumentErrors.BlockForbidden);

        var block = await _blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure<BlockDto>(DocumentErrors.BlockNotFound);

        return Result.Success(block.ToDto());
    }
}