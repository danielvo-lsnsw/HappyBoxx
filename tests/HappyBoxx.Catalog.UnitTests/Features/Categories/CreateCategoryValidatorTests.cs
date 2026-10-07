using HappyBoxx.Catalog.Api.Features.Categories;
using Shouldly;

namespace HappyBoxx.Catalog.UnitTests.Features.Categories;

public sealed class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_IsInvalid(string name)
    {
        var result = _validator.Validate(new CreateCategoryRequest(name, null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateCategoryRequest.Name));
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var result = _validator.Validate(new CreateCategoryRequest("Vegetables", "Fresh vegetables"));

        result.IsValid.ShouldBeTrue();
    }
}
