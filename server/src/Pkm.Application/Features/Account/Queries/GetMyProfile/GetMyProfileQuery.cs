using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Models;

namespace Pkm.Application.Features.Account.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<UserProfileDto>;
