using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Validation;
using HappyBoxx.Identity.Api.Domain;
using HappyBoxx.Identity.Api.Infrastructure.Persistence;
using HappyBoxx.Identity.Api.Infrastructure.Email;
using HappyBoxx.ServiceDefaults.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyBoxx.Identity.Api.Features.Staff;

public sealed record CreateStaffInvitationRequest(string Email, string Role);

public sealed record StaffInvitationResponse(
    Guid Id,
    string Email,
    string Role,
    string? InvitationUrl,
    bool EmailSent,
    DateTimeOffset ExpiresAtUtc);

public sealed record ClaimStaffInvitationRequest(string InvitationCode, string DisplayName);

public sealed record StaffAccountResponse(Guid Id, string Email, string DisplayName, string Role, string Status);

public sealed record StaffInvitationListItem(
    Guid Id,
    string Email,
    string Role,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ClaimedAtUtc,
    DateTimeOffset? RevokedAtUtc);

public sealed record ChangeStaffRoleRequest(string Role);

internal sealed class CreateStaffInvitationValidator : AbstractValidator<CreateStaffInvitationRequest>
{
    public CreateStaffInvitationValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Role).Must(IsStaffRole).WithMessage("Role must be Admin or Order Creator.");
    }

    internal static bool IsStaffRole(string role) => role is AccountRoles.Admin or AccountRoles.OrderCreator;
}

internal sealed class ClaimStaffInvitationValidator : AbstractValidator<ClaimStaffInvitationRequest>
{
    public ClaimStaffInvitationValidator()
    {
        RuleFor(request => request.InvitationCode).NotEmpty().MaximumLength(128);
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);
    }
}

internal sealed class ChangeStaffRoleValidator : AbstractValidator<ChangeStaffRoleRequest>
{
    public ChangeStaffRoleValidator()
    {
        RuleFor(request => request.Role).Must(CreateStaffInvitationValidator.IsStaffRole)
            .WithMessage("Role must be Admin or Order Creator.");
    }
}

internal sealed class CreateStaffInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("identity/staff/invitations", HandleAsync)
            .WithName("CreateStaffInvitation")
            .WithTags("Staff")
            .WithSummary("Create a single-use invitation for a staff account")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<CreateStaffInvitationRequest>();

    private static async Task<Results<Created<StaffInvitationResponse>, ProblemHttpResult>> HandleAsync(
        CreateStaffInvitationRequest request,
        ClaimsPrincipal principal,
        IdentityDbContext dbContext,
        IStaffInvitationEmailSender emailSender,
        ILogger<CreateStaffInvitationEndpoint> logger,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var creatorSubject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(creatorSubject))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        var email = request.Email.Trim();
        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = timeProvider.GetUtcNow();
        var invitation = StaffInvitation.Create(
            email,
            request.Role,
            StaffInvitationCode.Hash(code),
            creatorSubject,
            now,
            now.AddDays(7));

        dbContext.StaffInvitations.Add(invitation);
        dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
            creatorSubject,
            "StaffInvitationCreated",
            null,
            invitation.Id,
            now));
        await dbContext.SaveChangesAsync(cancellationToken);

        var invitationUrl = emailSender.CreateInvitationUrl(code);
        if (emailSender.IsEnabled)
        {
            try
            {
                await emailSender.SendAsync(email, request.Role, invitationUrl, invitation.ExpiresAtUtc, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not deliver staff invitation {InvitationId}.", invitation.Id);
                var failedAt = timeProvider.GetUtcNow();
                invitation.Revoke(failedAt);
                dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
                    creatorSubject,
                    "StaffInvitationEmailFailed",
                    null,
                    invitation.Id,
                    failedAt));
                await dbContext.SaveChangesAsync(cancellationToken);
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Invitation email could not be sent; the invitation was revoked");
            }

            var sentAt = timeProvider.GetUtcNow();
            dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
                creatorSubject,
                "StaffInvitationEmailSent",
                null,
                invitation.Id,
                sentAt));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Created(
            $"/api/v1/identity/staff/invitations/{invitation.Id}",
            new StaffInvitationResponse(
                invitation.Id,
                email,
                request.Role,
                emailSender.IsEnabled ? null : invitationUrl,
                emailSender.IsEnabled,
                invitation.ExpiresAtUtc));
    }
}

internal sealed class ClaimStaffInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("identity/staff/invitations/claim", HandleAsync)
            .WithName("ClaimStaffInvitation")
            .WithTags("Staff")
            .WithSummary("Claim an invitation after completing Entra email verification")
            .RequireAuthorization(HappyBoxxPolicies.Authenticated)
            .WithRequestValidation<ClaimStaffInvitationRequest>();

    private static async Task<Results<Created<StaffAccountResponse>, ProblemHttpResult>> HandleAsync(
        ClaimStaffInvitationRequest request,
        ClaimsPrincipal principal,
        IdentityDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value ?? principal.FindFirst("emails")?.Value;
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Verified email identity is required");
        }

        var now = timeProvider.GetUtcNow();
        var codeHash = StaffInvitationCode.Hash(request.InvitationCode);
        var invitation = await dbContext.StaffInvitations
            .FirstOrDefaultAsync(candidate => candidate.CodeHash == codeHash, cancellationToken);
        if (invitation is null || !invitation.IsUsable(now))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Invitation is invalid, expired, revoked, or already used");
        }

        if (!string.Equals(invitation.NormalizedEmail, email.Trim().ToUpperInvariant(), StringComparison.Ordinal))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Invitation email does not match the verified account");
        }

        var accountExists = await dbContext.Accounts.AnyAsync(
            account => account.ExternalSubject == subject || account.NormalizedEmail == invitation.NormalizedEmail,
            cancellationToken);
        if (accountExists)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "An account already exists for this identity or email address");
        }

        var accountToCreate = AccessAccount.CreateStaff(
            subject,
            invitation.Email,
            request.DisplayName.Trim(),
            invitation.Role,
            now);
        invitation.Claim(now);
        dbContext.Accounts.Add(accountToCreate);
        dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
            subject,
            "StaffInvitationClaimed",
            accountToCreate.Id,
            invitation.Id,
            now));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (SqlServerErrorCodes.IsUniqueConstraintViolation(exception))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invitation or account has already been claimed");
        }

        return TypedResults.Created(
            $"/api/v1/identity/accounts/{accountToCreate.Id}",
            new StaffAccountResponse(
                accountToCreate.Id,
                accountToCreate.Email,
                accountToCreate.DisplayName,
                accountToCreate.Role,
                accountToCreate.Status.ToString()));
    }
}

internal sealed class RevokeStaffInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("identity/staff/invitations/{id:guid}/revoke", HandleAsync)
            .WithName("RevokeStaffInvitation")
            .WithTags("Staff")
            .WithSummary("Revoke an unclaimed staff invitation")
            .RequireAuthorization(HappyBoxxPolicies.Admin);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id,
        ClaimsPrincipal principal,
        IdentityDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var invitation = await dbContext.StaffInvitations.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);
        if (invitation is null)
        {
            return TypedResults.NotFound();
        }
        if (invitation.ClaimedAtUtc is not null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Invitation has already been claimed");
        }

        var actorSubject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(actorSubject))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        var now = timeProvider.GetUtcNow();
        invitation.Revoke(now);
        dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
            actorSubject,
            "StaffInvitationRevoked",
            null,
            invitation.Id,
            now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}

internal sealed class ChangeStaffRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("identity/staff/accounts/{id:guid}/role", HandleAsync)
            .WithName("ChangeStaffRole")
            .WithTags("Staff")
            .WithSummary("Change an active staff account's HappyBoxx role")
            .RequireAuthorization(HappyBoxxPolicies.Admin)
            .WithRequestValidation<ChangeStaffRoleRequest>();

    private static async Task<Results<Ok<StaffAccountResponse>, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id,
        ChangeStaffRoleRequest request,
        ClaimsPrincipal principal,
        IdentityDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (account is null)
        {
            return TypedResults.NotFound();
        }
        if (account.Kind != AccountKind.Staff || account.Status != AccountStatus.Active)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Only active staff accounts can change roles");
        }
        if (account.Role == AccountRoles.Admin
            && request.Role != AccountRoles.Admin
            && await StaffAccountRules.IsLastActiveAdminAsync(dbContext, account.Id, cancellationToken))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "The final active Admin cannot be demoted");
        }

        var actorSubject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(actorSubject))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        var now = timeProvider.GetUtcNow();
        account.ChangeRole(request.Role);
        dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
            actorSubject,
            "StaffRoleChanged",
            account.Id,
            null,
            now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new StaffAccountResponse(
            account.Id,
            account.Email,
            account.DisplayName,
            account.Role,
            account.Status.ToString()));
    }
}

internal sealed class DisableStaffAccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("identity/staff/accounts/{id:guid}/disable", HandleAsync)
            .WithName("DisableStaffAccount")
            .WithTags("Staff")
            .WithSummary("Disable an active staff account")
            .RequireAuthorization(HappyBoxxPolicies.Admin);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id,
        ClaimsPrincipal principal,
        IdentityDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (account is null)
        {
            return TypedResults.NotFound();
        }
        if (account.Kind != AccountKind.Staff || account.Status != AccountStatus.Active)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Only active staff accounts can be disabled");
        }
        var actorSubject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(actorSubject))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }
        if (account.ExternalSubject == actorSubject)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Administrators cannot disable their own account");
        }
        if (account.Role == AccountRoles.Admin
            && await StaffAccountRules.IsLastActiveAdminAsync(dbContext, account.Id, cancellationToken))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "The final active Admin cannot be disabled");
        }

        var now = timeProvider.GetUtcNow();
        account.Disable(now);
        dbContext.AuditEvents.Add(IdentityAuditEvent.Create(
            actorSubject,
            "StaffAccountDisabled",
            account.Id,
            null,
            now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}

internal static class StaffAccountRules
{
    public static async Task<bool> IsLastActiveAdminAsync(
        IdentityDbContext dbContext,
        Guid excludedAccountId,
        CancellationToken cancellationToken)
    {
        var otherActiveAdmins = await dbContext.Accounts.CountAsync(
            account => account.Id != excludedAccountId
                && account.Kind == AccountKind.Staff
                && account.Status == AccountStatus.Active
                && account.Role == AccountRoles.Admin,
            cancellationToken);
        return otherActiveAdmins == 0;
    }
}

internal static class StaffInvitationCode
{
    public static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));
}

internal sealed class ListStaffAccountsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("identity/staff/accounts", HandleAsync)
            .WithName("ListStaffAccounts")
            .WithTags("Staff")
            .WithSummary("List HappyBoxx staff accounts")
            .RequireAuthorization(HappyBoxxPolicies.Admin);

    private static async Task<IReadOnlyList<StaffAccountResponse>> HandleAsync(
        IdentityDbContext dbContext,
        CancellationToken cancellationToken) =>
        await dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.Kind == AccountKind.Staff)
            .OrderBy(account => account.Email)
            .Select(account => new StaffAccountResponse(
                account.Id,
                account.Email,
                account.DisplayName,
                account.Role,
                account.Status.ToString()))
            .ToListAsync(cancellationToken);
}

internal sealed class ListStaffInvitationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("identity/staff/invitations", HandleAsync)
            .WithName("ListStaffInvitations")
            .WithTags("Staff")
            .WithSummary("List staff invitations without returning their secret codes")
            .RequireAuthorization(HappyBoxxPolicies.Admin);

    private static async Task<IReadOnlyList<StaffInvitationListItem>> HandleAsync(
        IdentityDbContext dbContext,
        CancellationToken cancellationToken) =>
        await dbContext.StaffInvitations
            .AsNoTracking()
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .Take(100)
            .Select(invitation => new StaffInvitationListItem(
                invitation.Id,
                invitation.Email,
                invitation.Role,
                invitation.CreatedAtUtc,
                invitation.ExpiresAtUtc,
                invitation.ClaimedAtUtc,
                invitation.RevokedAtUtc))
            .ToListAsync(cancellationToken);
}