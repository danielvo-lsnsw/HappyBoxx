using HappyBoxx.BuildingBlocks.Domain;

namespace HappyBoxx.Catalog.Api.Domain.Suppliers;

public sealed class Supplier : AuditableEntity
{
    public const int NameMaxLength = 200;

    private readonly List<ProductSupplier> _productSuppliers = [];

    private Supplier()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<ProductSupplier> ProductSuppliers => _productSuppliers;

    public static Supplier Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Supplier
        {
            Name = name.Trim(),
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
