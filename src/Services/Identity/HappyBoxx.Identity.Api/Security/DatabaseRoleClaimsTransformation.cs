using System.Security.Claims;
using HappyBoxx.Identity.Api.Domain;
using HappyBoxx.Identity.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Identity.Api.Security;

internal sealed class DatabaseRoleClaimsTransformation(IdentityDbContext dbContext) : IClaimsTransformation
{
    private const string RolesClaim = "roles";
    private const string ResolvedClaim = "happyboxx_role_resolved";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(ResolvedClaim, "true")
            || principal.Identities.Any(identity => identity.AuthenticationType == "HappyBoxx.Development"))
        {
            return principal;
        }

        var identity = principal.Identities.FirstOrDefault(candidate => candidate.IsAuthenticated);
        if (identity is null)
        {
            return principal;
        }

        foreach (var roleClaim in identity.FindAll(RolesClaim).ToArray())
        {
            identity.RemoveClaim(roleClaim);
        }

        var subject = principal.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var account = await dbContext.Accounts
                .AsNoTracking()
                .Where(candidate => candidate.ExternalSubject == subject && candidate.Status == AccountStatus.Active)
                .Select(candidate => new { candidate.Role })
                .FirstOrDefaultAsync();

            if (account is not null)
            {
                identity.AddClaim(new Claim(RolesClaim, account.Role));
            }
        }

        identity.AddClaim(new Claim(ResolvedClaim, "true"));
        return principal;
    }
}