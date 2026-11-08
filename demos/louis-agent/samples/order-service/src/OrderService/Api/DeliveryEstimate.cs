using System.Globalization;

namespace OrderService;

public sealed partial class OrderApi
{
    // Days until the warehouse dispatches an order placed on Monday..Saturday.
    private static readonly int[] DispatchDays = [1, 1, 1, 1, 3, 2];
    private const int TransitDays = 2;

    /// <summary>GET /orders/{id}/delivery-estimate - the expected delivery date.</summary>
    public string GetDeliveryEstimate(int id)
    {
        Order order = _orders.Get(id);
        DateTime created = DateTime.Parse(order.Created, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        int dispatchIn = DispatchDays[(int)created.DayOfWeek - 1];
        return created.Date.AddDays(dispatchIn + TransitDays).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
