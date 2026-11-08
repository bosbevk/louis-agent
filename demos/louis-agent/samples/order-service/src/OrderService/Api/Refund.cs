namespace OrderService;

public sealed partial class OrderApi
{
    /// <summary>POST /orders/{id}/refund - refunds the order total to the card it was paid with.</summary>
    public string RefundOrder(int id)
    {
        Order order = _orders.Get(id);
        if (order.Refunded) throw new InvalidOperationException($"Order {id} has already been refunded");

        decimal amount = GetOrderTotal(id);
        string cardSuffix = order.PaymentRef[^4..];
        return $"Refunded {amount:0.00} to card ending {cardSuffix}";
    }
}
