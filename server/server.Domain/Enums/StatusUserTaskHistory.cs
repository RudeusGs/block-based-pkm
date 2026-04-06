namespace server.Domain.Enums
{
    /// <summary>
    /// StatusUserTaskHistory: Trạng thái chi tiết của một bản ghi lịch sử tương tác với task.
    /// Dùng để phân tích thói quen và tính toán trọng số gợi ý.
    /// </summary>
    public enum StatusUserTaskHistory
    {
        /// <summary>
        /// Đã hoàn thành công việc thành công. (Tín hiệu tích cực nhất)
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Đã bắt đầu làm nhưng bỏ dở giữa chừng (không hoàn thành).
        /// Cho thấy Task có thể quá khó, quá dài hoặc bị ngắt quãng.
        /// </summary>
        Abandoned = 2,

        /// <summary>
        /// User chủ động bỏ qua task này khi nó được gợi ý (Nhấn "Làm việc khác").
        /// Cho thấy Task chưa phải ưu tiên hiện tại của User.
        /// </summary>
        Skipped = 3,

        /// <summary>
        /// User chủ động dời lịch thực hiện sang lúc khác (Hoãn).
        /// Khác với Skipped, Deferred cho thấy User có ý định làm nhưng không phải "ngay bây giờ".
        /// </summary>
        Deferred = 4,

        /// <summary>
        /// Task thất bại do yếu tố khách quan hoặc lỗi hệ thống.
        /// Trạng thái này giúp AI loại bỏ dữ liệu nhiễu khi tính hiệu suất User.
        /// </summary>
        Failed = 5
    }
}