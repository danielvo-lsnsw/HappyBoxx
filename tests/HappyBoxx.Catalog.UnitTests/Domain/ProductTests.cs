using HappyBoxx.Catalog.Api.Domain.Categories;
using HappyBoxx.Catalog.Api.Domain.Products;
using Shouldly;

namespace HappyBoxx.Catalog.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidInput_NormalisesSkuAndIsActive()
    {
        var category = Category.Create("Fruits", null);

        var product = Product.Create("  app-001 ", " Apple ", null, category.Id, UnitOfMeasure.Kilogram, 3.50m);

        product.Sku.ShouldBe("APP-001");
        product.Name.ShouldBe("Apple");
        product.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Product.Create("APP-001", "Apple", null, Guid.NewGuid(), UnitOfMeasure.Kilogram, -1m));
    }
}
