namespace OrderService;

public sealed partial class OrderApi
{
    /// <summary>GET /orders/{id}/packing-slip - what the warehouse prints: items, then the gift message in capitals.</summary>
    public string GetPackingSlip(int id)
    {
        Order order = _orders.Get(id);
        string items = string.Join("; ", order.Items.Select(item => $"{item.Quantity} x {item.Sku}"));
        string gift = order.GiftMessage!.ToUpperInvariant();
        return $"{items} | GIFT: {gift}";
    }
}
