using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Application.Features.Recommendations.Queries.GetUserTaskPreference;

public sealed record GetUserTaskPreferenceQuery(Guid WorkspaceId) : IQuery<UserTaskPreferenceDto>;
