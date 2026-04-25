using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Documents;

public static class DocumentErrors
{
    public static readonly Error InvalidExpectedRevision = new(
        "Document.InvalidExpectedRevision",
        "ExpectedRevision không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error InvalidEditorSessionId = new(
        "Document.InvalidEditorSessionId",
        "EditorSessionId không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error RevisionConflict = new(
        "Document.RevisionConflict",
        "Document đã thay đổi bởi thao tác khác. Vui lòng tải lại dữ liệu mới nhất.",
        ResultStatus.Conflict);

    public static readonly Error MissingUserContext = new(
        "Document.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error InvalidPageId = new(
        "Document.InvalidPageId",
        "PageId không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error InvalidBlockId = new(
        "Document.InvalidBlockId",
        "BlockId không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error PageNotFound = new(
        "Document.PageNotFound",
        "Không tìm thấy page.",
        ResultStatus.NotFound);

    public static readonly Error PageArchived = new(
        "Document.PageArchived",
        "Không thể chỉnh sửa page đã archive.",
        ResultStatus.Unprocessable);

    public static readonly Error PageForbidden = new(
        "Document.PageForbidden",
        "Không có quyền truy cập page/document này.",
        ResultStatus.Forbidden);

    public static readonly Error BlockNotFound = new(
        "Document.BlockNotFound",
        "Không tìm thấy block.",
        ResultStatus.NotFound);

    public static readonly Error BlockForbidden = new(
        "Document.BlockForbidden",
        "Không có quyền thao tác block này.",
        ResultStatus.Forbidden);

    public static readonly Error ParentBlockNotFound = new(
        "Document.ParentBlockNotFound",
        "Parent block không tồn tại hoặc đã bị xóa.",
        ResultStatus.NotFound);

    public static readonly Error ParentBlockDifferentPage = new(
        "Document.ParentBlockDifferentPage",
        "Parent block không thuộc cùng page.",
        ResultStatus.Unprocessable);

    public static readonly Error InvalidBlockPosition = new(
        "Document.InvalidBlockPosition",
        "Vị trí chèn block không hợp lệ. PreviousBlockId và NextBlockId phải thuộc cùng parent và liền kề nhau.",
        ResultStatus.Unprocessable);

    public static readonly Error BlockCycleDetected = new(
        "Document.BlockCycleDetected",
        "Không thể move block vào chính nó hoặc descendant của nó.",
        ResultStatus.Unprocessable);

    public static readonly Error LeaseConflict = new(
        "Document.LeaseConflict",
        "Block đang được người khác chỉnh sửa.",
        ResultStatus.Conflict);

    public static readonly Error LeaseRequired = new(
        "Document.LeaseRequired",
        "Bạn phải acquire edit lease trước khi ghi thay đổi lên block.",
        ResultStatus.Conflict);

    public static readonly Error LeaseSessionMismatch = new(
        "Document.LeaseSessionMismatch",
        "EditorSessionId không khớp với lease hiện tại của block.",
        ResultStatus.Conflict);
}