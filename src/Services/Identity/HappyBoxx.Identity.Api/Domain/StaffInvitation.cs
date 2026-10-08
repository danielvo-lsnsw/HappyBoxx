namespace HappyBoxx.Identity.Api.Domain;

public sealed class StaffInvitation
{
    private StaffInvitation() { }

    private StaffInvitation(
        string email,
        string role,
        string codeHash,
        string createdBySubject,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = Guid.CreateVersion7();
        Email = email;
        NormalizedEmail = email.Trim().ToUpperInvariant();
        Role = role;
        CodeHash = codeHash;
        CreatedBySubject = createdBySubject;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public string CreatedBySubject { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ClaimedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool IsUsable(DateTimeOffset nowUtc) =>
        ClaimedAtUtc is null && RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public void Claim(DateTimeOffset claimedAtUtc)
    {
        ClaimedAtUtc = claimedAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        RevokedAtUtc = revokedAtUtc;
    }

    public static StaffInvitation Create(
        string email,
        string role,
        string codeHash,
        string createdBySubject,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc) =>
        new(email, role, codeHash, createdBySubject, createdAtUtc, expiresAtUtc);
}