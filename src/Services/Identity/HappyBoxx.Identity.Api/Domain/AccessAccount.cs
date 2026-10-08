namespace HappyBoxx.Identity.Api.Domain;

public sealed class AccessAccount
{
    private AccessAccount() { }

    private AccessAccount(
        string externalSubject,
        string email,
        string displayName,
        AccountKind kind,
        string role,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        ExternalSubject = externalSubject;
        Email = email;
        NormalizedEmail = email.Trim().ToUpperInvariant();
        DisplayName = displayName;
        Kind = kind;
        Role = role;
        Status = AccountStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string ExternalSubject { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public AccountKind Kind { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public AccountStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? DisabledAtUtc { get; private set; }

    public static AccessAccount CreateCustomer(
        string externalSubject,
        string email,
        string displayName,
        DateTimeOffset createdAtUtc) =>
        new(externalSubject, email, displayName, AccountKind.Customer, AccountRoles.Customer, createdAtUtc);

    public static AccessAccount CreateStaff(
        string externalSubject,
        string email,
        string displayName,
        string role,
        DateTimeOffset createdAtUtc) =>
        new(externalSubject, email, displayName, AccountKind.Staff, role, createdAtUtc);

    public void ChangeRole(string role)
    {
        Role = role;
    }

    public void Disable(DateTimeOffset disabledAtUtc)
    {
        Status = AccountStatus.Disabled;
        DisabledAtUtc = disabledAtUtc;
    }
}