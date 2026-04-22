namespace Pkm.Domain.Common;
/// <summary>
/// CreationAuditedEntity: Lớp cơ sở trừu tượng cho các thực thể có thông tin về thời điểm tạo và ID.
/// </summary>
public abstract class CreationAuditedEntity
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedDate { get; private set; }

    protected CreationAuditedEntity(){ }

    protected CreationAuditedEntity(Guid id, DateTimeOffset createdDate)
    {
        if (id == Guid.Empty)
            throw new DomainException("Id không hợp lệ.");

        Id = id;
        CreatedDate = createdDate;
    }
}