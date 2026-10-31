using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Features.Invoices;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.Customers;

internal sealed class GetCustomerByIdHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<CustomerResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return customer is null ? CustomerErrors.NotFound(id) : CustomerResponse.From(customer);
    }
}

internal sealed class GetCustomerByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("customers/{id:guid}", HandleAsync)
            .WithName("GetCustomerById")
            .WithTags(Tags.Customers)
            .WithSummary("Get a customer")
            .RequireAuthorization(HappyBoxxPolicies.Staff);

    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        GetCustomerByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
