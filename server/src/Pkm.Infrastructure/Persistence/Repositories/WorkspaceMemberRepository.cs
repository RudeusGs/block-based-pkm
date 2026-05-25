using Microsoft.EntityFrameworkCore;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class WorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly DataContext _context;

    public WorkspaceMemberRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<WorkspaceMember?> GetByWorkspaceAndUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkspaceMembers
            .FirstOrDefaultAsync(
                member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId,
                cancellationToken);
    }

    public async Task<WorkspaceMemberReadModel?> GetDetailByWorkspaceAndUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(member =>
                member.WorkspaceId == workspaceId &&
                member.UserId == userId)
            .Join(
                _context.Users.AsNoTracking(),
                member => member.UserId,
                user => user.Id,
                (member, user) => new WorkspaceMemberReadModel(
                    member.WorkspaceId,
                    member.UserId,
                    user.UserName,
                    user.Email,
                    user.FullName,
                    user.AvatarUrl,
                    user.Status,
                    member.Role,
                    member.Role == WorkspaceRole.Owner,
                    member.CreatedDate,
                    member.UpdatedDate))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceMemberReadModel>> ListByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId)
            .Join(
                _context.Users.AsNoTracking(),
                member => member.UserId,
                user => user.Id,
                (member, user) => new
                {
                    Member = member,
                    User = user
                })
            .OrderByDescending(x => x.Member.Role == WorkspaceRole.Owner)
            .ThenBy(x => x.Member.Role)
            .ThenBy(x => x.User.FullName)
            .ThenBy(x => x.User.Email)
            .Select(x => new WorkspaceMemberReadModel(
                x.Member.WorkspaceId,
                x.Member.UserId,
                x.User.UserName,
                x.User.Email,
                x.User.FullName,
                x.User.AvatarUrl,
                x.User.Status,
                x.Member.Role,
                x.Member.Role == WorkspaceRole.Owner,
                x.Member.CreatedDate,
                x.Member.UpdatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(
                member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId,
                cancellationToken);
    }

    public void Add(WorkspaceMember member)
    {
        _context.WorkspaceMembers.Add(member);
    }

    public void Update(WorkspaceMember member)
    {
        _context.WorkspaceMembers.Update(member);
    }

    public void Remove(WorkspaceMember member)
    {
        _context.WorkspaceMembers.Remove(member);
    }
}
