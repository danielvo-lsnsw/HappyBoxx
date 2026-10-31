using HappyBoxx.Billing.Api.Domain;

namespace HappyBoxx.Billing.Api.Features.Customers;

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Address,
    string? Phone,
    string? Email,
    string? Website,
    string? Abn,
    string? CustomerType,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc)
{
    internal static CustomerResponse From(Customer customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.Address,
            customer.Phone,
            customer.Email,
            customer.Website,
            customer.Abn,
            customer.CustomerType,
            customer.IsActive,
            customer.UpdatedAtUtc);
}
