using HappyBoxx.Identity.Api.Domain;
using HappyBoxx.Identity.Api.Features.Staff;
using HappyBoxx.Identity.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace HappyBoxx.Identity.UnitTests.Features;

public sealed class StaffAccountRulesTests
{
    [Fact]
    public async Task IsLastActiveAdmin_ReturnsTrueForTheOnlyActiveAdmin()
    {
        await using var dbContext = CreateDbContext();
        var admin = CreateStaff("admin-one", AccountRoles.Admin);
        dbContext.Accounts.Add(admin);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var isLastAdmin = await StaffAccountRules.IsLastActiveAdminAsync(
            dbContext,
            admin.Id,
            TestContext.Current.CancellationToken);

        isLastAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLastActiveAdmin_ReturnsFalseWhenAnotherAdminIsActive()
    {
        await using var dbContext = CreateDbContext();
        var admin = CreateStaff("admin-one", AccountRoles.Admin);
        var anotherAdmin = CreateStaff("admin-two", AccountRoles.Admin);
        dbContext.Accounts.AddRange(admin, anotherAdmin);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var isLastAdmin = await StaffAccountRules.IsLastActiveAdminAsync(
            dbContext,
            admin.Id,
            TestContext.Current.CancellationToken);

        isLastAdmin.ShouldBeFalse();
    }

    private static IdentityDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AccessAccount CreateStaff(string subject, string role) =>
        AccessAccount.CreateStaff(
            subject,
            $"{subject}@example.test",
            subject,
            role,
            DateTimeOffset.UtcNow);
}