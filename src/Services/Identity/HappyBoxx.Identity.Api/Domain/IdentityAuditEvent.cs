namespace HappyBoxx.Identity.Api.Domain;

public sealed class IdentityAuditEvent
{
    private IdentityAuditEvent() { }

    private IdentityAuditEvent(string actorSubject, string action, Guid? targetAccountId, Guid? targetInvitationId, DateTimeOffset createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        ActorSubject = actorSubject;
        Action = action;
        TargetAccountId = targetAccountId;
        TargetInvitationId = targetInvitationId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string ActorSubject { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public Guid? TargetAccountId { get; private set; }
    public Guid? TargetInvitationId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static IdentityAuditEvent Create(
        string actorSubject,
        string action,
        Guid? targetAccountId,
        Guid? targetInvitationId,
        DateTimeOffset createdAtUtc) =>
        new(actorSubject, action, targetAccountId, targetInvitationId, createdAtUtc);
}