namespace OrderService;

/// <summary>
/// The service's API. Each method lives in its own file under Api/ (one per route); exceptions are caught and logged by
/// the host, the way a web framework would turn them into 500 responses.
/// </summary>
public sealed partial class OrderApi
{
    private readonly OrderRepository _orders;

    public OrderApi(OrderRepository orders) => _orders = orders;

    /// <summary>Sum of quantity times unit price, before any discount.</summary>
    internal static decimal Subtotal(Order order) => order.Items.Sum(item => item.Quantity * item.UnitPrice);
}
