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

public sealed record UpdateDeliveryScheduleItemStatusRequest(
    DeliveryScheduleItemStatus Status,
    DateTimeOffset? DeliveredAtUtc,
    string? ReasonNote);

internal sealed class UpdateDeliveryScheduleItemStatusValidator : AbstractValidator<UpdateDeliveryScheduleItemStatusRequest>
{
    public UpdateDeliveryScheduleItemStatusValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => status is DeliveryScheduleItemStatus.Delivered or DeliveryScheduleItemStatus.Undeliverable)
            .WithMessage("Status must be Delivered or Undeliverable.");

        RuleFor(request => request.DeliveredAtUtc)
            .NotNull()
            .When(request => request.Status == DeliveryScheduleItemStatus.Delivered);

        RuleFor(request => request.ReasonNote)
            .NotEmpty()
            .When(request => request.Status == DeliveryScheduleItemStatus.Undeliverable);
        RuleFor(request => request.ReasonNote)
            .MaximumLength(DeliverySchedule.UndeliveredReasonMaxLength);
    }
}

internal sealed class UpdateDeliveryScheduleItemStatusHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<DeliveryScheduleResponse>> HandleAsync(
        Guid scheduleId,
        Guid itemId,
        UpdateDeliveryScheduleItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await dbContext.DeliverySchedules
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);
        if (schedule is null)
        {
            return DeliveryScheduleErrors.NotFound(scheduleId);
        }

        if (schedule.Items.All(item => item.Id != itemId))
        {
            return DeliveryScheduleErrors.ItemNotFound(scheduleId, itemId);
        }

        if (request.Status == DeliveryScheduleItemStatus.Delivered)
        {
            schedule.MarkItemDelivered(itemId, request.DeliveredAtUtc ?? DateTimeOffset.UtcNow);
        }
        else
        {
            schedule.MarkItemUndeliverable(itemId, request.ReasonNote ?? string.Empty);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return DeliveryScheduleResponse.From(schedule);
    }
}

internal sealed class UpdateDeliveryScheduleItemStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("delivery-schedules/{scheduleId:guid}/items/{itemId:guid}/status", HandleAsync)
            .WithName("UpdateDeliveryScheduleItemStatus")
            .WithTags(Tags.DeliverySchedules)
            .WithSummary("Update delivery schedule item status")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<UpdateDeliveryScheduleItemStatusRequest>();

    private static async Task<Results<Ok<DeliveryScheduleResponse>, ProblemHttpResult>> HandleAsync(
        Guid scheduleId,
        Guid itemId,
        UpdateDeliveryScheduleItemStatusRequest request,
        UpdateDeliveryScheduleItemStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(scheduleId, itemId, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
