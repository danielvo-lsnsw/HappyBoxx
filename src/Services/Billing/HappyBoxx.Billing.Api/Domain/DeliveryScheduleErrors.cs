using HappyBoxx.BuildingBlocks.Results;

namespace HappyBoxx.Billing.Api.Domain;

public static class DeliveryScheduleErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("DeliverySchedule.NotFound", $"Delivery schedule '{id}' was not found.");

    public static Error ItemNotFound(Guid scheduleId, Guid itemId) =>
        Error.NotFound(
            "DeliverySchedule.ItemNotFound",
            $"Delivery schedule item '{itemId}' was not found in schedule '{scheduleId}'.");
}
