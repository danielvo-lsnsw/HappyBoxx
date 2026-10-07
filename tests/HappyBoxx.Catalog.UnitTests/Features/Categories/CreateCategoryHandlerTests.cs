using HappyBoxx.Catalog.Api.Domain.Categories;
using HappyBoxx.Catalog.Api.Features.Categories;
using HappyBoxx.BuildingBlocks.Results;
using Shouldly;

namespace HappyBoxx.Catalog.UnitTests.Features.Categories;

public sealed class CreateCategoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithNewName_CreatesCategory()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCategoryHandler(db);

        var result = await handler.HandleAsync(new CreateCategoryRequest("Fruits", null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Fruits");
        db.Categories.Count().ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateName_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Categories.Add(Category.Create("Fruits", null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateCategoryHandler(db);

        var result = await handler.HandleAsync(new CreateCategoryRequest(" Fruits ", null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Category.NameAlreadyExists");
    }
}
