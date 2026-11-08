using OrderService;

namespace OrderService.Tests;

/// <summary>The behaviour that works today. Regression tests for fixed bugs go in their own files under Regression/.</summary>
public class OrderApiTests
{
    internal static OrderApi Api()
    {
        string csv = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "data", "orders.csv");
        return new OrderApi(new OrderRepository(csv));
    }

    [Test]
    public void GetOrder_SummarisesTheOrder() =>
        Assert.That(Api().GetOrder(1001), Is.EqualTo("Order 1001 for Ada Lovelace: 2 item(s), starting with BOOK-1"));

    [Test]
    public void GetOrder_Unknown_ThrowsNotFound() =>
        Assert.Throws<OrderNotFoundException>(() => Api().GetOrder(9999));

    [Test]
    public void GetOrderTotal_WithoutDiscount_IsTheSubtotal() =>
        Assert.That(Api().GetOrderTotal(1002), Is.EqualTo(899.00m));

    [Test]
    public void GetOrderTotal_AppliesTheDiscountCode() =>
        Assert.That(Api().GetOrderTotal(1001), Is.EqualTo(26.10m));

    [Test]
    public void GetShippingCost_IsFreeFrom100() =>
        Assert.That(Api().GetShippingCost(1002), Is.EqualTo(0m));

    [Test]
    public void GetShippingCost_BelowTheThreshold_UsesTheParcelClass() =>
        Assert.That(Api().GetShippingCost(1001), Is.EqualTo(4.95m));

    [Test]
    public void GetInvoiceNumber_UsesTheCreatedMonth() =>
        Assert.That(Api().GetInvoiceNumber(1001), Is.EqualTo("INV-202609-1001"));

    [Test]
    public void GetPackingSlip_ShowsTheGiftMessageInCapitals() =>
        Assert.That(Api().GetPackingSlip(1002), Is.EqualTo("1 x LAPTOP-9 | GIFT: HAPPY BIRTHDAY"));

    [Test]
    public void GetLoyaltyPoints_OnePointPerTen() =>
        Assert.That(Api().GetLoyaltyPoints(1002), Is.EqualTo(89));

    [Test]
    public void GetVat_UsesTheCountryRate() =>
        Assert.That(Api().GetVat(1002), Is.EqualTo(143.54m));

    [Test]
    public void GetDeliveryEstimate_AddsDispatchAndTransitDays() =>
        Assert.That(Api().GetDeliveryEstimate(1001), Is.EqualTo("2026-10-01"));

    [Test]
    public void GetDiscountLabel_ShowsThePercentage() =>
        Assert.That(Api().GetDiscountLabel(1001), Is.EqualTo("WELCOME (10% off)"));

    [Test]
    public void GetCustomerInitials_FirstAndLastName() =>
        Assert.That(Api().GetCustomerInitials(1001), Is.EqualTo("AL"));

    [Test]
    public void RefundOrder_AlreadyRefunded_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() => Api().RefundOrder(1002));
}
