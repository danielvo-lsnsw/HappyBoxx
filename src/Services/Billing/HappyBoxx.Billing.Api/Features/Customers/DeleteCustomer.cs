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

internal sealed class DeleteCustomerHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound(id);
        }

        customer.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class DeleteCustomerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("customers/{id:guid}", HandleAsync)
            .WithName("DeleteCustomer")
            .WithTags(Tags.Customers)
            .WithSummary("Delete a customer")
            .RequireAuthorization(HappyBoxxPolicies.Staff);

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        DeleteCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
