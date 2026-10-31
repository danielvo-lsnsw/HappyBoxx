using FluentValidation;
using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.DeliverySchedules;

public sealed record CreateDeliveryScheduleRequest(
    IReadOnlyList<Guid> InvoiceIds,
    DateOnly? ScheduledDate,
    string? Name);

internal sealed class CreateDeliveryScheduleValidator : AbstractValidator<CreateDeliveryScheduleRequest>
{
    public CreateDeliveryScheduleValidator()
    {
        RuleFor(request => request.InvoiceIds)
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Invoice IDs must be unique.")
            .Must(ids => ids.Count <= 300)
            .WithMessage("A schedule can include at most 300 invoices.");
        RuleForEach(request => request.InvoiceIds).NotEmpty();
        RuleFor(request => request.Name).MaximumLength(DeliverySchedule.NameMaxLength);
    }
}

internal sealed class CreateDeliveryScheduleHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<DeliveryScheduleResponse>> HandleAsync(
        CreateDeliveryScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var invoiceIds = request.InvoiceIds.Distinct().ToArray();
        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoiceIds.Contains(invoice.Id))
            .Select(invoice => new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.InvoiceDate,
                invoice.CustomerName,
                invoice.CustomerAddress,
                invoice.CustomerPhone,
                invoice.Total
            })
            .ToListAsync(cancellationToken);

        if (invoices.Count != invoiceIds.Length)
        {
            var existing = invoices.Select(invoice => invoice.Id).ToHashSet();
            var missingId = invoiceIds.First(id => !existing.Contains(id));
            return InvoiceErrors.NotFound(missingId);
        }

        var scheduledDate = request.ScheduledDate ?? DateOnly.FromDateTime(DateTime.Today);
        var schedule = DeliverySchedule.Create(
            scheduledDate,
            request.Name,
            invoices.Select(invoice => new DeliveryScheduleInvoiceInput(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.InvoiceDate,
                invoice.CustomerName,
                invoice.CustomerAddress,
                invoice.CustomerPhone,
                invoice.Total)));

        dbContext.DeliverySchedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DeliveryScheduleResponse.From(schedule);
    }
}

internal sealed class CreateDeliveryScheduleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("delivery-schedules", HandleAsync)
            .WithName("CreateDeliverySchedule")
            .WithTags(Tags.DeliverySchedules)
            .WithSummary("Create delivery schedule from invoices")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<CreateDeliveryScheduleRequest>();

    private static async Task<Results<CreatedAtRoute<DeliveryScheduleResponse>, ProblemHttpResult>> HandleAsync(
        CreateDeliveryScheduleRequest request,
        CreateDeliveryScheduleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetDeliveryScheduleById", new { id = result.Value.Id })
            : result.Error.ToProblem();
    }
}
