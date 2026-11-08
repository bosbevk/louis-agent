using System.Globalization;

namespace OrderService;

public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);

/// <summary>An order as stored. <see cref="Created"/> is kept as written (an ISO date, sometimes with a time).</summary>
public sealed record Order(
    int Id, string Customer, IReadOnlyList<OrderItem> Items, string DiscountCode, string PaymentRef, bool Refunded,
    string Created, string Country, string? GiftMessage);

/// <summary>A client asked for an order that doesn't exist (a 404, not a bug).</summary>
public sealed class OrderNotFoundException(int id) : Exception($"Order {id} not found");

/// <summary>Reads orders from data/orders.csv: id,customer,items,discount_code,payment_ref,refunded,created,country,gift_message.</summary>
public sealed class OrderRepository
{
    private readonly Dictionary<int, Order> _orders;

    public OrderRepository(string csvPath)
    {
        _orders = File.ReadLines(csvPath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(Parse)
            .ToDictionary(order => order.Id);
    }

    public Order Get(int id) => _orders.TryGetValue(id, out var order) ? order : throw new OrderNotFoundException(id);

    // Items look like "SKU:quantity:unitPrice|SKU:quantity:unitPrice".
    private static Order Parse(string line)
    {
        string[] f = line.Split(',');
        var items = f[2].Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split(':'))
            .Select(p => new OrderItem(p[0], int.Parse(p[1], CultureInfo.InvariantCulture), decimal.Parse(p[2], CultureInfo.InvariantCulture)))
            .ToList();
        return new Order(int.Parse(f[0], CultureInfo.InvariantCulture), f[1], items, f[3], f[4], bool.Parse(f[5]),
            f[6], f[7], f[8].Length == 0 ? null : f[8]);
    }
}
