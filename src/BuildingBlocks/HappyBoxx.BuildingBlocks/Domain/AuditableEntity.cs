namespace HappyBoxx.BuildingBlocks.Domain;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAtUtc { get; internal set; }

    public DateTimeOffset UpdatedAtUtc { get; internal set; }
}
