namespace EKR.API.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public int Price { get; set; }
    public string Season { get; set; } = "SS";
    public string ModelType { get; set; } = "Jacket";
    public int PiecesPerSeries { get; set; } = 4;
    public int MinQuantity { get; set; } = 1;
    public string ColorMetaJson { get; set; } = "[]";
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
