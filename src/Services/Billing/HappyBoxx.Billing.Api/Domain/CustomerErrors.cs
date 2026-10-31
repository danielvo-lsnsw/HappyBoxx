using HappyBoxx.BuildingBlocks.Results;

namespace HappyBoxx.Billing.Api.Domain;

public static class CustomerErrors
{
    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("Customer.NameAlreadyExists", $"Customer '{name}' already exists.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Customer.NotFound", $"Customer '{id}' was not found.");

    public static Error EmailMissing(Guid id) =>
        Error.Validation("Customer.EmailMissing", $"Customer '{id}' does not have an email address.");
}
