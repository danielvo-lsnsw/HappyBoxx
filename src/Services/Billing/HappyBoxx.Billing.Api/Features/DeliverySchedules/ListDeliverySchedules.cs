using FluentValidation;
using HappyBoxx.Billing.Api.Domain;
using HappyBoxx.Billing.Api.Infrastructure.Persistence;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Pagination;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Features.DeliverySchedules;

public sealed record ListDeliverySchedulesQuery(int Page = 1, int PageSize = 20, string? Search = null) : IPagedQuery;

internal sealed class ListDeliverySchedulesValidator : AbstractValidator<ListDeliverySchedulesQuery>
{
    public ListDeliverySchedulesValidator()
    {
        this.AddPagingRules();
        RuleFor(query => query.Search).MaximumLength(150);
    }
}

internal sealed class ListDeliverySchedulesHandler(BillingDbContext dbContext) : IHandler
{
    public Task<PagedResult<DeliveryScheduleSummaryResponse>> HandleAsync(
        ListDeliverySchedulesQuery query,
        CancellationToken cancellationToken)
    {
        var schedules = dbContext.DeliverySchedules
            .AsNoTracking()
            .Include(schedule => schedule.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            schedules = schedules.Where(schedule => schedule.Name.Contains(search));
        }

        return schedules
            .OrderByDescending(schedule => schedule.ScheduledDate)
            .ThenByDescending(schedule => schedule.CreatedAtUtc)
            .Select(schedule => new DeliveryScheduleSummaryResponse(
                schedule.Id,
                schedule.Name,
                schedule.ScheduledDate,
                schedule.Items.Count,
                schedule.Items.Count(item => item.Status == DeliveryScheduleItemStatus.Delivered),
                schedule.Items.Count(item => item.Status == DeliveryScheduleItemStatus.Undeliverable),
                schedule.Items.Count(item => item.Status == DeliveryScheduleItemStatus.Pending),
                schedule.CreatedAtUtc))
            .ToPagedResultAsync(query, cancellationToken);
    }
}

internal sealed class ListDeliverySchedulesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("delivery-schedules", HandleAsync)
            .WithName("ListDeliverySchedules")
            .WithTags(Tags.DeliverySchedules)
            .WithSummary("List delivery schedules")
            .RequireAuthorization(HappyBoxxPolicies.Staff)
            .WithRequestValidation<ListDeliverySchedulesQuery>();

    private static async Task<Ok<PagedResult<DeliveryScheduleSummaryResponse>>> HandleAsync(
        [AsParameters] ListDeliverySchedulesQuery query,
        ListDeliverySchedulesHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));
}
