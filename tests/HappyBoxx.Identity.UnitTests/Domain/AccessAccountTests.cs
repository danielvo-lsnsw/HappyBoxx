using HappyBoxx.Identity.Api.Domain;
using Shouldly;

namespace HappyBoxx.Identity.UnitTests.Domain;

public sealed class AccessAccountTests
{
    [Fact]
    public void CreateCustomer_ActivatesOnlyTheIdentityPassedByVerifiedRegistration()
    {
        var account = AccessAccount.CreateCustomer(
            "verified-subject",
            "customer@example.test",
            "Test Customer",
            DateTimeOffset.UtcNow);

        account.Status.ShouldBe(AccountStatus.Active);
        account.Kind.ShouldBe(AccountKind.Customer);
        account.Role.ShouldBe(AccountRoles.Customer);
        account.ExternalSubject.ShouldBe("verified-subject");
    }

    [Fact]
    public void Disable_RemovesAccountFromTheActiveState()
    {
        var now = DateTimeOffset.UtcNow;
        var account = AccessAccount.CreateStaff(
            "staff-subject",
            "staff@example.test",
            "Test Staff",
            AccountRoles.OrderCreator,
            now);

        account.Disable(now.AddMinutes(1));

        account.Status.ShouldBe(AccountStatus.Disabled);
        account.DisabledAtUtc.ShouldBe(now.AddMinutes(1));
    }
}