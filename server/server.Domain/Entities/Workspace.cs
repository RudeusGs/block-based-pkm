using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// Workspace: Không gian làm việc chung cho các trang và công việc.
    /// </summary>
    public class Workspace : EntityBase
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public int OwnerId { get; private set; }
        public Workspace(string name, int ownerId, string? description = null)
        {
            SetName(name);
            if (ownerId <= 0) throw new DomainException("OwnerId phải lớn hơn 0.");
            OwnerId = ownerId;
            Description = description;
        }
        public void UpdateInformation(string newName, string? newDescription)
        {
            SetName(newName);
            Description = newDescription;
            MarkUpdated();
        }
        public void TransferOwnership(int newOwnerId)
        {
            if (newOwnerId <= 0) throw new DomainException("Chủ sở hữu mới không hợp lệ.");
            if (OwnerId == newOwnerId) return;
            OwnerId = newOwnerId;
            MarkUpdated();
        }
        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên Workspace không được để trống.");
            if (name.Length > 50)
                throw new DomainException("Tên Workspace không được quá 50 ký tự.");
            Name = name;
        }
    }
}