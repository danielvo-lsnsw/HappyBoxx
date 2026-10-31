using FluentValidation;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Pagination;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.Invoices;

public sealed record ListInvoicesQuery(int Page = 1, int PageSize = 20, string? Search = null) : IPagedQuery;

internal sealed class ListInvoicesValidator : AbstractValidator<ListInvoicesQuery>
{
    public ListInvoicesValidator()
    {
        this.AddPagingRules();
        RuleFor(query => query.Search).MaximumLength(150);
    }
}

internal sealed class ListInvoicesHandler(BillingDbContext dbContext) : IHandler
{
    public Task<PagedResult<InvoiceSummaryResponse>> HandleAsync(
        ListInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var invoices = dbContext.Invoices.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            invoices = invoices.Where(invoice =>
                invoice.InvoiceNumber.Contains(search) || invoice.CustomerName.Contains(search));
        }

        return invoices
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenByDescending(invoice => invoice.CreatedAtUtc)
            .Select(invoice => new InvoiceSummaryResponse(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.InvoiceDate,
                invoice.CustomerId,
                invoice.CustomerName,
                invoice.Total,
                invoice.PaidOn))
            .ToPagedResultAsync(query, cancellationToken);
    }
}

internal sealed class ListInvoicesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("invoices", HandleAsync)
            .WithName("ListInvoices")
            .WithTags(Tags.Invoices)
            .WithSummary("List invoices")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<ListInvoicesQuery>();

    private static async Task<Ok<PagedResult<InvoiceSummaryResponse>>> HandleAsync(
        [AsParameters] ListInvoicesQuery query,
        ListInvoicesHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
