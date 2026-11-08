namespace OrderService;

public sealed partial class OrderApi
{
    private static readonly Dictionary<string, decimal> VatRates = new()
    {
        ["NL"] = 0.21m,
        ["DE"] = 0.19m,
        ["FR"] = 0.20m,
    };

    /// <summary>GET /orders/{id}/vat - the VAT included in the order total, at the rate of the order's country.</summary>
    public decimal GetVat(int id)
    {
        Order order = _orders.Get(id);
        decimal rate = VatRates[order.Country];
        decimal total = GetOrderTotal(id);
        return Math.Round(total - total / (1 + rate), 2);
    }
}
