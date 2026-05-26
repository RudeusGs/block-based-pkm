using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Files;

public sealed class StoredFile : EntityBase
{
    public const int MaxProviderLength = 80;
    public const int MaxStorageFileIdLength = 500;
    public const int MaxFileNameLength = 500;
    public const int MaxOriginalFileNameLength = 500;
    public const int MaxContentTypeLength = 150;
    public const int MaxUrlLength = 2048;
    public const int MaxPurposeLength = 80;

    public Guid UploadedByUserId { get; private set; }
    public string StorageProvider { get; private set; } = string.Empty;
    public string StorageFileId { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string PublicUrl { get; private set; } = string.Empty;
    public string? WebViewLink { get; private set; }
    public string? Purpose { get; private set; }

    private StoredFile()
    {
    }

    private StoredFile(
        Guid id,
        Guid uploadedByUserId,
        string storageProvider,
        string storageFileId,
        string fileName,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string publicUrl,
        string? webViewLink,
        string? purpose,
        DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(uploadedByUserId, nameof(uploadedByUserId));

        if (sizeBytes <= 0)
            throw new DomainException("File size is invalid.");

        UploadedByUserId = uploadedByUserId;
        StorageProvider = TextRules.NormalizeRequired(storageProvider, MaxProviderLength, nameof(StorageProvider));
        StorageFileId = TextRules.NormalizeRequired(storageFileId, MaxStorageFileIdLength, nameof(StorageFileId));
        FileName = TextRules.NormalizeRequired(fileName, MaxFileNameLength, nameof(FileName));
        OriginalFileName = TextRules.NormalizeRequired(originalFileName, MaxOriginalFileNameLength, nameof(OriginalFileName));
        ContentType = TextRules.NormalizeRequired(contentType, MaxContentTypeLength, nameof(ContentType));
        SizeBytes = sizeBytes;
        PublicUrl = TextRules.NormalizeRequired(publicUrl, MaxUrlLength, nameof(PublicUrl));
        WebViewLink = TextRules.NormalizeOptional(webViewLink, MaxUrlLength, nameof(WebViewLink));
        Purpose = TextRules.NormalizeOptional(purpose, MaxPurposeLength, nameof(Purpose));
    }

    public static StoredFile Create(
        Guid id,
        Guid uploadedByUserId,
        string storageProvider,
        string storageFileId,
        string fileName,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string publicUrl,
        string? webViewLink,
        string? purpose,
        DateTimeOffset now)
        => new(
            id,
            uploadedByUserId,
            storageProvider,
            storageFileId,
            fileName,
            originalFileName,
            contentType,
            sizeBytes,
            publicUrl,
            webViewLink,
            purpose,
            now);
}
