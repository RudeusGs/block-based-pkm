using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Workspaces;

public static class WorkspaceErrors
{
    public static readonly Error MissingUserContext = new(
        "Workspace.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error WorkspaceNotFound = new(
        "Workspace.NotFound",
        "Không tìm thấy workspace.",
        ResultStatus.NotFound);

    public static readonly Error WorkspaceForbidden = new(
        "Workspace.Forbidden",
        "Không có quyền truy cập workspace này.",
        ResultStatus.Forbidden);

    public static readonly Error WorkspaceOwnerOnly = new(
        "Workspace.OwnerOnly",
        "Chỉ owner mới được thực hiện thao tác này.",
        ResultStatus.Forbidden);

    public static readonly Error WorkspaceManageMembersForbidden = new(
        "Workspace.ManageMembersForbidden",
        "Không có quyền quản lý thành viên của workspace.",
        ResultStatus.Forbidden);

    public static readonly Error WorkspaceMemberNotFound = new(
        "Workspace.MemberNotFound",
        "Không tìm thấy thành viên trong workspace.",
        ResultStatus.NotFound);

    public static readonly Error WorkspaceMemberAlreadyExists = new(
        "Workspace.MemberAlreadyExists",
        "Người dùng đã là thành viên của workspace.",
        ResultStatus.Conflict);

    public static readonly Error PublicWorkspaceJoinOnly = new(
        "Workspace.PublicWorkspaceJoinOnly",
        "Chỉ có thể tự tham gia workspace đang để Public.",
        ResultStatus.Forbidden);

    public static readonly Error TargetUserNotFound = new(
        "Workspace.TargetUserNotFound",
        "Không tìm thấy người dùng được chỉ định.",
        ResultStatus.NotFound);

    public static readonly Error CannotModifyOwnerMembership = new(
        "Workspace.CannotModifyOwnerMembership",
        "Không thể thay đổi hoặc xóa owner của workspace trong phiên bản hiện tại.",
        ResultStatus.Unprocessable);

    public static readonly Error CannotAssignOwnerRole = new(
        "Workspace.CannotAssignOwnerRole",
        "Không thể gán Owner qua API quản lý thành viên trong phiên bản hiện tại.",
        ResultStatus.Unprocessable);

    public static readonly Error CannotManageYourself = new(
        "Workspace.CannotManageYourself",
        "Không thể tự thay đổi hoặc tự xóa chính mình trong thao tác này.",
        ResultStatus.Unprocessable);


    public static readonly Error WorkspaceInvitationAlreadyPending = new(
        "Workspace.InvitationAlreadyPending",
        "Email này đã có lời mời đang chờ xác nhận trong workspace.",
        ResultStatus.Conflict);

    public static readonly Error InvalidInvitationToken = new(
        "Workspace.InvalidInvitationToken",
        "Token xác nhận lời mời không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error InvitationNotFound = new(
        "Workspace.InvitationNotFound",
        "Không tìm thấy lời mời workspace.",
        ResultStatus.NotFound);

    public static readonly Error InvitationExpired = new(
        "Workspace.InvitationExpired",
        "Lời mời workspace đã hết hạn.",
        ResultStatus.Unprocessable);

    public static readonly Error InvitationAlreadyAccepted = new(
        "Workspace.InvitationAlreadyAccepted",
        "Lời mời workspace đã được xác nhận trước đó.",
        ResultStatus.Conflict);

    public static readonly Error InvitationEmailMismatch = new(
        "Workspace.InvitationEmailMismatch",
        "Email tài khoản không khớp với email trong lời mời.",
        ResultStatus.Forbidden);

    public static readonly Error WorkspaceInvitationEmailFailed = new(
        "Workspace.InvitationEmailFailed",
        "Tạo lời mời thành công nhưng gửi email thất bại. Kiểm tra cấu hình SMTP.",
        ResultStatus.Failure);

    public static Error TargetUserNotFoundByEmail(string email)
        => new(
            "Workspace.TargetUserNotFoundByEmail",
            $"Không tìm thấy tài khoản với email '{email}'. Người được mời cần đăng ký bằng đúng email này trước khi xác nhận.",
            ResultStatus.NotFound);

    public static Error InvalidCreateRequest(IReadOnlyList<string> details)
        => new(
            "Workspace.InvalidCreateRequest",
            "Dữ liệu tạo workspace không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidUpdateRequest(IReadOnlyList<string> details)
        => new(
            "Workspace.InvalidUpdateRequest",
            "Dữ liệu cập nhật workspace không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidAddMemberRequest(IReadOnlyList<string> details)
        => new(
            "Workspace.InvalidAddMemberRequest",
            "Dữ liệu thêm thành viên không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidRoleChangeRequest(IReadOnlyList<string> details)
        => new(
            "Workspace.InvalidRoleChangeRequest",
            "Dữ liệu đổi vai trò không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidWorkspaceId(Guid workspaceId)
        => new(
            "Workspace.InvalidWorkspaceId",
            $"WorkspaceId '{workspaceId}' không hợp lệ.",
            ResultStatus.Validation);

    public static Error InvalidUserId(Guid userId)
        => new(
            "Workspace.InvalidUserId",
            $"UserId '{userId}' không hợp lệ.",
            ResultStatus.Validation);
    public static Error TargetUserAccountNotActive(string email)
        => new(
            "Workspace.TargetUserAccountNotActive",
            $"Tài khoản với email {email} hiện đang bị khóa hoặc đã bị vô hiệu hóa.",
            ResultStatus.Forbidden);
}
