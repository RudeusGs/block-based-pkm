namespace Pkm.Application.Abstractions.Storage;

public sealed record FileStorageUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Purpose);

public sealed record FileStorageUploadResult(
    string Provider,
    string StorageFileId,
    string FileName,
    string PublicUrl,
    string? WebViewLink);

public interface IFileStorageService
{
    Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FileStorageException : Exception
{
    public FileStorageException(string message) : base(message)
    {
    }

    public FileStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
