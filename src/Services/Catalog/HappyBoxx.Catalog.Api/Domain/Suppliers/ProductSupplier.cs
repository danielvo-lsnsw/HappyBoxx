namespace HappyBoxx.Catalog.Api.Domain.Suppliers;

public sealed class ProductSupplier
{
    public const int NoteMaxLength = 2000;

    private ProductSupplier()
    {
    }

    public Guid ProductId { get; private set; }

    public Guid SupplierId { get; private set; }

    public bool IsDefault { get; private set; }

    public decimal? LastBuyPrice { get; private set; }

    public DateTimeOffset? LastBuyPriceAtUtc { get; private set; }

    public string? Note { get; private set; }

    public Products.Product Product { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    public static ProductSupplier Create(Guid productId, Guid supplierId, bool isDefault) =>
        new()
        {
            ProductId = productId,
            SupplierId = supplierId,
            IsDefault = isDefault,
        };

    public void SetDefault(bool isDefault) => IsDefault = isDefault;

    public void SetLastBuyPrice(decimal? price, DateTimeOffset? atUtc)
    {
        LastBuyPrice = price;
        LastBuyPriceAtUtc = atUtc;
    }

    public void SetNote(string? note) => Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
