namespace Pkm.Domain.SharedKernel;
/// <summary>
/// Base class for entities that need identity and creation timestamp metadata.
/// </summary>
public abstract class CreationAuditedEntity
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedDate { get; private set; }

    protected CreationAuditedEntity() { }

    protected CreationAuditedEntity(Guid id, DateTimeOffset createdDate)
    {
        if (id == Guid.Empty)
            throw new DomainException("Id is invalid.");

        Id = id;
        CreatedDate = createdDate;
    }
}
