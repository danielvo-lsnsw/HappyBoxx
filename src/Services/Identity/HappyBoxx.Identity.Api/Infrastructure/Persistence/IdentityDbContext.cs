using HappyBoxx.Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Identity.Api.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    public DbSet<AccessAccount> Accounts => Set<AccessAccount>();

    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    public DbSet<StaffInvitation> StaffInvitations => Set<StaffInvitation>();

    public DbSet<IdentityAuditEvent> AuditEvents => Set<IdentityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}

internal sealed class AccessAccountConfiguration : IEntityTypeConfiguration<AccessAccount>
{
    public void Configure(EntityTypeBuilder<AccessAccount> builder)
    {
        builder.ToTable("AccessAccounts");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.ExternalSubject).HasMaxLength(200).IsRequired();
        builder.Property(account => account.Email).HasMaxLength(320).IsRequired();
        builder.Property(account => account.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(account => account.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(account => account.Role).HasMaxLength(40).IsRequired();
        builder.HasIndex(account => account.ExternalSubject).IsUnique();
        builder.HasIndex(account => account.NormalizedEmail).IsUnique();
    }
}

internal sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("CustomerProfiles");
        builder.HasKey(profile => profile.AccountId);
        builder.Property(profile => profile.PhoneNumber).HasMaxLength(40).IsRequired();
        builder.Property(profile => profile.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.AddressLine2).HasMaxLength(200);
        builder.Property(profile => profile.City).HasMaxLength(120).IsRequired();
        builder.Property(profile => profile.Region).HasMaxLength(120);
        builder.Property(profile => profile.PostalCode).HasMaxLength(32).IsRequired();
        builder.Property(profile => profile.Country).HasMaxLength(2).IsRequired();
        builder.HasOne<AccessAccount>()
            .WithOne()
            .HasForeignKey<CustomerProfile>(profile => profile.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StaffInvitationConfiguration : IEntityTypeConfiguration<StaffInvitation>
{
    public void Configure(EntityTypeBuilder<StaffInvitation> builder)
    {
        builder.ToTable("StaffInvitations");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.Id).ValueGeneratedNever();
        builder.Property(invitation => invitation.Email).HasMaxLength(320).IsRequired();
        builder.Property(invitation => invitation.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(invitation => invitation.Role).HasMaxLength(40).IsRequired();
        builder.Property(invitation => invitation.CodeHash).HasMaxLength(64).IsRequired();
        builder.Property(invitation => invitation.CreatedBySubject).HasMaxLength(200).IsRequired();
        builder.HasIndex(invitation => invitation.CodeHash).IsUnique();
        builder.HasIndex(invitation => new { invitation.NormalizedEmail, invitation.ExpiresAtUtc });
    }
}

internal sealed class IdentityAuditEventConfiguration : IEntityTypeConfiguration<IdentityAuditEvent>
{
    public void Configure(EntityTypeBuilder<IdentityAuditEvent> builder)
    {
        builder.ToTable("IdentityAuditEvents");
        builder.HasKey(auditEvent => auditEvent.Id);
        builder.Property(auditEvent => auditEvent.Id).ValueGeneratedNever();
        builder.Property(auditEvent => auditEvent.ActorSubject).HasMaxLength(200).IsRequired();
        builder.Property(auditEvent => auditEvent.Action).HasMaxLength(80).IsRequired();
        builder.HasIndex(auditEvent => auditEvent.CreatedAtUtc);
        builder.HasIndex(auditEvent => auditEvent.TargetAccountId);
        builder.HasIndex(auditEvent => auditEvent.TargetInvitationId);
    }
}