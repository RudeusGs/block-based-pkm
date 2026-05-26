using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Social;
using Pkm.Api.Contracts.Responses.Social;
using Pkm.Api.Mapping;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Commands;
using Pkm.Application.Features.Social.Models;
using Pkm.Application.Features.Social.Queries;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/social")]
public sealed class SocialController : BaseController
{
    private const long MaxRequestBodySizeBytes = 10 * 1024 * 1024;

    public SocialController(
        ICurrentUser currentUser,
        IUseCaseDispatcher useCaseDispatcher)
        : base(currentUser, useCaseDispatcher)
    {
    }

    [HttpGet("users/search")]
    [ProducesResponseType(typeof(ApiResult<UserSearchResultPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<UserSearchResultPagedResultResponse>>> SearchUsers(
        [FromQuery] string? keyword,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<SearchUsersQuery, UserSearchResultPagedResultDto>(
            new SearchUsersQuery(keyword ?? string.Empty, pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("users/{userId:guid}/profile")]
    [ProducesResponseType(typeof(ApiResult<UserProfilePageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<UserProfilePageResponse>>> GetProfile(
        [FromRoute] Guid userId,
        [FromQuery] int workspacePageNumber = 1,
        [FromQuery] int workspacePageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<GetProfileQuery, UserProfilePageDto>(
            new GetProfileQuery(userId, workspacePageNumber, workspacePageSize),
            cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPut("me/profile-page")]
    [ProducesResponseType(typeof(ApiResult<UserProfilePageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<UserProfilePageResponse>>> UpdateMyProfilePage(
        [FromBody] UpdateMyProfilePageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<UpdateMyProfilePageCommand, UserProfilePageDto>(
            new UpdateMyProfilePageCommand(request.Bio, request.CoverImageUrl),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("me/profile-cover-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBodySizeBytes)]
    [ProducesResponseType(typeof(ApiResult<UserProfilePageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 413)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<UserProfilePageResponse>>> UploadMyProfileCoverImage(
        [FromForm] UploadProfileCoverImageFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!FileUploadRequestMapper.HasValidFile(request.File))
            return BadRequest(FileUploadRequestMapper.CreateFileRequiredFailure<UserProfilePageResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var result = await ExecuteAsync<UploadMyProfileCoverImageCommand, UserProfilePageDto>(
            new UploadMyProfileCoverImageCommand(
                file.FileName,
                FileUploadRequestMapper.ResolveContentType(file),
                file.Length,
                stream),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("friend-requests")]
    [ProducesResponseType(typeof(ApiResult<FriendRequestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<FriendRequestResponse>>> SendFriendRequest(
        [FromBody] SendFriendRequestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<SendFriendRequestCommand, FriendRequestDto>(
            new SendFriendRequestCommand(request.AddresseeUserId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("friend-requests/incoming")]
    [ProducesResponseType(typeof(ApiResult<FriendRequestPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<FriendRequestPagedResultResponse>>> ListIncomingRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListIncomingFriendRequestsQuery, FriendRequestPagedResultDto>(
            new ListIncomingFriendRequestsQuery(pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("friend-requests/outgoing")]
    [ProducesResponseType(typeof(ApiResult<FriendRequestPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<FriendRequestPagedResultResponse>>> ListOutgoingRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListOutgoingFriendRequestsQuery, FriendRequestPagedResultDto>(
            new ListOutgoingFriendRequestsQuery(pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("friend-requests/{requestId:guid}/accept")]
    [ProducesResponseType(typeof(ApiResult<FriendRequestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<FriendRequestResponse>>> AcceptFriendRequest(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<AcceptFriendRequestCommand, FriendRequestDto>(
            new AcceptFriendRequestCommand(requestId),
            cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("friend-requests/{requestId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResult<FriendRequestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<FriendRequestResponse>>> RejectFriendRequest(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<RejectFriendRequestCommand, FriendRequestDto>(
            new RejectFriendRequestCommand(requestId),
            cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("friend-requests/{requestId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResult<FriendRequestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<FriendRequestResponse>>> CancelFriendRequest(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<CancelFriendRequestCommand, FriendRequestDto>(
            new CancelFriendRequestCommand(requestId),
            cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("friends")]
    [ProducesResponseType(typeof(ApiResult<FriendPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<FriendPagedResultResponse>>> ListFriends(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListFriendsQuery, FriendPagedResultDto>(
            new ListFriendsQuery(pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("friends/{friendUserId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> RemoveFriend(
        [FromRoute] Guid friendUserId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            new RemoveFriendCommand(friendUserId),
            cancellationToken);
        return HandleResult(result);
    }

}
