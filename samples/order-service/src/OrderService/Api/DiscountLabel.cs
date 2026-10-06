using System.Globalization;

namespace OrderService;

public sealed partial class OrderApi
{
    /// <summary>GET /orders/{id}/discount-label - how the receipt shows the code, e.g. "WELCOME (10% off)".</summary>
    public string GetDiscountLabel(int id)
    {
        Order order = _orders.Get(id);
        string code = order.DiscountCode;
        if (code.Length == 0) return "no discount";

        int percent = int.Parse(code[^2..], CultureInfo.InvariantCulture);
        return $"{code[..^2]} ({percent}% off)";
    }
}
