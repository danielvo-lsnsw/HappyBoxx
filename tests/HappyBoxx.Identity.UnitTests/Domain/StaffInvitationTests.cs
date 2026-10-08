using HappyBoxx.Identity.Api.Domain;
using Shouldly;

namespace HappyBoxx.Identity.UnitTests.Domain;

public sealed class StaffInvitationTests
{
    [Fact]
    public void IsUsable_ReturnsFalseAfterInvitationExpires()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var invitation = CreateInvitation(createdAt, createdAt.AddDays(7));

        invitation.IsUsable(createdAt.AddDays(7)).ShouldBeFalse();
    }

    [Fact]
    public void Claim_MakesInvitationSingleUse()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = CreateInvitation(now, now.AddDays(7));

        invitation.Claim(now.AddMinutes(1));

        invitation.IsUsable(now.AddMinutes(2)).ShouldBeFalse();
        invitation.ClaimedAtUtc.ShouldBe(now.AddMinutes(1));
    }

    [Fact]
    public void Revoke_MakesInvitationUnusable()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = CreateInvitation(now, now.AddDays(7));

        invitation.Revoke(now.AddMinutes(1));

        invitation.IsUsable(now.AddMinutes(2)).ShouldBeFalse();
        invitation.RevokedAtUtc.ShouldBe(now.AddMinutes(1));
    }

    private static StaffInvitation CreateInvitation(DateTimeOffset createdAt, DateTimeOffset expiresAt) =>
        StaffInvitation.Create(
            "staff@example.test",
            AccountRoles.OrderCreator,
            "one-way-code-hash",
            "admin-subject",
            createdAt,
            expiresAt);
}