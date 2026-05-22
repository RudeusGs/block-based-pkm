namespace Pkm.Infrastructure.Storage;

public sealed class CloudinaryStorageOptions
{
    public const string SectionName = "CloudinaryStorage";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Folder { get; set; } = "block-based-pkm";
    public bool UseSecureUrl { get; set; } = true;
    public int MaxImageSizeMb { get; set; } = 8;
    public string[] AllowedFormats { get; set; } = new[]
    {
        "jpg",
        "jpeg",
        "png",
        "webp",
        "gif",
    };
}
