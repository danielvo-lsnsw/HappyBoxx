using FluentValidation;
using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.Billing.Api.Features.Invoices;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Pagination;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.Customers;

public sealed record ListCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = true) : IPagedQuery;

internal sealed class ListCustomersValidator : AbstractValidator<ListCustomersQuery>
{
    public ListCustomersValidator()
    {
        this.AddPagingRules();
        RuleFor(query => query.Search).MaximumLength(Customer.NameMaxLength);
    }
}

internal sealed class ListCustomersHandler(BillingDbContext dbContext) : IHandler
{
    public Task<PagedResult<CustomerResponse>> HandleAsync(ListCustomersQuery query, CancellationToken cancellationToken)
    {
        var customers = dbContext.Customers.AsNoTracking();

        if (query.IsActive.HasValue)
        {
            customers = customers.Where(customer => customer.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            customers = customers.Where(customer =>
                customer.Name.Contains(search) ||
                customer.Address.Contains(search) ||
                (customer.Phone != null && customer.Phone.Contains(search)) ||
                (customer.Email != null && customer.Email.Contains(search)) ||
                (customer.Website != null && customer.Website.Contains(search)) ||
                (customer.Abn != null && customer.Abn.Contains(search)) ||
                (customer.CustomerType != null && customer.CustomerType.Contains(search)));
        }

        return customers
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerResponse(
                customer.Id,
                customer.Name,
                customer.Address,
                customer.Phone,
                customer.Email,
                customer.Website,
                customer.Abn,
                customer.CustomerType,
                customer.IsActive,
                customer.UpdatedAtUtc))
            .ToPagedResultAsync(query, cancellationToken);
    }
}

internal sealed class ListCustomersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("customers", HandleAsync)
            .WithName("ListCustomers")
            .WithTags(Tags.Customers)
            .WithSummary("List customers")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<ListCustomersQuery>();

    private static async Task<Ok<PagedResult<CustomerResponse>>> HandleAsync(
        [AsParameters] ListCustomersQuery query,
        ListCustomersHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
