namespace server.Domain.Base
{
    public abstract class EntityBase
    {
        public int Id { get; protected set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? UpdatedDate { get; private set; }

        public bool IsDeleted { get; private set; }
        public DateTime? DeletedDate { get; private set; }

        public byte[] RowVersion { get; private set; } = default!;

        protected EntityBase()
        {
            CreatedDate = DateTime.UtcNow;
            IsDeleted = false;
        }

        public void MarkUpdated()
        {
            EnsureNotDeleted();
            UpdatedDate = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            if (IsDeleted) return;

            IsDeleted = true;
            DeletedDate = DateTime.UtcNow;
            UpdatedDate = DateTime.UtcNow;
        }

        public void Restore()
        {
            if (!IsDeleted) return;

            IsDeleted = false;
            DeletedDate = null;
            UpdatedDate = DateTime.UtcNow;
        }

        protected void EnsureNotDeleted()
        {
            if (IsDeleted)
                throw new DomainException("Entity đã bị xóa.");
        }
    }
}