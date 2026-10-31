using HappyBoxx.Billing.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyBoxx.Billing.Api.Infrastructure.Persistence;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public const string Schema = "billing";

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DeliverySchedule> DeliverySchedules => Set<DeliverySchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).ValueGeneratedNever();
        builder.Property(invoice => invoice.InvoiceNumber)
            .HasMaxLength(Invoice.InvoiceNumberMaxLength)
            .IsRequired();
        builder.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
        builder.Property(invoice => invoice.CustomerName)
            .HasMaxLength(Invoice.CustomerNameMaxLength)
            .IsRequired();
        builder.Property(invoice => invoice.CustomerAddress)
            .HasMaxLength(Invoice.CustomerAddressMaxLength)
            .IsRequired();
        builder.Property(invoice => invoice.CustomerPhone)
            .HasMaxLength(Customer.PhoneMaxLength);
        builder.Property(invoice => invoice.Subtotal).HasPrecision(18, 2);
        builder.Property(invoice => invoice.GstAmount).HasPrecision(18, 2);
        builder.Property(invoice => invoice.Total).HasPrecision(18, 2);
        builder.Property(invoice => invoice.PaidOn);
        builder.Property(invoice => invoice.PaymentReference)
            .HasMaxLength(Invoice.PaymentReferenceMaxLength);
        builder.Property(invoice => invoice.PaymentNote)
            .HasMaxLength(Invoice.PaymentNoteMaxLength);
        builder.HasIndex(invoice => invoice.InvoiceDate);
        builder.HasIndex(invoice => invoice.CustomerId);
        builder.HasIndex(invoice => invoice.PaidOn);
        builder.HasOne(invoice => invoice.Customer)
            .WithMany()
            .HasForeignKey(invoice => invoice.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey("InvoiceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(invoice => invoice.Lines)
            .HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property(line => line.Description)
            .HasMaxLength(InvoiceLine.DescriptionMaxLength)
            .IsRequired();
        builder.Property(line => line.PriceTier)
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.Property(line => line.Unit)
            .HasMaxLength(InvoiceLine.UnitMaxLength)
            .IsRequired();
        builder.Property(line => line.Quantity).HasPrecision(18, 3);
        builder.Property(line => line.UnitPriceInclusive).HasPrecision(18, 2);
        builder.Property(line => line.AmountInclusive).HasPrecision(18, 2);
        builder.HasIndex("InvoiceId", nameof(InvoiceLine.LineNumber)).IsUnique();
        builder.HasIndex(line => line.ProductId);
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();
        builder.Property(customer => customer.Name)
            .HasMaxLength(Customer.NameMaxLength)
            .IsRequired();
        builder.Property(customer => customer.Address)
            .HasMaxLength(Customer.AddressMaxLength)
            .IsRequired();
        builder.Property(customer => customer.Phone)
            .HasMaxLength(Customer.PhoneMaxLength);
        builder.Property(customer => customer.Email)
            .HasMaxLength(Customer.EmailMaxLength);
        builder.Property(customer => customer.Website)
            .HasMaxLength(Customer.WebsiteMaxLength);
        builder.Property(customer => customer.Abn)
            .HasMaxLength(Customer.AbnMaxLength);
        builder.Property(customer => customer.CustomerType)
            .HasMaxLength(Customer.CustomerTypeMaxLength);
        builder.HasIndex(customer => customer.Name).IsUnique();
        builder.HasIndex(customer => customer.IsActive);
    }

    internal sealed class DeliveryScheduleConfiguration : IEntityTypeConfiguration<DeliverySchedule>
    {
        public void Configure(EntityTypeBuilder<DeliverySchedule> builder)
        {
            builder.ToTable("DeliverySchedules");
            builder.HasKey(schedule => schedule.Id);
            builder.Property(schedule => schedule.Id).ValueGeneratedNever();
            builder.Property(schedule => schedule.Name)
                .HasMaxLength(DeliverySchedule.NameMaxLength)
                .IsRequired();
            builder.Property(schedule => schedule.ScheduledDate).IsRequired();
            builder.HasIndex(schedule => schedule.ScheduledDate);
            builder.HasIndex(schedule => schedule.CreatedAtUtc);

            builder.HasMany(schedule => schedule.Items)
                .WithOne()
                .HasForeignKey("DeliveryScheduleId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(schedule => schedule.Items)
                .HasField("_items")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }

    internal sealed class DeliveryScheduleItemConfiguration : IEntityTypeConfiguration<DeliveryScheduleItem>
    {
        public void Configure(EntityTypeBuilder<DeliveryScheduleItem> builder)
        {
            builder.ToTable("DeliveryScheduleItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Id).ValueGeneratedNever();
            builder.Property(item => item.InvoiceNumber)
                .HasMaxLength(DeliverySchedule.InvoiceNumberMaxLength)
                .IsRequired();
            builder.Property(item => item.CustomerName)
                .HasMaxLength(DeliverySchedule.CustomerNameMaxLength)
                .IsRequired();
            builder.Property(item => item.CustomerAddress)
                .HasMaxLength(DeliverySchedule.CustomerAddressMaxLength)
                .IsRequired();
            builder.Property(item => item.CustomerPhone)
                .HasMaxLength(DeliverySchedule.CustomerPhoneMaxLength);
            builder.Property(item => item.Total).HasPrecision(18, 2);
            builder.Property(item => item.Status)
                .HasConversion<string>()
                .HasMaxLength(24)
                .IsRequired();
            builder.Property(item => item.DeliveredAtUtc);
            builder.Property(item => item.UndeliveredReason)
                .HasMaxLength(DeliverySchedule.UndeliveredReasonMaxLength);
            builder.HasIndex("DeliveryScheduleId", nameof(DeliveryScheduleItem.InvoiceId)).IsUnique();
            builder.HasIndex(item => item.Status);
        }
    }
}
