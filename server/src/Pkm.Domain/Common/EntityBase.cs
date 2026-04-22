namespace Pkm.Domain.Common;

public abstract class EntityBase
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedDate { get; private set; }
    public DateTimeOffset? UpdatedDate { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedDate { get; private set; }

    // EF Core/Npgsql optimistic concurrency token.
    // Database-managed and updated automatically on each successful write.
    public uint RowVersion { get; private set; }

    protected EntityBase() { }

    protected EntityBase(Guid id, DateTimeOffset createdDate)
    {
        if (id == Guid.Empty)
            throw new DomainException("Id không hợp lệ.");

        Id = id;
        CreatedDate = createdDate;
        IsDeleted = false;
    }

    protected void Touch(DateTimeOffset utcNow)
    {
        ThrowIfDeleted();
        UpdatedDate = utcNow;
    }

    public void SoftDelete(DateTimeOffset utcNow)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedDate = utcNow;
        UpdatedDate = utcNow;
    }

    public void Restore(DateTimeOffset utcNow)
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedDate = null;
        UpdatedDate = utcNow;
    }

    protected void ThrowIfDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Entity đã bị xóa.");
    }
}