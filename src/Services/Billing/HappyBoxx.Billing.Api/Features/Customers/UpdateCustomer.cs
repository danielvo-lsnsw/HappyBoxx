using FluentValidation;
using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Features.Invoices;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.Customers;

public sealed record UpdateCustomerRequest(
    string Name,
    string Address,
    string Phone,
    string? Email,
    string? Website,
    string? Abn,
    string? CustomerType,
    bool IsActive);

internal sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(Customer.NameMaxLength);
        RuleFor(request => request.Address).NotEmpty().MaximumLength(Customer.AddressMaxLength);
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(Customer.PhoneMaxLength);
        RuleFor(request => request.Email)
            .MaximumLength(Customer.EmailMaxLength)
            .Must(CustomerValidation.BeValidEmail)
            .WithMessage("Email is invalid.");
        RuleFor(request => request.Website)
            .MaximumLength(Customer.WebsiteMaxLength)
            .Must(CustomerValidation.BeValidWebsite)
            .WithMessage("Website is invalid.");
        RuleFor(request => request.Abn)
            .MaximumLength(Customer.AbnMaxLength)
            .Must(CustomerValidation.BeValidAbn)
            .WithMessage("ABN must be exactly 11 digits.");
        RuleFor(request => request.CustomerType)
            .MaximumLength(Customer.CustomerTypeMaxLength)
            .Must(CustomerValidation.BeValidCustomerType)
            .WithMessage("Customer type is invalid.");
    }
}

internal sealed class UpdateCustomerHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<CustomerResponse>> HandleAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound(id);
        }

        var name = request.Name.Trim();
        var duplicateNameExists = await dbContext.Customers
            .AnyAsync(item => item.Id != id && item.Name == name, cancellationToken);
        if (duplicateNameExists)
        {
            return CustomerErrors.NameAlreadyExists(name);
        }

        customer.Update(
            name,
            request.Address,
            request.Phone,
            request.Email,
            CustomerValidation.NormalizeWebsite(request.Website),
            request.Abn,
            request.CustomerType,
            request.IsActive);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return CustomerErrors.NameAlreadyExists(name);
        }

        return CustomerResponse.From(customer);
    }
}

internal sealed class UpdateCustomerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("customers/{id:guid}", HandleAsync)
            .WithName("UpdateCustomer")
            .WithTags(Tags.Customers)
            .WithSummary("Update a customer")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<UpdateCustomerRequest>();

    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        UpdateCustomerRequest request,
        UpdateCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
