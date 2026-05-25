using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Queries.GetWorkTaskById;

public sealed record GetWorkTaskByIdQuery(Guid TaskId) : IQuery<WorkTaskDto>;
