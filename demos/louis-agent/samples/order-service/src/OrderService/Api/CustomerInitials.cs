namespace OrderService;

public sealed partial class OrderApi
{
    /// <summary>GET /orders/{id}/customer-initials - the initials printed on the parcel label, e.g. "AL".</summary>
    public string GetCustomerInitials(int id)
    {
        Order order = _orders.Get(id);
        string[] parts = order.Customer.Split(' ');
        return $"{parts[0][0]}{parts[1][0]}";
    }
}
