using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Profiles;

namespace Pkm.Application.Common.Abstractions.Persistence;

public interface IUserProfileRepository
{
    Task<UserProfilePage?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfilePage?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfilePageDto?> GetProfileAsync(
        Guid viewedUserId,
        Guid viewerUserId,
        IReadOnlyList<ProfileWorkspaceDto> workspaces,
        int workspacePageNumber,
        int workspacePageSize,
        int workspaceTotalCount,
        int workspaceTotalPages,
        CancellationToken cancellationToken = default);

    void Add(UserProfilePage profile);

    void Update(UserProfilePage profile);
}
