namespace OrderService;

public sealed partial class OrderApi
{
    /// <summary>GET /orders/{id}/loyalty-points - one point per 10.00 spent (before discounts).</summary>
    public int GetLoyaltyPoints(int id)
    {
        Order order = _orders.Get(id);
        int cents = checked((int)(Subtotal(order) * 100));
        return cents / 1000;
    }
}
