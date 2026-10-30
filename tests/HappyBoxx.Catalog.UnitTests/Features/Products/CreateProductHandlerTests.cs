using HappyBoxx.Catalog.Api.Domain.Products;
using HappyBoxx.Catalog.Api.Features.Products;
using Shouldly;

namespace HappyBoxx.Catalog.UnitTests.Features.Products;

public sealed class CreateProductHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithUnknownCategory_ReturnsCategoryNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateProductHandler(db, TimeProvider.System);
        var categoryId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new CreateProductRequest("TOM-001", null, "Tomato", null, categoryId, UnitOfMeasure.Kilogram, null, 2.10m, null, null, null, null, null),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.CategoryNotFound(categoryId));
    }
}
