using HappyBoxx.Billing.Api.Domain;
using Shouldly;

namespace HappyBoxx.Billing.UnitTests.Domain;

public sealed class InvoiceTests
{
    [Fact]
    public void Create_CalculatesGstIncludedInLinePrices()
    {
        var invoice = Invoice.Create(
            "HB-001",
            new DateOnly(2026, 10, 31),
            Guid.CreateVersion7(),
            "Lemak Garden City",
            "Westfield Garden City Shopping Centre",
            "0400000000",
            [
                new InvoiceLineInput(Guid.CreateVersion7(), InvoicePriceTier.Near, "Onions", "Bag", 3m, 28m),
                new InvoiceLineInput(Guid.CreateVersion7(), InvoicePriceTier.Near, "Shallots", "Bunch", 5m, 1.99m),
            ]);

        invoice.Total.ShouldBe(93.95m);
        invoice.GstAmount.ShouldBe(8.54m);
        invoice.Subtotal.ShouldBe(85.41m);
        invoice.Lines.Select(line => line.AmountInclusive).ShouldBe([84m, 9.95m]);
    }

    [Fact]
    public void Create_RoundsLineAmountsToCents()
    {
        var invoice = Invoice.Create(
            "HB-002",
            new DateOnly(2026, 10, 31),
            Guid.CreateVersion7(),
            "MK Chinese",
            "North Lakes",
            null,
            [new InvoiceLineInput(Guid.CreateVersion7(), InvoicePriceTier.Far, "Product", "Carton", 1.25m, 2.99m)]);

        invoice.Total.ShouldBe(3.74m);
        invoice.GstAmount.ShouldBe(0.34m);
        invoice.Subtotal.ShouldBe(3.40m);
    }
}
