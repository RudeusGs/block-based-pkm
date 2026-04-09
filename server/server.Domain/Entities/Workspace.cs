using server.Domain.Base;

public class Workspace : EntityBase
{
    private const int MaxNameLength = 50;
    private const int MaxDescriptionLength = 500;

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int OwnerId { get; private set; }

    protected Workspace() { }

    public Workspace(string name, int ownerId, string? description = null)
    {
        SetName(name);

        if (ownerId <= 0)
            throw new DomainException("OwnerId không hợp lệ.");

        OwnerId = ownerId;
        Description = NormalizeDescription(description);
    }

    public void UpdateInformation(string newName, string? newDescription)
    {
        SetName(newName);
        Description = NormalizeDescription(newDescription);
        MarkUpdated();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên Workspace không được để trống.");

        var trimmed = name.Trim();

        if (trimmed.Length > MaxNameLength)
            throw new DomainException($"Tên Workspace không được quá {MaxNameLength} ký tự.");

        Name = trimmed;
    }

    private string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var trimmed = description.Trim();

        if (trimmed.Length > MaxDescriptionLength)
            throw new DomainException($"Description không được quá {MaxDescriptionLength} ký tự.");

        return trimmed;
    }
}