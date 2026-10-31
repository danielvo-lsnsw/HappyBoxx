using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.Invoices;

internal sealed class GetInvoiceByIdHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<InvoiceResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(item => item.Lines)
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return invoice is null ? InvoiceErrors.NotFound(id) : InvoiceResponse.From(invoice, invoice.Customer?.Email);
    }
}

internal sealed class GetInvoiceByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("invoices/{id:guid}", HandleAsync)
            .WithName("GetInvoiceById")
            .WithTags(Tags.Invoices)
            .WithSummary("Get an invoice")
            .RequireAuthorization(HappyBoxxPolicies.Staff);

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        GetInvoiceByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
