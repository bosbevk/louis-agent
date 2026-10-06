using System.Globalization;
using OrderService;

// A tiny stand-in for a microservice: each "request" is an API method name and an order id.
//   dotnet run -- <method> <order id>      one call, e.g. "order-total 1003"
//   dotnet run -- serve data/requests.txt  replay a traffic file, one call per line
// Unhandled exceptions are logged to logs/errors.jsonl, the way a real service would report them to its error tracker.
var api = new OrderApi(new OrderRepository(Path.Combine("data", "orders.csv")));

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

Console.Error.WriteLine("Usage: OrderService <get-order|order-total|refund> <order id> | serve <traffic file>");
return 2;

int Handle(string method, string id)
{
    try
    {
        int orderId = int.Parse(id, CultureInfo.InvariantCulture);
        string result = method switch
        {
            "get-order" => api.GetOrder(orderId),
            "order-total" => api.GetOrderTotal(orderId).ToString("0.00", CultureInfo.InvariantCulture),
            "refund" => api.RefundOrder(orderId),
            _ => throw new ArgumentException($"Unknown method '{method}'"),
        };
        Console.WriteLine($"200 {method} {id}: {result}");
        return 0;
    }
    catch (Exception ex)
    {
        string errorId = ErrorLog.Record(method, id, ex);
        Console.WriteLine($"500 {method} {id}: {ex.GetType().Name}: {ex.Message} (error {errorId})");
        return 1;
    }
}
