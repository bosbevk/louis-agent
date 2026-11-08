namespace OrderService;

public sealed partial class OrderApi
{
    private static readonly Dictionary<string, decimal> DiscountRates = new()
    {
        ["WELCOME10"] = 0.10m,
        ["VIP20"] = 0.20m,
    };

    /// <summary>GET /orders/{id}/total - the amount to charge, after any discount code.</summary>
    public decimal GetOrderTotal(int id)
    {
        Order order = _orders.Get(id);
        decimal subtotal = Subtotal(order);
        if (string.IsNullOrEmpty(order.DiscountCode)) return subtotal;

        decimal rate = DiscountRates[order.DiscountCode];
        return Math.Round(subtotal * (1 - rate), 2);
    }
}
