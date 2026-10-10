using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Core.Services;

public class OrderService(
    IRepository<Order> orders,
    IRepository<Product> products,
    IRepository<Cart> carts,
    ICouponService coupons,
    IEmailService email) : IOrderService
{
    public async Task<Result<Order>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        if (!request.Payment.Success || string.IsNullOrEmpty(request.Payment.TransactionId))
            return Result<Order>.Fail("Payment was not completed.");

        var cart = (await carts.GetAllAsync(ct)).FirstOrDefault(c => c.OwnerId == request.CartOwnerId);
        if (cart is null || cart.Items.Count == 0)
            return Result<Order>.Fail("Your cart is empty.");

        // Load the catalog slice the cart touches and validate stock at current prices.
        var productIds = cart.Items.Select(i => i.ProductId).ToHashSet();
        var catalog = (await products.GetAllAsync(ct))
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        foreach (var item in cart.Items)
        {
            if (!catalog.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                return Result<Order>.Fail("An item in your cart is no longer available.");
            var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
            var stock = variant?.Stock ?? product.TotalStock();
            if (item.Quantity > stock)
                return Result<Order>.Fail($"Only {stock} left of \"{product.Name}\".");
        }

        var subtotal = cart.Items.Sum(i =>
        {
            var product = catalog[i.ProductId];
            var variant = product.Variants.FirstOrDefault(v => v.Id == i.VariantId);
            return (variant?.EffectivePrice(product.Price) ?? product.Price) * i.Quantity;
        });

        var validation = await coupons.ValidateAsync(cart.AppliedCouponCode, subtotal, ct);
        var discount = validation.IsValid ? validation.DiscountAmount : 0;
        var shipping = Shipping.Resolve(request.ShippingMethodId, subtotal)
            ?? throw new InvalidOperationException("No shipping method resolved.");

        var order = new Order
        {
            OrderNumber = await NextOrderNumberAsync(ct),
            UserId = request.UserId,
            Email = request.ShippingAddress.Email,
            Status = OrderStatus.Paid,
            ShippingAddress = request.ShippingAddress,
            ShippingMethodId = shipping.Id,
            ShippingMethodName = shipping.Name,
            CouponCode = validation.IsValid ? validation.Coupon!.Code : null,
            Subtotal = subtotal,
            Discount = discount,
            ShippingFee = shipping.Fee,
            Total = Math.Max(0, subtotal - discount) + shipping.Fee,
            PaymentTransactionId = request.Payment.TransactionId,
            CardLast4 = request.Payment.CardLast4,
            Lines = cart.Items.Select(i =>
            {
                var product = catalog[i.ProductId];
                var variant = product.Variants.FirstOrDefault(v => v.Id == i.VariantId);
                return new OrderLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    VariantName = variant?.Name ?? "Standard",
                    Sku = variant?.Sku ?? product.Sku,
                    ImageUrl = product.Images.FirstOrDefault() ?? "/images/catalog/placeholder.svg",
                    UnitPrice = variant?.EffectivePrice(product.Price) ?? product.Price,
                    Quantity = i.Quantity
                };
            }).ToList()
        };

        // Decrement stock now that everything validated.
        foreach (var item in cart.Items)
        {
            var product = catalog[item.ProductId];
            var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
            if (variant is not null) variant.Stock -= item.Quantity;
            await products.UpdateAsync(product, ct);
        }

        await orders.AddAsync(order, ct);

        if (validation.IsValid)
            await coupons.RecordUsageAsync(validation.Coupon!.Id, ct);

        // Empty the cart.
        cart.Items.Clear();
        cart.AppliedCouponCode = null;
        cart.UpdatedAt = DateTime.UtcNow;
        await carts.UpdateAsync(cart, ct);

        await SendOrderEmailAsync(order, EmailType.OrderConfirmation,
            "Order confirmed 🎉",
            $"Hi {order.ShippingAddress.FullName},\n\nThanks for your order! We received your payment and your order is now being prepared.\n\nOrder: {order.OrderNumber}\nItems: {order.Lines.Sum(l => l.Quantity)}\nTotal: {order.Total:C}\n\nWe'll email you again when it ships.",
            ct);

        return Result<Order>.Ok(order);
    }

    public Task<Order?> GetByIdAsync(string id, CancellationToken ct = default) => orders.GetByIdAsync(id, ct);

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default) =>
        (await orders.GetAllAsync(ct))
            .FirstOrDefault(o => o.OrderNumber.Equals(orderNumber, StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string userId, CancellationToken ct = default) =>
        (await orders.GetAllAsync(ct))
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt)
            .ToList();

    public async Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status = null, CancellationToken ct = default) =>
        (await orders.GetAllAsync(ct))
            .Where(o => status is null || o.Status == status)
            .OrderByDescending(o => o.PlacedAt)
            .ToList();

    public async Task<Result> AdvanceStatusAsync(string orderId, OrderStatus to, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null) return Result.Fail("Order not found.");

        var allowed = (order.Status, to) switch
        {
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Placed, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
            return Result.Fail($"Cannot move an order from {order.Status} to {to}.");

        order.Status = to;
        order.UpdatedAt = DateTime.UtcNow;
        await orders.UpdateAsync(order, ct);

        if (to == OrderStatus.Cancelled)
        {
            await RestockAsync(order, ct);
            await SendOrderEmailAsync(order, EmailType.OrderCancelled,
                $"Order {order.OrderNumber} cancelled",
                $"Hi {order.ShippingAddress.FullName},\n\nYour order {order.OrderNumber} has been cancelled and refunded. We hope to see you again soon.",
                ct);
        }
        if (to == OrderStatus.Shipped)
        {
            await SendOrderEmailAsync(order, EmailType.OrderShipped,
                $"Order {order.OrderNumber} is on its way 📦",
                $"Hi {order.ShippingAddress.FullName},\n\nGood news — your order {order.OrderNumber} has shipped via {order.ShippingMethodName}.\n\nTotal: {order.Total:C}",
                ct);
        }
        else if (to == OrderStatus.Delivered)
        {
            await SendOrderEmailAsync(order, EmailType.OrderDelivered,
                $"Order {order.OrderNumber} delivered ✅",
                $"Hi {order.ShippingAddress.FullName},\n\nYour order {order.OrderNumber} has been delivered. Enjoy!\n\nIf you have a moment, we'd love a review.",
                ct);
        }

        return Result.Ok();
    }

    public async Task<IReadOnlyList<(string Label, int Count)>> GetStatusCountsAsync(CancellationToken ct = default)
    {
        var all = await orders.GetAllAsync(ct);
        return Enum.GetValues<OrderStatus>()
            .Select(s => (s.ToString(), all.Count(o => o.Status == s)))
            .ToList();
    }

    public async Task<(int OrderCount, decimal Revenue, decimal AvgOrderValue, int LowStockCount)> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var all = (await orders.GetAllAsync(ct)).Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var revenue = all.Sum(o => o.Total);
        var lowStock = (await products.GetAllAsync(ct))
            .Count(p => p.IsActive && p.Variants.Any(v => v.IsActive && v.Stock <= 5));
        return (all.Count, revenue, all.Count == 0 ? 0 : revenue / all.Count, lowStock);
    }

    private async Task RestockAsync(Order order, CancellationToken ct)
    {
        foreach (var line in order.Lines)
        {
            var product = await products.GetByIdAsync(line.ProductId, ct);
            if (product is null) continue;
            var variant = product.Variants.FirstOrDefault(v => v.Sku == line.Sku);
            if (variant is not null)
            {
                variant.Stock += line.Quantity;
                await products.UpdateAsync(product, ct);
            }
        }
    }

    private async Task<string> NextOrderNumberAsync(CancellationToken ct)
    {
        var existing = (await orders.GetAllAsync(ct)).Select(o => o.OrderNumber).ToHashSet();
        string number;
        do
        {
            number = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 10000)}";
        } while (existing.Contains(number));
        return number;
    }

    private async Task SendOrderEmailAsync(Order order, EmailType type, string subject, string body, CancellationToken ct) =>
        await email.SendAsync(order.Email, subject, body + $"\n\n— The Nimbus Team", type, ct);
}
