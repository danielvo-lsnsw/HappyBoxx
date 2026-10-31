namespace HappyBoxx.Billing.Api.Domain;

public sealed class InvoiceLine
{
    public const int DescriptionMaxLength = 200;
    public const int UnitMaxLength = 30;

    private InvoiceLine()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public int LineNumber { get; private set; }

    public Guid ProductId { get; private set; }

    public InvoicePriceTier PriceTier { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPriceInclusive { get; private set; }

    public decimal AmountInclusive { get; private set; }

    internal static InvoiceLine Create(
        int lineNumber,
        Guid productId,
        InvoicePriceTier priceTier,
        string description,
        string unit,
        decimal quantity,
        decimal unitPriceInclusive)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPriceInclusive);

        return new InvoiceLine
        {
            LineNumber = lineNumber,
            ProductId = productId,
            PriceTier = priceTier,
            Description = description.Trim(),
            Unit = unit.Trim(),
            Quantity = quantity,
            UnitPriceInclusive = unitPriceInclusive,
            AmountInclusive = decimal.Round(quantity * unitPriceInclusive, 2, MidpointRounding.AwayFromZero),
        };
    }
}

public enum InvoicePriceTier
{
    Near = 0,
    Far = 1,
}
