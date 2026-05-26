using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Commands.RenewBlockLease;

public sealed record RenewBlockLeaseCommand(
    Guid BlockId,
    string EditorSessionId) : ICommand<BlockLeaseDto>;
