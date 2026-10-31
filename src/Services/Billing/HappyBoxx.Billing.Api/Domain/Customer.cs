using HappyBoxx.BuildingBlocks.Domain;

namespace HappyBoxx.Billing.Api.Domain;

public sealed class Customer : AuditableEntity
{
    public const int NameMaxLength = 150;
    public const int AddressMaxLength = 500;
    public const int PhoneMaxLength = 40;
    public const int EmailMaxLength = 254;
    public const int WebsiteMaxLength = 200;
    public const int AbnMaxLength = 11;
    public const int CustomerTypeMaxLength = 60;

    private Customer()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Website { get; private set; }

    public string? Abn { get; private set; }

    public string? CustomerType { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Customer Create(
        string name,
        string address,
        string phone,
        string? email,
        string? website,
        string? abn,
        string? customerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        var customer = new Customer
        {
            IsActive = true,
        };
        customer.Apply(name, address, phone, email, website, abn, customerType, isActive: true);

        return customer;
    }

    public void Update(
        string name,
        string address,
        string phone,
        string? email,
        string? website,
        string? abn,
        string? customerType,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        Apply(name, address, phone, email, website, abn, customerType, isActive);
    }

    public void Deactivate() => IsActive = false;

    private void Apply(
        string name,
        string address,
        string phone,
        string? email,
        string? website,
        string? abn,
        string? customerType,
        bool isActive)
    {
        Name = name.Trim();
        Address = address.Trim();
        Phone = phone.Trim();
        Email = TrimOrNull(email);
        Website = TrimOrNull(website);
        Abn = TrimOrNull(abn);
        CustomerType = TrimOrNull(customerType);
        IsActive = isActive;
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
