namespace HappyBoxx.Identity.Api.Domain;

public sealed class CustomerProfile
{
    private CustomerProfile() { }

    private CustomerProfile(
        Guid accountId,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string city,
        string? region,
        string postalCode,
        string country,
        DateTimeOffset createdAtUtc)
    {
        AccountId = accountId;
        PhoneNumber = phoneNumber;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid AccountId { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CustomerProfile Create(
        Guid accountId,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string city,
        string? region,
        string postalCode,
        string country,
        DateTimeOffset createdAtUtc) =>
        new(accountId, phoneNumber, addressLine1, addressLine2, city, region, postalCode, country, createdAtUtc);
}