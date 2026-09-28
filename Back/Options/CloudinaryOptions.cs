namespace EKR.API.Options;

public class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    /// <summary>Dashboard → Cloud name</summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>Dashboard → API Keys → API Key</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Dashboard → API Keys → API Secret</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Folder inside Media Library, e.g. ekr/products</summary>
    public string Folder { get; set; } = "ekr/products";
}
