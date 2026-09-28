using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EKR.API.Options;
using Microsoft.Extensions.Options;

namespace EKR.API.Services;

public interface IImageStorage
{
    Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public class CloudinaryImageStorage : IImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly HttpClient _httpClient;
    private readonly CloudinaryOptions _options;

    public CloudinaryImageStorage(HttpClient httpClient, IOptions<CloudinaryOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.CloudName) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary не настроен. Укажи CloudName, ApiKey и ApiSecret в appsettings.json (Dashboard → API Keys).");
        }

        if (file.Length == 0)
        {
            throw new InvalidOperationException("Пустой файл изображения.");
        }

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("Разрешены только изображения: jpg, jpeg, png, webp.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["timestamp"] = timestamp
        };

        if (!string.IsNullOrWhiteSpace(_options.Folder))
        {
            signParams["folder"] = _options.Folder;
        }

        var signature = CreateSignature(signParams, _options.ApiSecret);

        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(streamContent, "file", file.FileName);
        content.Add(new StringContent(_options.ApiKey), "api_key");
        content.Add(new StringContent(timestamp), "timestamp");
        content.Add(new StringContent(signature), "signature");

        if (!string.IsNullOrWhiteSpace(_options.Folder))
        {
            content.Add(new StringContent(_options.Folder), "folder");
        }

        var url = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("secure_url", out var secureUrl))
        {
            throw new InvalidOperationException("Cloudinary не вернул secure_url.");
        }

        return secureUrl.GetString()
            ?? throw new InvalidOperationException("Cloudinary вернул пустой secure_url.");
    }

    private static string CreateSignature(SortedDictionary<string, string> parameters, string apiSecret)
    {
        var toSign = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}")) + apiSecret;
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(toSign));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
