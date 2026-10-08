using System.Text.Json;
using EKR.API.Data;
using EKR.API.DTOs;
using EKR.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EKR.API.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> SearchAsync(string? query);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductRequest request, string? imageUrl);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, string? imageUrl);
    Task<bool> DeleteAsync(Guid id);
}

public class ProductService : IProductService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string? query)
    {
        var products = _db.Products.AsNoTracking().Include(p => p.Variants).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            products = products.Where(p =>
                p.ModelName.ToLower().Contains(q) ||
                p.Code.ToLower().Contains(q) ||
                (p.Description != null && p.Description.ToLower().Contains(q)) ||
                p.Variants.Any(v => v.Color.ToLower().Contains(q) || v.Size.ToLower().Contains(q)));
        }
        var list = await products.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _db.Products.AsNoTracking().Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? null : Map(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, string? imageUrl)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(p => p.Code == code))
            throw new InvalidOperationException($"Код модели «{code}» уже занят.");

        var colorMeta = NormalizeColorMeta(request.ColorMeta, request.Variants);
        var product = new Product
        {
            Code = code,
            ModelName = request.ModelName.Trim(),
            Description = request.Description?.Trim(),
            ImagePath = imageUrl,
            Price = request.Price,
            Season = request.Season.Trim(),
            ModelType = request.ModelType.Trim(),
            PiecesPerSeries = request.PiecesPerSeries,
            MinQuantity = request.MinQuantity,
            ColorMetaJson = JsonSerializer.Serialize(colorMeta, JsonOpts),
            IsPublished = request.IsPublished,
            Variants = request.Variants.Select(v => new ProductVariant
            {
                Color = v.Color.Trim(),
                Size = v.Size.Trim(),
                StockQuantity = v.StockQuantity
            }).ToList()
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return Map(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, string? imageUrl)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return null;

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(p => p.Code == code && p.Id != id))
            throw new InvalidOperationException($"Код модели «{code}» уже занят.");

        var colorMeta = NormalizeColorMeta(request.ColorMeta, request.Variants);
        var colorMetaJson = JsonSerializer.Serialize(colorMeta, JsonOpts);
        var modelName = request.ModelName.Trim();
        var description = request.Description?.Trim();
        var updatedAt = DateTime.UtcNow;
        var imagePath = string.IsNullOrWhiteSpace(imageUrl) ? product.ImagePath : imageUrl;

        await _db.Products.Where(p => p.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(p => p.Code, code)
            .SetProperty(p => p.ModelName, modelName)
            .SetProperty(p => p.Description, description)
            .SetProperty(p => p.Price, request.Price)
            .SetProperty(p => p.Season, request.Season.Trim())
            .SetProperty(p => p.ModelType, request.ModelType.Trim())
            .SetProperty(p => p.PiecesPerSeries, request.PiecesPerSeries)
            .SetProperty(p => p.MinQuantity, request.MinQuantity)
            .SetProperty(p => p.ColorMetaJson, colorMetaJson)
            .SetProperty(p => p.IsPublished, request.IsPublished)
            .SetProperty(p => p.UpdatedAt, updatedAt)
            .SetProperty(p => p.ImagePath, imagePath));

        var incoming = request.Variants
            .Select(v => new {
                Id = v.Id is { } vid && vid != Guid.Empty ? vid : (Guid?)null,
                Color = v.Color.Trim(),
                Size = v.Size.Trim(),
                StockQuantity = v.StockQuantity
            })
            .GroupBy(v => (v.Color.ToLowerInvariant(), v.Size.ToLowerInvariant()))
            .Select(g => g.First())
            .ToList();

        var existingVariants = await _db.ProductVariants.AsNoTracking().Where(v => v.ProductId == id).ToListAsync();
        var keepIds = new HashSet<Guid>();

        foreach (var item in incoming)
        {
            var existing = item.Id.HasValue ? existingVariants.FirstOrDefault(v => v.Id == item.Id.Value) : null;
            existing ??= existingVariants.FirstOrDefault(v =>
                string.Equals(v.Color, item.Color, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(v.Size, item.Size, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                keepIds.Add(existing.Id);
                await _db.ProductVariants.Where(v => v.Id == existing.Id).ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.Color, item.Color)
                    .SetProperty(v => v.Size, item.Size)
                    .SetProperty(v => v.StockQuantity, item.StockQuantity));
            }
            else
            {
                _db.ProductVariants.Add(new ProductVariant
                {
                    ProductId = id,
                    Color = item.Color,
                    Size = item.Size,
                    StockQuantity = item.StockQuantity
                });
            }
        }

        var removeIds = existingVariants.Where(v => !keepIds.Contains(v.Id)).Select(v => v.Id).ToList();
        if (removeIds.Count > 0)
        {
            var usedIds = await _db.OrderItems.AsNoTracking()
                .Where(oi => removeIds.Contains(oi.ProductVariantId))
                .Select(oi => oi.ProductVariantId).Distinct().ToListAsync();
            if (usedIds.Count > 0)
            {
                var blocked = existingVariants.Where(v => usedIds.Contains(v.Id));
                var names = string.Join(", ", blocked.Select(v => $"{v.Color}/{v.Size}"));
                throw new InvalidOperationException($"Нельзя удалить варианты, которые уже есть в заказах: {names}");
            }
            await _db.ProductVariants.Where(v => removeIds.Contains(v.Id)).ExecuteDeleteAsync();
        }

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
        => await _db.Products.Where(p => p.Id == id).ExecuteDeleteAsync() > 0;

    private static ProductDto Map(Product product)
    {
        var colorMeta = ParseColorMeta(product.ColorMetaJson);
        if (colorMeta.Count == 0)
            colorMeta = product.Variants.Select(v => v.Color).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(c => $"{c}:{c}:#888888").ToList();

        return new(
            product.Id, product.Code, product.ModelName, product.Description,
            NormalizeImageUrl(product.ImagePath), product.Price, product.Season, product.ModelType,
            product.PiecesPerSeries, product.MinQuantity, colorMeta, product.IsPublished,
            product.Variants.Sum(v => v.StockQuantity),
            product.Variants.OrderBy(v => v.Color).ThenBy(v => v.Size)
                .Select(v => new ProductVariantDto(v.Id, v.Color, v.Size, v.StockQuantity)).ToList(),
            product.CreatedAt);
    }

    private static List<string> NormalizeColorMeta(IReadOnlyList<string>? colorMeta, IReadOnlyList<ProductVariantInput> variants)
    {
        if (colorMeta is { Count: > 0 })
            return colorMeta.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return variants.Select(v => v.Color.Trim()).Where(c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(c => $"{c}:{c}:#888888").ToList();
    }

    private static List<string> ParseColorMeta(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? []; }
        catch { return []; }
    }

    private static string? NormalizeImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return null;
        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return imagePath;
        return $"/uploads/{Path.GetFileName(imagePath)}";
    }
}
