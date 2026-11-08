namespace OrderService;

public sealed partial class OrderApi
{
    /// <summary>GET /orders/{id} - a one-line summary of the order.</summary>
    public string GetOrder(int id)
    {
        Order order = _orders.Get(id);
        return $"Order {order.Id} for {order.Customer}: {order.Items.Count} item(s), starting with {order.Items[0].Sku}";
    }
}
