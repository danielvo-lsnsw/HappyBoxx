using System.Net.Mail;
using System.Security.Claims;
using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Identity.Api.Domain;
using HappyBoxx.Identity.Api.Infrastructure.Persistence;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Identity.Api.Features.Customers;

public sealed record RegisterCustomerRequest(
    string DisplayName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string Country);

public sealed record CustomerAccountResponse(
    Guid AccountId,
    string Email,
    string DisplayName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string Country,
    string Status,
    string Role);

internal sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerValidator()
    {
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.PhoneNumber).NotEmpty().MaximumLength(40);
        RuleFor(request => request.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(request => request.AddressLine2).MaximumLength(200);
        RuleFor(request => request.City).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Region).MaximumLength(120);
        RuleFor(request => request.PostalCode).NotEmpty().MaximumLength(32);
        RuleFor(request => request.Country).Length(2).Matches("^[A-Za-z]{2}$");
    }
}

internal sealed class RegisterCustomerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("identity/customers/register", HandleAsync)
            .WithName("RegisterCustomer")
            .WithTags("Customers")
            .WithSummary("Create an active customer account after Entra email verification")
            .RequireAuthorization(HappyBoxxPolicies.Authenticated)
            .WithRequestValidation<RegisterCustomerRequest>();

    private static async Task<Results<Created<CustomerAccountResponse>, ProblemHttpResult>> HandleAsync(
        RegisterCustomerRequest request,
        ClaimsPrincipal principal,
        IdentityDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value
            ?? principal.FindFirst("emails")?.Value;

        if (string.IsNullOrWhiteSpace(subject)
            || string.IsNullOrWhiteSpace(email)
            || !MailAddress.TryCreate(email, out _))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Verified email identity is required");
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var accountExists = await dbContext.Accounts.AnyAsync(
            account => account.ExternalSubject == subject || account.NormalizedEmail == normalizedEmail,
            cancellationToken);
        if (accountExists)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "An account already exists for this identity or email address");
        }

        var now = timeProvider.GetUtcNow();
        var account = AccessAccount.CreateCustomer(subject, email.Trim(), request.DisplayName.Trim(), now);
        var profile = CustomerProfile.Create(
            account.Id,
            request.PhoneNumber.Trim(),
            request.AddressLine1.Trim(),
            request.AddressLine2?.Trim(),
            request.City.Trim(),
            request.Region?.Trim(),
            request.PostalCode.Trim(),
            request.Country.Trim().ToUpperInvariant(),
            now);

        dbContext.Accounts.Add(account);
        dbContext.CustomerProfiles.Add(profile);
        dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
            subject,
            "CustomerAccountCreated",
            account.Id,
            null,
            now));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (SqlServerErrorCodes.IsUniqueConstraintViolation(exception))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "An account already exists for this identity or email address");
        }

        return TypedResults.Created(
            $"/api/v1/identity/accounts/{account.Id}",
            new CustomerAccountResponse(
                account.Id,
                account.Email,
                account.DisplayName,
                profile.PhoneNumber,
                profile.AddressLine1,
                profile.AddressLine2,
                profile.City,
                profile.Region,
                profile.PostalCode,
                profile.Country,
                account.Status.ToString(),
                account.Role));
    }
}