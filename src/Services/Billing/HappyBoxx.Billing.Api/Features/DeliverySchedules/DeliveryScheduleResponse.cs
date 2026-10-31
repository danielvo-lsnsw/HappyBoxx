using HappyBoxx.Billing.Api.Domain;

namespace HappyBoxx.Billing.Api.Features.DeliverySchedules;

public sealed record DeliveryScheduleSummaryResponse(
    Guid Id,
    string Name,
    DateOnly ScheduledDate,
    int TotalInvoices,
    int DeliveredCount,
    int UndeliverableCount,
    int PendingCount,
    DateTimeOffset CreatedAtUtc);

public sealed record DeliveryScheduleItemResponse(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string CustomerName,
    string CustomerAddress,
    string? CustomerPhone,
    decimal Total,
    DeliveryScheduleItemStatus Status,
    DateTimeOffset? DeliveredAtUtc,
    string? UndeliveredReason);

public sealed record DeliveryScheduleResponse(
    Guid Id,
    string Name,
    DateOnly ScheduledDate,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<DeliveryScheduleItemResponse> Items)
{
    internal static DeliveryScheduleResponse From(DeliverySchedule schedule) =>
        new(
            schedule.Id,
            schedule.Name,
            schedule.ScheduledDate,
            schedule.CreatedAtUtc,
            schedule.Items
                .OrderBy(item => item.InvoiceNumber)
                .Select(item => new DeliveryScheduleItemResponse(
                    item.Id,
                    item.InvoiceId,
                    item.InvoiceNumber,
                    item.InvoiceDate,
                    item.CustomerName,
                    item.CustomerAddress,
                    item.CustomerPhone,
                    item.Total,
                    item.Status,
                    item.DeliveredAtUtc,
                    item.UndeliveredReason))
                .ToArray());
}

internal static class Tags
{
    public const string DeliverySchedules = "DeliverySchedules";
}
