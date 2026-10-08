namespace EKR.API.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public OrderSource Source { get; set; } = OrderSource.Manual;
    public Guid? WebsiteOrderId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public AppUser CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool StockDeducted { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
