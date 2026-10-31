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
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace HappyBoxx.Billing.Api.Features.Customers;

public sealed record CreateCustomerRequest(
    string Name,
    string Address,
    string Phone,
    string? Email,
    string? Website,
    string? Abn,
    string? CustomerType);

internal static partial class CustomerValidation
{
    public static readonly string[] AllowedCustomerTypes =
    [
        "Restaurant",
        "Grocery shop",
        "Convenience store",
        "Cafe",
        "Bakery",
        "Butcher",
        "Other"
    ];

    [GeneratedRegex("^\\d{11}$")]
    private static partial Regex AbnRegex();

    public static bool BeValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            _ = new MailAddress(value.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool BeValidWebsite(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var website = value.Trim();
        if (!website.Contains("://", StringComparison.Ordinal))
        {
            website = $"https://{website}";
        }

        return Uri.TryCreate(website, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
               !string.IsNullOrWhiteSpace(uri.Host);
    }

    public static bool BeValidAbn(string? value) =>
        string.IsNullOrWhiteSpace(value) || AbnRegex().IsMatch(value.Trim());

    public static bool BeValidCustomerType(string? value) =>
        string.IsNullOrWhiteSpace(value) || AllowedCustomerTypes.Contains(value.Trim(), StringComparer.Ordinal);

    public static string? NormalizeWebsite(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var website = value.Trim();
        return website.Contains("://", StringComparison.Ordinal) ? website : $"https://{website}";
    }
}

internal sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
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

internal sealed class CreateCustomerHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<CustomerResponse>> HandleAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await dbContext.Customers.AnyAsync(customer => customer.Name == name, cancellationToken))
        {
            return CustomerErrors.NameAlreadyExists(name);
        }

        var normalizedWebsite = CustomerValidation.NormalizeWebsite(request.Website);
        var customer = Customer.Create(
            name,
            request.Address,
            request.Phone,
            request.Email,
            normalizedWebsite,
            request.Abn,
            request.CustomerType);
        dbContext.Customers.Add(customer);

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

internal sealed class CreateCustomerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("customers", HandleAsync)
            .WithName("CreateCustomer")
            .WithTags(Tags.Customers)
            .WithSummary("Create a customer")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<CreateCustomerRequest>();

    private static async Task<Results<CreatedAtRoute<CustomerResponse>, ProblemHttpResult>> HandleAsync(
        CreateCustomerRequest request,
        CreateCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "ListCustomers")
            : result.Error.ToProblem();
    }
}
