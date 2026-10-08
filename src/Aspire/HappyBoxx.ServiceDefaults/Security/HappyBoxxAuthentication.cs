using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HappyBoxx.ServiceDefaults.Security;

public static class HappyBoxxPolicies
{
    public const string Admin = "HappyBoxx.Admin";
    public const string Staff = "HappyBoxx.Staff";
    public const string Authenticated = "HappyBoxx.Authenticated";
}

public static class HappyBoxxAuthentication
{
    private const string DevelopmentScheme = "HappyBoxx.Development";

    public static IServiceCollection AddHappyBoxxAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var developmentMode = environment.IsDevelopment()
            && string.Equals(configuration["Authentication:Mode"], "Development", StringComparison.Ordinal);
        var roleSource = configuration["Authentication:RoleSource"];
        if (roleSource is not ("IdentityService" or "Database"))
        {
            throw new InvalidOperationException(
                "Authentication:RoleSource must be configured as IdentityService or Database.");
        }

        var authentication = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = developmentMode ? DevelopmentScheme : JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = developmentMode ? DevelopmentScheme : JwtBearerDefaults.AuthenticationScheme;
        });

        if (developmentMode)
        {
            authentication.AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                DevelopmentScheme,
                _ => { });
        }
        else
        {
            var authority = configuration["Authentication:Authority"];
            var audience = configuration["Authentication:Audience"];
            if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "Authentication:Authority and Authentication:Audience must be configured outside Development mode.");
            }

            authentication.AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "roles",
                };
            });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(HappyBoxxPolicies.Authenticated, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(HappyBoxxPolicies.Staff, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User.IsInRole("Admin") || context.User.IsInRole("Order Creator")));
            options.AddPolicy(HappyBoxxPolicies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole("Admin"));
        });

            if (roleSource == "IdentityService")
            {
                services.AddHttpContextAccessor();
                services.AddHttpClient("identity-access").AddServiceDiscovery();
                services.AddTransient<IClaimsTransformation, IdentityServiceRoleClaimsTransformation>();
            }

        return services;
    }
}

internal sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers["X-HappyBoxx-Dev-Subject"].ToString();
        var email = Request.Headers["X-HappyBoxx-Dev-Email"].ToString();
        var role = Request.Headers["X-HappyBoxx-Dev-Role"].ToString();

        if (string.IsNullOrWhiteSpace(subject)
            || string.IsNullOrWhiteSpace(email)
            || role is not ("Admin" or "Order Creator" or "Customer"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim("sub", subject),
            new Claim("email", email),
            new Claim("roles", role),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name, "name", "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class IdentityServiceRoleClaimsTransformation(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<IdentityServiceRoleClaimsTransformation> logger)
    : IClaimsTransformation
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

        var resolvedPrincipal = CloneWithoutRoleClaims(principal);
        var httpContext = httpContextAccessor.HttpContext;
        var authorization = httpContext?.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorization, out var authorizationHeader)
            || !string.Equals(authorizationHeader.Scheme, "Bearer", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(authorizationHeader.Parameter))
        {
            MarkResolved(resolvedPrincipal);
            return resolvedPrincipal;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https+http://identity-api/api/v1/identity/access/current");
            request.Headers.Authorization = authorizationHeader;

            using var response = await httpClientFactory.CreateClient("identity-access").SendAsync(
                request,
                httpContext?.RequestAborted ?? CancellationToken.None);

            if (response.IsSuccessStatusCode)
            {
                var role = await response.Content.ReadFromJsonAsync<IdentityRoleResponse>(
                    cancellationToken: httpContext?.RequestAborted ?? CancellationToken.None);
                if (role is { IsActive: true } && !string.IsNullOrWhiteSpace(role.Role))
                {
                    resolvedPrincipal.Identities.FirstOrDefault(identity => identity.IsAuthenticated)
                        ?.AddClaim(new Claim(RolesClaim, role.Role));
                }
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Could not resolve HappyBoxx application role for the authenticated identity.");
        }

        MarkResolved(resolvedPrincipal);
        return resolvedPrincipal;
    }

    private static ClaimsPrincipal CloneWithoutRoleClaims(ClaimsPrincipal principal)
    {
        var identities = principal.Identities.Select(source =>
        {
            var identity = new ClaimsIdentity(source);
            foreach (var roleClaim in identity.FindAll(RolesClaim).ToArray())
            {
                identity.RemoveClaim(roleClaim);
            }
            return identity;
        });
        return new ClaimsPrincipal(identities);
    }

    private static void MarkResolved(ClaimsPrincipal principal) =>
        principal.Identities.FirstOrDefault(identity => identity.IsAuthenticated)
            ?.AddClaim(new Claim(ResolvedClaim, "true"));

    private sealed record IdentityRoleResponse(string Role, bool IsActive);
}