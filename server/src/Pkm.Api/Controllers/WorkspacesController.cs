using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Workspaces;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Workspaces;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;
using Pkm.Application.Features.Workspaces.Commands.ChangeWorkspaceMemberRole;
using Pkm.Application.Features.Workspaces.Commands.CreateWorkspace;
using Pkm.Application.Features.Workspaces.Commands.DeleteWorkspace;
using Pkm.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;
using Pkm.Application.Features.Workspaces.Commands.UpdateWorkspace;
using Pkm.Application.Features.Workspaces.Queries.GetWorkspaceById;
using Pkm.Application.Features.Workspaces.Queries.ListWorkspaceMembers;
using Pkm.Domain.Workspaces;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/workspaces")]
public sealed class WorkspacesController : BaseController
{
    private readonly CreateWorkspaceHandler _createWorkspaceHandler;
    private readonly UpdateWorkspaceHandler _updateWorkspaceHandler;
    private readonly DeleteWorkspaceHandler _deleteWorkspaceHandler;
    private readonly GetWorkspaceByIdHandler _getWorkspaceByIdHandler;
    private readonly ListWorkspaceMembersHandler _listWorkspaceMembersHandler;
    private readonly AddWorkspaceMemberHandler _addWorkspaceMemberHandler;
    private readonly ChangeWorkspaceMemberRoleHandler _changeWorkspaceMemberRoleHandler;
    private readonly RemoveWorkspaceMemberHandler _removeWorkspaceMemberHandler;

    public WorkspacesController(
        ICurrentUser currentUser,
        CreateWorkspaceHandler createWorkspaceHandler,
        UpdateWorkspaceHandler updateWorkspaceHandler,
        DeleteWorkspaceHandler deleteWorkspaceHandler,
        GetWorkspaceByIdHandler getWorkspaceByIdHandler,
        ListWorkspaceMembersHandler listWorkspaceMembersHandler,
        AddWorkspaceMemberHandler addWorkspaceMemberHandler,
        ChangeWorkspaceMemberRoleHandler changeWorkspaceMemberRoleHandler,
        RemoveWorkspaceMemberHandler removeWorkspaceMemberHandler)
        : base(currentUser)
    {
        _createWorkspaceHandler = createWorkspaceHandler;
        _updateWorkspaceHandler = updateWorkspaceHandler;
        _deleteWorkspaceHandler = deleteWorkspaceHandler;
        _getWorkspaceByIdHandler = getWorkspaceByIdHandler;
        _listWorkspaceMembersHandler = listWorkspaceMembersHandler;
        _addWorkspaceMemberHandler = addWorkspaceMemberHandler;
        _changeWorkspaceMemberRoleHandler = changeWorkspaceMemberRoleHandler;
        _removeWorkspaceMemberHandler = removeWorkspaceMemberHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<WorkspaceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceResponse>>> Create(
        [FromBody] CreateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateWorkspaceCommand(
            request.Name,
            request.Description);

        var result = await _createWorkspaceHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPut("{workspaceId:guid}")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceResponse>>> Update(
        [FromRoute] Guid workspaceId,
        [FromBody] UpdateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkspaceCommand(
            workspaceId,
            request.Name,
            request.Description);

        var result = await _updateWorkspaceHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("{workspaceId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> Delete(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _deleteWorkspaceHandler.HandleAsync(
            new DeleteWorkspaceCommand(workspaceId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{workspaceId:guid}")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<WorkspaceResponse>>> GetById(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _getWorkspaceByIdHandler.HandleAsync(
            new GetWorkspaceByIdQuery(workspaceId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("{workspaceId:guid}/members")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<WorkspaceMemberResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<WorkspaceMemberResponse>>>> ListMembers(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _listWorkspaceMembersHandler.HandleAsync(
            new ListWorkspaceMembersQuery(workspaceId),
            cancellationToken);

        return HandleResult(result, x => (IReadOnlyList<WorkspaceMemberResponse>)x.Select(y => y.ToResponse()).ToArray());
    }

    [HttpPost("{workspaceId:guid}/members")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceMemberResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceMemberResponse>>> AddMember(
        [FromRoute] Guid workspaceId,
        [FromBody] AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseWorkspaceRole(request.Role, out var role))
        {
            return HandleResult<WorkspaceMemberResponse>(
                Result.Failure<WorkspaceMemberResponse>(
                    WorkspaceErrors.InvalidAddMemberRequest(new[]
                    {
                        "Role không hợp lệ. Giá trị hợp lệ: owner, manager, member, viewer."
                    })));
        }

        var command = new AddWorkspaceMemberCommand(
            workspaceId,
            request.UserId,
            role);

        var result = await _addWorkspaceMemberHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("{workspaceId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceMemberResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceMemberResponse>>> ChangeMemberRole(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid userId,
        [FromBody] ChangeWorkspaceMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseWorkspaceRole(request.Role, out var role))
        {
            return HandleResult<WorkspaceMemberResponse>(
                Result.Failure<WorkspaceMemberResponse>(
                    WorkspaceErrors.InvalidRoleChangeRequest(new[]
                    {
                        "Role không hợp lệ. Giá trị hợp lệ: owner, manager, member, viewer."
                    })));
        }

        var command = new ChangeWorkspaceMemberRoleCommand(
            workspaceId,
            userId,
            role);

        var result = await _changeWorkspaceMemberRoleHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult>> RemoveMember(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _removeWorkspaceMemberHandler.HandleAsync(
            new RemoveWorkspaceMemberCommand(workspaceId, userId),
            cancellationToken);

        return HandleResult(result);
    }

    private static bool TryParseWorkspaceRole(string? rawRole, out WorkspaceRole role)
    {
        role = default;

        if (string.IsNullOrWhiteSpace(rawRole))
            return false;

        switch (rawRole.Trim().ToLowerInvariant())
        {
            case "owner":
                role = WorkspaceRole.Owner;
                return true;

            case "manager":
                role = WorkspaceRole.Manager;
                return true;

            case "member":
                role = WorkspaceRole.Member;
                return true;

            case "viewer":
                role = WorkspaceRole.Viewer;
                return true;

            default:
                return false;
        }
    }
}