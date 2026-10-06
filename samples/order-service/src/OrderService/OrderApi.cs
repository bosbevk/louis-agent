namespace OrderService;

/// <summary>The service's API methods. Each one is what a client calls; exceptions are caught and logged by the host.</summary>
public sealed class OrderApi(OrderRepository orders)
{
    private static readonly Dictionary<string, decimal> DiscountRates = new()
    {
        ["WELCOME10"] = 0.10m,
        ["VIP20"] = 0.20m,
    };

    /// <summary>GET /orders/{id}</summary>
    public string GetOrder(int id)
    {
        Order order = orders.Get(id);
        return $"Order {order.Id} for {order.Customer}: {order.Items.Count} item(s)";
    }

    /// <summary>GET /orders/{id}/total - the amount to charge, after any discount code.</summary>
    public decimal GetOrderTotal(int id)
    {
        Order order = orders.Get(id);
        decimal subtotal = order.Items.Sum(item => item.Quantity * item.UnitPrice);
        if (string.IsNullOrEmpty(order.DiscountCode)) return subtotal;

        decimal rate = DiscountRates[order.DiscountCode];
        return Math.Round(subtotal * (1 - rate), 2);
    }

    /// <summary>POST /orders/{id}/refund - refunds the order total to the card it was paid with.</summary>
    public string RefundOrder(int id)
    {
        Order order = orders.Get(id);
        if (order.Refunded) throw new InvalidOperationException($"Order {id} has already been refunded");

        decimal amount = GetOrderTotal(id);
        string cardSuffix = order.PaymentRef[^4..];
        return $"Refunded {amount:0.00} to card ending {cardSuffix}";
    }
}
