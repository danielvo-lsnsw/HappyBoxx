using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Results;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.DeliverySchedules;

internal sealed class GetDeliveryScheduleByIdHandler(BillingDbContext dbContext) : IHandler
{
    public async Task<Result<DeliveryScheduleResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await dbContext.DeliverySchedules
            .AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return schedule is null
            ? DeliveryScheduleErrors.NotFound(id)
            : DeliveryScheduleResponse.From(schedule);
    }
}

internal sealed class GetDeliveryScheduleByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("delivery-schedules/{id:guid}", HandleAsync)
            .WithName("GetDeliveryScheduleById")
            .WithTags(Tags.DeliverySchedules)
            .WithSummary("Get a delivery schedule")
            .RequireAuthorization(HappyBoxxPolicies.Staff);

    private static async Task<Results<Ok<DeliveryScheduleResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        GetDeliveryScheduleByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
