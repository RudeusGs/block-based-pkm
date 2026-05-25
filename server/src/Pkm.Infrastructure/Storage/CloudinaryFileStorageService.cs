using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Pkm.Application.Common.Abstractions.Storage;

namespace Pkm.Infrastructure.Storage;

internal sealed class CloudinaryFileStorageService : IFileStorageService
{
    private const string ProviderName = "cloudinary";

    private readonly CloudinaryStorageOptions _options;
    private readonly Lazy<Cloudinary> _cloudinary;

    public CloudinaryFileStorageService(IOptions<CloudinaryStorageOptions> options)
    {
        _options = options.Value;
        _cloudinary = new Lazy<Cloudinary>(CreateCloudinaryClient);
    }

    public async Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Content is null)
            throw new FileStorageException("Luồng file upload không hợp lệ.");

        ValidateRequest(request);

        if (request.Content.CanSeek)
            request.Content.Position = 0;

        try
        {
            var storedFileName = BuildStoredFileName(request.FileName);
            var publicId = BuildPublicId(request.FileName);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(storedFileName, request.Content),
                Folder = NormalizeFolder(_options.Folder),
                PublicId = publicId,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
            };

            var uploadResult = await _cloudinary.Value.UploadAsync(uploadParams);

            if (uploadResult.Error is not null)
                throw new FileStorageException(uploadResult.Error.Message);

            var publicUrl = BuildPublicUrl(uploadResult);
            var storageFileId = !string.IsNullOrWhiteSpace(uploadResult.PublicId)
                ? uploadResult.PublicId
                : uploadResult.AssetId;

            if (string.IsNullOrWhiteSpace(storageFileId))
                throw new FileStorageException("Cloudinary không trả về public id cho ảnh đã upload.");

            if (string.IsNullOrWhiteSpace(publicUrl))
                throw new FileStorageException("Cloudinary không trả về URL ảnh public.");

            return new FileStorageUploadResult(
                ProviderName,
                storageFileId,
                storedFileName,
                publicUrl,
                publicUrl);
        }
        catch (FileStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileStorageException("Không upload được file lên Cloudinary.", ex);
        }
    }

    private Cloudinary CreateCloudinaryClient()
    {
        if (string.IsNullOrWhiteSpace(_options.CloudName))
            throw new FileStorageException("Thiếu cấu hình CloudinaryStorage:CloudName.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new FileStorageException("Thiếu cấu hình CloudinaryStorage:ApiKey.");

        if (string.IsNullOrWhiteSpace(_options.ApiSecret))
            throw new FileStorageException("Thiếu cấu hình CloudinaryStorage:ApiSecret.");

        var account = new Account(
            _options.CloudName.Trim(),
            _options.ApiKey.Trim(),
            _options.ApiSecret.Trim());

        var cloudinary = new Cloudinary(account);
        cloudinary.Api.Secure = _options.UseSecureUrl;

        return cloudinary;
    }

    private void ValidateRequest(FileStorageUploadRequest request)
    {
        if (request.SizeBytes <= 0)
            throw new FileStorageException("File upload không được rỗng.");

        var maxBytes = Math.Max(_options.MaxImageSizeMb, 1) * 1024L * 1024L;
        if (request.SizeBytes > maxBytes)
            throw new FileStorageException($"Ảnh quá lớn. Vui lòng chọn ảnh nhỏ hơn {_options.MaxImageSizeMb}MB.");

        var extension = Path.GetExtension(request.FileName)
            .TrimStart('.')
            .ToLowerInvariant();

        var allowedFormats = _options.AllowedFormats.Length == 0
            ? new[] { "jpg", "jpeg", "png", "webp", "gif" }
            : _options.AllowedFormats;

        if (string.IsNullOrWhiteSpace(extension) ||
            !allowedFormats.Any(format => string.Equals(format, extension, StringComparison.OrdinalIgnoreCase)))
        {
            throw new FileStorageException("Định dạng ảnh không được hỗ trợ.");
        }
    }

    private string BuildPublicUrl(ImageUploadResult uploadResult)
    {
        var secureUrl = uploadResult.SecureUrl?.ToString();
        var url = uploadResult.Url?.ToString();

        return _options.UseSecureUrl
            ? secureUrl ?? url ?? string.Empty
            : url ?? secureUrl ?? string.Empty;
    }

    private static string NormalizeFolder(string folder)
    {
        var normalized = folder.Trim().Trim('/');

        return string.IsNullOrWhiteSpace(normalized)
            ? "block-based-pkm"
            : normalized;
    }

    private static string BuildStoredFileName(string fileName)
    {
        var originalName = Path.GetFileName(fileName ?? string.Empty);
        var extension = Path.GetExtension(originalName);
        var baseName = Path.GetFileNameWithoutExtension(originalName);

        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "image";

        var safeBaseName = SanitizeName(baseName);
        if (string.IsNullOrWhiteSpace(safeBaseName))
            safeBaseName = "image";

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        return $"{safeBaseName}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    private static string BuildPublicId(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        var safeBaseName = SanitizeName(baseName);

        if (string.IsNullOrWhiteSpace(safeBaseName))
            safeBaseName = "image";

        return $"{safeBaseName}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
    }

    private static string SanitizeName(string value)
    {
        var safeChars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray();

        var safe = new string(safeChars);

        while (safe.Contains("--", StringComparison.Ordinal))
            safe = safe.Replace("--", "-", StringComparison.Ordinal);

        return safe.Trim('-');
    }
}
