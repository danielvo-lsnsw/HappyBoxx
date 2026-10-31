using HappyBoxx.BuildingBlocks.Results;

namespace HappyBoxx.Billing.Api.Domain;

public static class InvoiceErrors
{
    public static Error NumberAlreadyExists(string number) =>
        Error.Conflict("Invoice.NumberAlreadyExists", $"Invoice number '{number}' is already in use.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Invoice.NotFound", $"Invoice '{id}' was not found.");
}
