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

public sealed record CreateCategoryRequest(string Name, string? Description);

internal sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Category.DescriptionMaxLength);
    }
}

internal sealed class CreateCategoryHandler(CatalogDbContext dbContext) : IHandler
{
    public async Task<Result<CategoryResponse>> HandleAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await dbContext.Categories.AnyAsync(c => c.Name == name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists(name);
        }

        var category = Category.Create(name, request.Description);
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CategoryResponse.From(category);
    }
}

internal sealed class CreateCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("categories", HandleAsync)
            .WithName("CreateCategory")
            .WithTags(Tags.Categories)
            .WithSummary("Create a category")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<CreateCategoryRequest>();

    private static async Task<Results<CreatedAtRoute<CategoryResponse>, ProblemHttpResult>> HandleAsync(
        CreateCategoryRequest request,
        CreateCategoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCategoryById", new { id = result.Value.Id })
            : result.Error.ToProblem();
    }
}
