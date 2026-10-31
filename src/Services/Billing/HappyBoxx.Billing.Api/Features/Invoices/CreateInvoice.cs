using FluentValidation;
using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.Invoices;

public sealed record CreateInvoiceLineRequest(
    Guid ProductId,
    InvoicePriceTier PriceTier,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPriceInclusive);

public sealed record CreateInvoiceRequest(
    string InvoiceNumber,
    DateOnly InvoiceDate,
    Guid CustomerId,
    IReadOnlyList<CreateInvoiceLineRequest> Lines);

internal sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(request => request.InvoiceNumber)
            .NotEmpty()
            .MaximumLength(Invoice.InvoiceNumberMaxLength);
        RuleFor(request => request.InvoiceDate).NotEqual(default(DateOnly));
        RuleFor(request => request.CustomerId).NotEmpty();
        RuleFor(request => request.Lines)
            .NotEmpty()
            .Must(lines => lines.Count <= 100)
            .WithMessage("An invoice can include at most 100 lines.");
        RuleForEach(request => request.Lines).ChildRules(line =>
        {
            line.RuleFor(item => item.ProductId).NotEmpty();
            line.RuleFor(item => item.PriceTier).IsInEnum();
            line.RuleFor(item => item.Description)
                .NotEmpty()
                .MaximumLength(InvoiceLine.DescriptionMaxLength);
            line.RuleFor(item => item.Unit)
                .NotEmpty()
                .MaximumLength(InvoiceLine.UnitMaxLength);
            line.RuleFor(item => item.Quantity)
                .GreaterThan(0)
                .PrecisionScale(18, 3, ignoreTrailingZeros: true);
            line.RuleFor(item => item.UnitPriceInclusive)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true);
        });
    }
}

internal sealed class CreateInvoiceHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<InvoiceResponse>> HandleAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var number = request.InvoiceNumber.Trim();
        if (await dbContext.Invoices.AnyAsync(invoice => invoice.InvoiceNumber == number, cancellationToken))
        {
            return InvoiceErrors.NumberAlreadyExists(number);
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.CustomerId && item.IsActive, cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(request.CustomerId);
        }

        var invoice = Invoice.Create(
            number,
            request.InvoiceDate,
            request.CustomerId,
            customer.Name,
            customer.Address,
            customer.Phone,
            request.Lines.Select(line => new InvoiceLineInput(
                line.ProductId,
                line.PriceTier,
                line.Description,
                line.Unit,
                line.Quantity,
                line.UnitPriceInclusive)));

        dbContext.Invoices.Add(invoice);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return InvoiceErrors.NumberAlreadyExists(number);
        }

        return InvoiceResponse.From(invoice, customer.Email);
    }
}

internal sealed class CreateInvoiceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("invoices", HandleAsync)
            .WithName("CreateInvoice")
            .WithTags(Tags.Invoices)
            .WithSummary("Create an invoice")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<CreateInvoiceRequest>();

    private static async Task<Results<CreatedAtRoute<InvoiceResponse>, ProblemHttpResult>> HandleAsync(
        CreateInvoiceRequest request,
        CreateInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetInvoiceById", new { id = result.Value.Id })
            : result.Error.ToProblem();
    }
}
