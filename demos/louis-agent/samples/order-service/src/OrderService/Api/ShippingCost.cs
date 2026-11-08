namespace OrderService;

public sealed partial class OrderApi
{
    private const decimal FreeShippingFrom = 100m;

    /// <summary>GET /orders/{id}/shipping - free from 100.00; otherwise the parcel class follows the average item price.</summary>
    public decimal GetShippingCost(int id)
    {
        Order order = _orders.Get(id);
        decimal subtotal = Subtotal(order);
        if (subtotal >= FreeShippingFrom) return 0m;

        decimal averageItemPrice = subtotal / order.Items.Count;
        return averageItemPrice > 50m ? 9.95m : 4.95m;
    }
}
