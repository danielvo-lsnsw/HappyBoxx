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

public sealed record MarkAllDeliveryScheduleItemsDeliveredRequest(DateTimeOffset? DeliveredAtUtc);

internal sealed class MarkAllDeliveryScheduleItemsDeliveredValidator : AbstractValidator<MarkAllDeliveryScheduleItemsDeliveredRequest>
{
    public MarkAllDeliveryScheduleItemsDeliveredValidator()
    {
    }
}

internal sealed class MarkAllDeliveryScheduleItemsDeliveredHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<DeliveryScheduleResponse>> HandleAsync(
        Guid scheduleId,
        MarkAllDeliveryScheduleItemsDeliveredRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await dbContext.DeliverySchedules
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);
        if (schedule is null)
        {
            return DeliveryScheduleErrors.NotFound(scheduleId);
        }

        schedule.MarkAllDelivered(request.DeliveredAtUtc ?? DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DeliveryScheduleResponse.From(schedule);
    }
}

internal sealed class MarkAllDeliveryScheduleItemsDeliveredEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("delivery-schedules/{scheduleId:guid}/deliver-all", HandleAsync)
            .WithName("MarkAllDeliveryScheduleItemsDelivered")
            .WithTags(Tags.DeliverySchedules)
            .WithSummary("Mark all delivery schedule items as delivered")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<MarkAllDeliveryScheduleItemsDeliveredRequest>();

    private static async Task<Results<Ok<DeliveryScheduleResponse>, ProblemHttpResult>> HandleAsync(
        Guid scheduleId,
        MarkAllDeliveryScheduleItemsDeliveredRequest request,
        MarkAllDeliveryScheduleItemsDeliveredHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(scheduleId, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
