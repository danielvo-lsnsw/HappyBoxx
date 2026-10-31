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

public sealed record RecordInvoicePaymentRequest(
    DateOnly PaidOn,
    string? PaymentReference,
    string? PaymentNote,
    bool SendReceiptEmail,
    IReadOnlyList<string>? ReceiptToEmails,
    bool IncludeInvoice);

internal sealed class RecordInvoicePaymentValidator : AbstractValidator<RecordInvoicePaymentRequest>
{
    public RecordInvoicePaymentValidator()
    {
        RuleFor(request => request.PaidOn).NotEqual(default(DateOnly));
        RuleFor(request => request.PaymentReference).MaximumLength(Invoice.PaymentReferenceMaxLength);
        RuleFor(request => request.PaymentNote).MaximumLength(Invoice.PaymentNoteMaxLength);
        RuleForEach(request => request.ReceiptToEmails).EmailAddress();
        RuleFor(request => request.ReceiptToEmails)
            .Must(emails => emails is null || emails.Count <= 20)
            .WithMessage("At most 20 receipt recipients are allowed.");
    }
}

internal sealed class RecordInvoicePaymentHandler(BillingDbContext dbContext, IInvoiceEmailSender emailSender) : IHandler
{
    public async Task<Result<InvoiceResponse>> HandleAsync(
        Guid id,
        RecordInvoicePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
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

        invoice.RecordPayment(request.PaidOn, request.PaymentReference, request.PaymentNote);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.SendReceiptEmail)
        {
            if (!emailSender.IsEnabled)
            {
                return Error.Validation(
                    "Invoice.EmailDisabled",
                    "Invoice email delivery is disabled. Configure Email settings for Billing API.");
            }

            var recipients = (request.ReceiptToEmails ?? [])
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

            await emailSender.SendReceiptAsync(
                recipients,
                emailInvoice,
                new InvoicePaymentReceiptDetails(request.PaidOn, request.PaymentReference, request.PaymentNote),
                request.IncludeInvoice,
                cancellationToken);
        }

        return InvoiceResponse.From(invoice, customer.Email);
    }
}

internal sealed class RecordInvoicePaymentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("invoices/{id:guid}/payment", HandleAsync)
            .WithName("RecordInvoicePayment")
            .WithTags(Tags.Invoices)
            .WithSummary("Record invoice payment")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<RecordInvoicePaymentRequest>();

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        RecordInvoicePaymentRequest request,
        RecordInvoicePaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
