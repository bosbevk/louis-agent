using System.Globalization;

namespace OrderService;

public sealed partial class OrderApi
{
    /// <summary>GET /orders/{id}/invoice-number - INV-{year}{month}-{id}, from the order's created date.</summary>
    public string GetInvoiceNumber(int id)
    {
        Order order = _orders.Get(id);
        DateTime created = DateTime.ParseExact(order.Created, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"INV-{created:yyyyMM}-{order.Id}";
    }
}
