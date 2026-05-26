using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Commands.ReleaseBlockLease;

public sealed record ReleaseBlockLeaseCommand(
    Guid BlockId,
    string EditorSessionId) : ICommand<BlockLeaseDto>;
