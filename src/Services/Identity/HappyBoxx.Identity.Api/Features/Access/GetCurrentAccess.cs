using System.Security.Claims;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.Identity.Api.Domain;
using HappyBoxx.Identity.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Identity.Api.Features.Access;

public sealed record CurrentAccessResponse(bool IsActive, string Role);

internal sealed class GetCurrentAccessEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("identity/access/current", HandleAsync)
            .WithName("GetCurrentAccess")
            .WithTags("Identity")
            .WithSummary("Get the authenticated user's current HappyBoxx account role");

    private static async Task<Results<Ok<CurrentAccessResponse>, NotFound>> HandleAsync(
        ClaimsPrincipal principal,
        IdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return TypedResults.NotFound();
        }

        var account = await dbContext.Accounts
            .AsNoTracking()
            .Where(candidate => candidate.ExternalSubject == subject)
            .Select(candidate => new { candidate.Status, candidate.Role })
            .FirstOrDefaultAsync(cancellationToken);

        return account is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new CurrentAccessResponse(account.Status == AccountStatus.Active, account.Role));
    }
}