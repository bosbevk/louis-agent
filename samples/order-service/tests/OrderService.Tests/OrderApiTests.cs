using OrderService;

namespace OrderService.Tests;

public class OrderApiTests
{
    private static OrderApi Api()
    {
        string csv = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "data", "orders.csv");
        return new OrderApi(new OrderRepository(csv));
    }

    [Test]
    public void GetOrderTotal_WithoutDiscount_IsTheSubtotal() =>
        Assert.That(Api().GetOrderTotal(1002), Is.EqualTo(899.00m));

    [Test]
    public void GetOrderTotal_AppliesTheDiscountCode() =>
        Assert.That(Api().GetOrderTotal(1001), Is.EqualTo(26.10m));

    [Test]
    public void GetOrder_Unknown_ThrowsNotFound() =>
        Assert.Throws<OrderNotFoundException>(() => Api().GetOrder(9999));

    [Test]
    public void RefundOrder_AlreadyRefunded_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() => Api().RefundOrder(1002));
}
