using EKR.API.Data;
using EKR.API.DTOs;
using EKR.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EKR.API.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderStatus? status = null);
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<OrderDto> CreateAsync(CreateOrderRequest request, string userId);
    Task<OrderDto?> AdvanceStatusAsync(Guid id, UpdateOrderStatusRequest request, string userId, IReadOnlyList<string> roles);
}

public class OrderService : IOrderService
{    private static readonly Dictionary<string, Dictionary<OrderStatus, OrderStatus[]>> RoleTransitions = new()
    {
        [AppRoles.Admin] = new()
        {
            [OrderStatus.Created] = [OrderStatus.Accepted],
            [OrderStatus.Accepted] = [OrderStatus.SentToFactory],
            [OrderStatus.Ready] = [OrderStatus.Shipped]
        },
        [AppRoles.Factory] = new()
        {
            [OrderStatus.SentToFactory] = [OrderStatus.InProduction],
            [OrderStatus.InProduction] = [OrderStatus.Ready]
        },
        [AppRoles.SuperAdmin] = new()
        {
            [OrderStatus.Created] = [OrderStatus.Accepted],
            [OrderStatus.Accepted] = [OrderStatus.SentToFactory],
            [OrderStatus.SentToFactory] = [OrderStatus.InProduction],
            [OrderStatus.InProduction] = [OrderStatus.Ready],
            [OrderStatus.Ready] = [OrderStatus.Shipped]
        }
    };

    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderStatus? status = null)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.CreatedByUser)
            .Include(o => o.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v.Product)
            .Include(o => o.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return orders.Select(Map).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.CreatedByUser)
            .Include(o => o.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v.Product)
            .Include(o => o.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null ? null : Map(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, string userId)
    {
        var variantIds = request.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        if (variants.Count != variantIds.Count)
        {
            throw new InvalidOperationException("Один или несколько вариантов товара не найдены.");
        }

        foreach (var item in request.Items)
        {
            var variant = variants.First(v => v.Id == item.ProductVariantId);
            if (variant.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Недостаточно на складе: {variant.Product.ModelName} / {variant.Color} / {variant.Size}. Есть {variant.StockQuantity}, нужно {item.Quantity}.");
            }
        }

        var order = new Order
        {
            OrderNumber = $"E.K.R.-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            CustomerName = request.CustomerName.Trim(),
            Notes = request.Notes?.Trim(),
            Status = OrderStatus.Created,
            CreatedByUserId = userId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductVariantId = i.ProductVariantId,
                Quantity = i.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);
        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = OrderStatus.Created,
            ToStatus = OrderStatus.Created,
            ChangedByUserId = userId,
            Comment = "Заказ создан"
        });

        await _db.SaveChangesAsync();

        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<OrderDto?> AdvanceStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        string userId,
        IReadOnlyList<string> roles)
    {
        // Don't Include Identity users here — ConcurrencyStamp causes DbUpdateConcurrencyException on SaveChanges
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return null;
        }

        if (!CanTransition(roles, order.Status, request.Status))
        {
            throw new InvalidOperationException(
                $"Роль не может перевести заказ из «{order.Status}» в «{request.Status}».");
        }

        var from = order.Status;
        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = from,
            ToStatus = request.Status,
            ChangedByUserId = userId,
            Comment = request.Comment?.Trim()
        });

        if (request.Status == OrderStatus.Accepted && !order.StockDeducted)
        {
            foreach (var item in order.Items)
            {
                if (item.ProductVariant.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Нельзя принять заказ: не хватает склада для {item.ProductVariant.Product.ModelName} / {item.ProductVariant.Color} / {item.ProductVariant.Size}. Есть {item.ProductVariant.StockQuantity}, нужно {item.Quantity}.");
                }

                item.ProductVariant.StockQuantity -= item.Quantity;
            }

            order.StockDeducted = true;
        }

        await _db.SaveChangesAsync();
        return await GetByIdAsync(order.Id);
    }

    private static bool CanTransition(IReadOnlyList<string> roles, OrderStatus from, OrderStatus to)
    {
        foreach (var role in roles)
        {
            if (!RoleTransitions.TryGetValue(role, out var map))
            {
                continue;
            }

            if (map.TryGetValue(from, out var allowed) && allowed.Contains(to))
            {
                return true;
            }
        }

        return false;
    }

    private static OrderDto Map(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.CustomerName,
            order.Notes,
            order.Status,
            order.Status.ToString(),
            order.CreatedByUser?.FullName ?? order.CreatedByUserId,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductVariantId,
                i.ProductVariant.Product.ModelName,
                i.ProductVariant.Color,
                i.ProductVariant.Size,
                i.Quantity)).ToList(),
            order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryDto(
                    h.FromStatus,
                    h.ToStatus,
                    h.ChangedByUser?.FullName ?? h.ChangedByUserId,
                    h.Comment,
                    h.ChangedAt))
                .ToList());
}
