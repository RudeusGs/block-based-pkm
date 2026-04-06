namespace server.Domain.Enums
{
    /// <summary>
    /// StatusTaskRecommendation: Trạng thái của một lời gợi ý công việc từ hệ thống AI.
    /// Dùng để theo dõi phản hồi của người dùng đối với các gợi ý được đưa ra.
    /// </summary>
    public enum StatusTaskRecommendation
    {
        /// <summary>
        /// Gợi ý vừa được tạo và đang chờ người dùng phản hồi.
        /// Đây là trạng thái mặc định khi hệ thống đưa ra một Task mới cho User.
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Người dùng đã chấp nhận lời gợi ý (nhấn nút "Làm ngay" hoặc "Đồng ý").
        /// Task này sẽ được đưa vào danh sách ưu tiên thực hiện hiện tại.
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// Người dùng chủ động từ chối lời gợi ý (nhấn "Không quan tâm" hoặc "Xóa").
        /// Trạng thái này là tín hiệu quan trọng để AI giảm trọng số gợi ý loại Task này trong tương lai.
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// Người dùng đã thực sự hoàn thành công việc thông qua lời gợi ý này.
        /// Đây là kết quả thành công nhất (Conversion), minh chứng cho độ chính xác của thuật toán.
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Gợi ý đã hết hạn (quá thời gian hiệu lực mà người dùng không có bất kỳ tương tác nào).
        /// Dùng để dọn dẹp các gợi ý cũ, giúp danh sách gợi ý luôn tươi mới.
        /// </summary>
        Expired = 5,
    }
}