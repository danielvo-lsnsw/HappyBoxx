using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Catalog.Api.Domain.Categories;
using HappyBoxx.Catalog.Api.Infrastructure.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Catalog.Api.Features.Categories;

public sealed record UpdateCategoryRequest(string Name, string? Description, bool IsActive);

internal sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Category.DescriptionMaxLength);
    }
}

internal sealed class UpdateCategoryHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<Result<CategoryResponse>> HandleAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(id);
        }

        var name = request.Name.Trim();
        if (await dbContext.Categories.AnyAsync(c => c.Id != id && c.Name == name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists(name);
        }

        category.Update(name, request.Description, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CategoryResponse.From(category);
    }
}

internal sealed class UpdateCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("categories/{id:guid}", HandleAsync)
            .WithName("UpdateCategory")
            .WithTags(Tags.Categories)
            .WithSummary("Update a category")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<UpdateCategoryRequest>();

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        UpdateCategoryRequest request,
        UpdateCategoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
