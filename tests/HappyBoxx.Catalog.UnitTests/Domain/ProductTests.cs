using HappyBoxx.Catalog.Api.Domain.Categories;
using HappyBoxx.Catalog.Api.Domain.Products;
using Shouldly;

namespace HappyBoxx.Catalog.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidInput_NormalisesSkuAndIsActive()
    {
        var category = Category.Create("FRU", "Fruits", null);

        var product = Product.Create(
            "  app-001 ",
            null,
            " Apple ",
            null,
            category.Id,
            UnitOfMeasure.Kilogram,
            null,
            3.50m,
            null,
            null,
            null);

        product.Sku.ShouldBe("APP-001");
        product.Name.ShouldBe("Apple");
        product.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Product.Create("APP-001", null, "Apple", null, Guid.NewGuid(), UnitOfMeasure.Kilogram, null, -1m, null, null, null));
    }
}
