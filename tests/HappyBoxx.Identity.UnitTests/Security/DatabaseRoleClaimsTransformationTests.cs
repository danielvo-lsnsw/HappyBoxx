using System.Security.Claims;
using HappyBoxx.Identity.Api.Domain;
using HappyBoxx.Identity.Api.Infrastructure.Persistence;
using HappyBoxx.Identity.Api.Security;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace HappyBoxx.Identity.UnitTests.Security;

public sealed class DatabaseRoleClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_ReplacesTokenRolesWithTheActiveDatabaseRole()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Accounts.Add(AccessAccount.CreateStaff(
            "entra-subject",
            "staff@example.test",
            "Test Staff",
            AccountRoles.OrderCreator,
            DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var principal = CreatePrincipal("entra-subject", "Admin");
        var transformer = new DatabaseRoleClaimsTransformation(dbContext);

        var transformed = await transformer.TransformAsync(principal);

        transformed.IsInRole(AccountRoles.OrderCreator).ShouldBeTrue();
        transformed.IsInRole(AccountRoles.Admin).ShouldBeFalse();
    }

    [Fact]
    public async Task TransformAsync_DoesNotGrantRolesToDisabledAccounts()
    {
        await using var dbContext = CreateDbContext();
        var account = AccessAccount.CreateStaff(
            "disabled-subject",
            "disabled@example.test",
            "Disabled Staff",
            AccountRoles.Admin,
            DateTimeOffset.UtcNow);
        account.Disable(DateTimeOffset.UtcNow.AddMinutes(1));
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transformed = await new DatabaseRoleClaimsTransformation(dbContext)
            .TransformAsync(CreatePrincipal("disabled-subject", AccountRoles.Admin));

        transformed.IsInRole(AccountRoles.Admin).ShouldBeFalse();
    }

    private static IdentityDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ClaimsPrincipal CreatePrincipal(string subject, string role) =>
        new(new ClaimsIdentity(
        [
            new Claim("sub", subject),
            new Claim("roles", role),
        ],
        "Bearer",
        "name",
        "roles"));
}