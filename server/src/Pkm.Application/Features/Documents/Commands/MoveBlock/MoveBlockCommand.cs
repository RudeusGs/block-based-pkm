using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Commands.MoveBlock;

public sealed record MoveBlockCommand(
    Guid BlockId,
    long ExpectedRevision,
    string EditorSessionId,
    Guid? NewParentBlockId,
    Guid? PreviousBlockId,
    Guid? NextBlockId) : ICommand<BlockMutationDto>;
