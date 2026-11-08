using System.Globalization;
using OrderService;

// A tiny stand-in for a microservice: each "request" is an API method name and an order id.
//   dotnet run -- <method> <order id>      one call, e.g. "order-total 1003"
//   dotnet run -- serve data/requests.txt  replay a traffic file, one call per line
// Unhandled exceptions are logged to logs/errors.jsonl, the way a real service would report them to its error tracker.
var api = new OrderApi(new OrderRepository(Path.Combine("data", "orders.csv")));

var routes = new Dictionary<string, Func<int, string>>
{
    ["get-order"] = api.GetOrder,
    ["order-total"] = id => Money(api.GetOrderTotal(id)),
    ["shipping-cost"] = id => Money(api.GetShippingCost(id)),
    ["invoice-number"] = api.GetInvoiceNumber,
    ["packing-slip"] = api.GetPackingSlip,
    ["loyalty-points"] = id => api.GetLoyaltyPoints(id).ToString(CultureInfo.InvariantCulture),
    ["vat"] = id => Money(api.GetVat(id)),
    ["delivery-estimate"] = api.GetDeliveryEstimate,
    ["discount-label"] = api.GetDiscountLabel,
    ["customer-initials"] = api.GetCustomerInitials,
    ["refund"] = api.RefundOrder,
};

if (args is ["serve", var trafficFile])
{
    foreach (string line in File.ReadLines(trafficFile).Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#')))
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Handle(parts[0], parts.ElementAtOrDefault(1) ?? "");
    }
    return 0;
}

if (args is [var method, var id]) return Handle(method, id);

Console.Error.WriteLine($"Usage: OrderService <{string.Join("|", routes.Keys)}> <order id> | serve <traffic file>");
return 2;

int Handle(string method, string id)
{
    try
    {
        if (!routes.TryGetValue(method, out var route)) throw new ArgumentException($"Unknown method '{method}'");
        string result = route(int.Parse(id, CultureInfo.InvariantCulture));
        Console.WriteLine($"200 {method} {id}: {result}");
        return 0;
    }
    catch (Exception ex)
    {
        string errorId = ErrorLog.Record(method, id, ex);
        Console.WriteLine($"500 {method} {id}: {ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")} (error {errorId})");
        return 1;
    }
}

static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
