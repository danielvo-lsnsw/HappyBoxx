using FluentValidation;
using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Infrastructure.Email;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.Invoices;

public sealed record EmailInvoiceRequest(IReadOnlyList<string>? ToEmails, bool IncludeInvoice, string? Subject, string? Message);

internal sealed class EmailInvoiceValidator : AbstractValidator<EmailInvoiceRequest>
{
    public EmailInvoiceValidator()
    {
        RuleForEach(request => request.ToEmails).EmailAddress();
        RuleFor(request => request.ToEmails)
            .Must(emails => emails is null || emails.Count <= 20)
            .WithMessage("At most 20 recipients are allowed.");
        RuleFor(request => request.Subject).MaximumLength(200);
        RuleFor(request => request.Message).MaximumLength(4000);
    }
}

internal sealed class EmailInvoiceHandler(BillingDbContext dbContext, IInvoiceEmailSender emailSender) : IHandler
{
    public async Task<Result> HandleAsync(Guid id, EmailInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (!emailSender.IsEnabled)
        {
            return Error.Validation(
                "Invoice.EmailDisabled",
                "Invoice email delivery is disabled. Configure Email settings for Billing API.");
        }

        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.NotFound(id);
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == invoice.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound(invoice.CustomerId);
        }

        var recipients = (request.ToEmails ?? [])
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (recipients.Length == 0 && !string.IsNullOrWhiteSpace(customer.Email))
        {
            recipients = [customer.Email.Trim()];
        }

        if (recipients.Length == 0)
        {
            return CustomerErrors.EmailMissing(customer.Id);
        }

        var emailInvoice = new InvoiceEmailContent(
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.CustomerName,
            invoice.CustomerAddress,
            invoice.Subtotal,
            invoice.GstAmount,
            invoice.Total,
            invoice.Lines
                .OrderBy(line => line.LineNumber)
                .Select(line => new InvoiceEmailLine(
                    line.Description,
                    line.Unit,
                    line.Quantity,
                    line.UnitPriceInclusive,
                    line.AmountInclusive))
                .ToArray());

        if (invoice.PaidOn is not null)
        {
            await emailSender.SendPaidInvoicePackageAsync(
                recipients,
                emailInvoice,
                new InvoicePaymentReceiptDetails(invoice.PaidOn.Value, invoice.PaymentReference, invoice.PaymentNote),
                request.IncludeInvoice,
                request.Subject,
                request.Message,
                cancellationToken);
        }
        else
        {
            await emailSender.SendInvoiceAsync(
                recipients,
                emailInvoice,
                request.Subject,
                request.Message,
                cancellationToken);
        }

        return Result.Success();
    }
}

internal sealed class EmailInvoiceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("invoices/{id:guid}/email", HandleAsync)
            .WithName("EmailInvoice")
            .WithTags(Tags.Invoices)
            .WithSummary("Email an invoice")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<EmailInvoiceRequest>();

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        EmailInvoiceRequest request,
        EmailInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
