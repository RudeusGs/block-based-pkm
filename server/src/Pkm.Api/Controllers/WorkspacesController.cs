using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Mapping;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Workspaces;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Workspaces;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Commands.AcceptWorkspaceInvitation;
using Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;
using Pkm.Application.Features.Workspaces.Commands.ChangeWorkspaceMemberRole;
using Pkm.Application.Features.Workspaces.Commands.CreateWorkspace;
using Pkm.Application.Features.Workspaces.Commands.DeleteWorkspace;
using Pkm.Application.Features.Workspaces.Commands.JoinPublicWorkspaceAsViewer;
using Pkm.Application.Features.Workspaces.Commands.TransferWorkspaceOwnership;
using Pkm.Application.Features.Workspaces.Commands.LeaveWorkspace;
using Pkm.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;
using Pkm.Application.Features.Workspaces.Commands.UpdateWorkspace;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Queries.GetWorkspaceById;
using Pkm.Application.Features.Workspaces.Queries.ListWorkspaceMembers;
using Pkm.Domain.Workspaces;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/workspaces")]
public sealed class WorkspacesController : BaseController
{
    private readonly IUseCaseDispatcher _dispatcher;

    public WorkspacesController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser)
    {
        _dispatcher = dispatcher;
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
        if (!EnumRequestParsers.TryParseWorkspaceVisibility(request.Visibility, out var visibility))
        {
            return HandleResult<WorkspaceResponse>(
                Result.Failure<WorkspaceResponse>(
                    WorkspaceErrors.InvalidCreateRequest(new[]
                    {
                        "Visibility không hợp lệ. Giá trị hợp lệ: private, public."
                    })));
        }

        var command = new CreateWorkspaceCommand(
            request.Name,
            request.Description,
            visibility ?? WorkspaceVisibility.Private);

        var result = await _dispatcher.ExecuteAsync<CreateWorkspaceCommand, WorkspaceDto>(command, cancellationToken);
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
        if (!EnumRequestParsers.TryParseWorkspaceVisibility(request.Visibility, out var visibility))
        {
            return HandleResult<WorkspaceResponse>(
                Result.Failure<WorkspaceResponse>(
                    WorkspaceErrors.InvalidUpdateRequest(new[]
                    {
                        "Visibility không hợp lệ. Giá trị hợp lệ: private, public."
                    })));
        }

        var command = new UpdateWorkspaceCommand(
            workspaceId,
            request.Name,
            request.Description,
            visibility);

        var result = await _dispatcher.ExecuteAsync<UpdateWorkspaceCommand, WorkspaceDto>(command, cancellationToken);
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
        var result = await _dispatcher.ExecuteAsync(
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
        var result = await _dispatcher.QueryAsync<GetWorkspaceByIdQuery, WorkspaceDto>(
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
        var result = await _dispatcher.QueryAsync<ListWorkspaceMembersQuery, IReadOnlyList<WorkspaceMemberDto>>(
            new ListWorkspaceMembersQuery(workspaceId),
            cancellationToken);

        return HandleResult(result, x => (IReadOnlyList<WorkspaceMemberResponse>)x.Select(y => y.ToResponse()).ToArray());
    }

    [HttpPost("{workspaceId:guid}/members")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceInvitationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceInvitationResponse>>> InviteMember(
        [FromRoute] Guid workspaceId,
        [FromBody] AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!EnumRequestParsers.TryParseWorkspaceRole(request.Role, out var role))
        {
            return HandleResult<WorkspaceInvitationResponse>(
                Result.Failure<WorkspaceInvitationResponse>(
                    WorkspaceErrors.InvalidAddMemberRequest(new[]
                    {
                        "Role không hợp lệ. Giá trị hợp lệ: manager, member, viewer."
                    })));
        }

        var command = new AddWorkspaceMemberCommand(
            workspaceId,
            request.Email,
            role);

        var result = await _dispatcher.ExecuteAsync<AddWorkspaceMemberCommand, WorkspaceInvitationDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }


    [HttpPost("{workspaceId:guid}/join-as-viewer")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceResponse>>> JoinAsViewer(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync<JoinPublicWorkspaceAsViewerCommand, WorkspaceDto>(
            new JoinPublicWorkspaceAsViewerCommand(workspaceId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("{workspaceId:guid}/leave")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult>> Leave(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync(
            new LeaveWorkspaceCommand(workspaceId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{workspaceId:guid}/transfer-ownership")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceResponse>>> TransferOwnership(
        [FromRoute] Guid workspaceId,
        [FromBody] TransferWorkspaceOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync<TransferWorkspaceOwnershipCommand, WorkspaceDto>(
            new TransferWorkspaceOwnershipCommand(workspaceId, request.NewOwnerUserId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [AllowAnonymous]
    [HttpGet("/api/v1/workspace-invitations/accept")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceMemberResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceMemberResponse>>> AcceptInvitation(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync<AcceptWorkspaceInvitationCommand, WorkspaceMemberDto>(
            new AcceptWorkspaceInvitationCommand(token),
            cancellationToken);

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
        if (!EnumRequestParsers.TryParseWorkspaceRole(request.Role, out var role))
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

        var result = await _dispatcher.ExecuteAsync<ChangeWorkspaceMemberRoleCommand, WorkspaceMemberDto>(command, cancellationToken);
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
        var result = await _dispatcher.ExecuteAsync(
            new RemoveWorkspaceMemberCommand(workspaceId, userId),
            cancellationToken);

        return HandleResult(result);
    }
}
