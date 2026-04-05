namespace server.Domain.Base
{
    // EntityBase: Lớp cơ sở cho tất cả các thực thể trong domain, cung cấp các thuộc tính và phương thức chung.
    public abstract class EntityBase
    {
        // Id: Mã định danh duy nhất của thực thể, tự động tăng và được thiết lập bởi ORM.
        public int Id { get; protected set; }
        // CreatedDate: Thời điểm thực thể được tạo, tự động thiết lập khi khởi tạo.
        public DateTime CreatedDate { get; private set; }
        // UpdatedDate: Thời điểm thực thể được cập nhật lần cuối, có thể null nếu chưa từng cập nhật.
        public DateTime? UpdatedDate { get; private set; }
        // IsDeleted: Cờ đánh dấu thực thể đã bị xóa (soft delete), mặc định là false.

        // Constructor: Khởi tạo thực thể mới, thiết lập CreatedDate
        protected EntityBase()
        {
            CreatedDate = DateTime.UtcNow;
        }

        // MarkUpdated: Phương thức để đánh dấu thực thể đã được cập nhật, thiết lập UpdatedDate thành thời điểm hiện tại.
        public void MarkUpdated()
        {
            UpdatedDate = DateTime.UtcNow;
        }
    }
}