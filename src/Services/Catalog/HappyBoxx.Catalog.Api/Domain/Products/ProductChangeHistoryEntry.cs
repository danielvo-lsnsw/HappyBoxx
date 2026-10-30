using HappyBoxx.BuildingBlocks.Domain;

namespace HappyBoxx.Catalog.Api.Domain.Products;

public sealed class ProductChangeHistoryEntry : Entity
{
    private ProductChangeHistoryEntry()
    {
    }

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string FieldName { get; private set; } = string.Empty;

    public string? OldValue { get; private set; }

    public string? NewValue { get; private set; }

    public string ChangedBy { get; private set; } = string.Empty;

    public DateTimeOffset ChangedAtUtc { get; private set; }

    public static ProductChangeHistoryEntry Create(
        Guid productId,
        string fieldName,
        string? oldValue,
        string? newValue,
        string changedBy,
        DateTimeOffset changedAtUtc) =>
        new()
        {
            ProductId = productId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            ChangedAtUtc = changedAtUtc,
        };
}
