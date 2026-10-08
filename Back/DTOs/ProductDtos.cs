using System.ComponentModel.DataAnnotations;

namespace EKR.API.DTOs;

public record ProductVariantDto(Guid Id, string Color, string Size, int StockQuantity);

public record ProductDto(
    Guid Id,
    string Code,
    string ModelName,
    string? Description,
    string? ImageUrl,
    int Price,
    string Season,
    string ModelType,
    int PiecesPerSeries,
    int MinQuantity,
    IReadOnlyList<string> ColorMeta,
    bool IsPublished,
    int TotalStock,
    IReadOnlyList<ProductVariantDto> Variants,
    DateTime CreatedAt);

public record ProductVariantInput(
    Guid? Id,
    [Required, MaxLength(80)] string Color,
    [Required, MaxLength(80)] string Size,
    [Range(0, int.MaxValue)] int StockQuantity);

public record CreateProductRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(200)] string ModelName,
    [MaxLength(2000)] string? Description,
    [Range(0, int.MaxValue)] int Price,
    [Required, MaxLength(50)] string Season,
    [Required, MaxLength(100)] string ModelType,
    [Range(1, 100)] int PiecesPerSeries,
    [Range(1, int.MaxValue)] int MinQuantity,
    IReadOnlyList<string>? ColorMeta,
    bool IsPublished,
    [MinLength(1)] IReadOnlyList<ProductVariantInput> Variants);

public record UpdateProductRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(200)] string ModelName,
    [MaxLength(2000)] string? Description,
    [Range(0, int.MaxValue)] int Price,
    [Required, MaxLength(50)] string Season,
    [Required, MaxLength(100)] string ModelType,
    [Range(1, 100)] int PiecesPerSeries,
    [Range(1, int.MaxValue)] int MinQuantity,
    IReadOnlyList<string>? ColorMeta,
    bool IsPublished,
    [MinLength(1)] IReadOnlyList<ProductVariantInput> Variants);
