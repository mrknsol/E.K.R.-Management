using System.ComponentModel.DataAnnotations;
using EKR.API.Models;

namespace EKR.API.DTOs;

public record OrderItemInput(
    [Required] Guid ProductVariantId,
    [Range(1, int.MaxValue)] int Quantity);

public record CreateOrderRequest(
    [Required, MaxLength(200)] string CustomerName,
    [MaxLength(2000)] string? Notes,
    [MinLength(1)] IReadOnlyList<OrderItemInput> Items);

public record UpdateOrderStatusRequest(
    [Required] OrderStatus Status,
    [MaxLength(1000)] string? Comment);

public record OrderItemDto(
    Guid Id,
    Guid ProductVariantId,
    string ModelName,
    string Color,
    string Size,
    int Quantity);

public record OrderStatusHistoryDto(
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    string ChangedBy,
    string? Comment,
    DateTime ChangedAt);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string? Notes,
    OrderStatus Status,
    string StatusName,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> History);
