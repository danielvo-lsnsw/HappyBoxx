using HappyBoxx.BuildingBlocks.Domain;

namespace HappyBoxx.Billing.Api.Domain;

public sealed class DeliverySchedule : AuditableEntity
{
    public const int NameMaxLength = 120;
    public const int InvoiceNumberMaxLength = 32;
    public const int CustomerNameMaxLength = 150;
    public const int CustomerAddressMaxLength = 500;
    public const int CustomerPhoneMaxLength = 40;
    public const int UndeliveredReasonMaxLength = 500;

    private readonly List<DeliveryScheduleItem> _items = [];

    private DeliverySchedule()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public DateOnly ScheduledDate { get; private set; }

    public IReadOnlyCollection<DeliveryScheduleItem> Items => _items;

    public static DeliverySchedule Create(
        DateOnly scheduledDate,
        string? name,
        IEnumerable<DeliveryScheduleInvoiceInput> invoices)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        var items = invoices.ToArray();
        if (items.Length == 0)
        {
            throw new ArgumentException("At least one invoice is required.", nameof(invoices));
        }

        var scheduleName = string.IsNullOrWhiteSpace(name)
            ? $"Delivery {scheduledDate:yyyy-MM-dd}"
            : name.Trim();

        var schedule = new DeliverySchedule
        {
            Name = scheduleName,
            ScheduledDate = scheduledDate,
        };

        schedule._items.AddRange(items.Select(DeliveryScheduleItem.Create));
        return schedule;
    }

    public void MarkItemDelivered(Guid itemId, DateTimeOffset deliveredAtUtc)
    {
        var item = _items.FirstOrDefault(entry => entry.Id == itemId)
            ?? throw new InvalidOperationException($"Delivery schedule item '{itemId}' was not found.");
        item.MarkDelivered(deliveredAtUtc);
    }

    public void MarkItemUndeliverable(Guid itemId, string reason)
    {
        var item = _items.FirstOrDefault(entry => entry.Id == itemId)
            ?? throw new InvalidOperationException($"Delivery schedule item '{itemId}' was not found.");
        item.MarkUndeliverable(reason);
    }

    public void MarkAllDelivered(DateTimeOffset deliveredAtUtc)
    {
        foreach (var item in _items)
        {
            item.MarkDelivered(deliveredAtUtc);
        }
    }
}

public sealed class DeliveryScheduleItem : Entity
{
    private DeliveryScheduleItem()
    {
    }

    public Guid InvoiceId { get; private set; }

    public string InvoiceNumber { get; private set; } = string.Empty;

    public DateOnly InvoiceDate { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;

    public string CustomerAddress { get; private set; } = string.Empty;

    public string? CustomerPhone { get; private set; }

    public decimal Total { get; private set; }

    public DeliveryScheduleItemStatus Status { get; private set; } = DeliveryScheduleItemStatus.Pending;

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public string? UndeliveredReason { get; private set; }

    internal static DeliveryScheduleItem Create(DeliveryScheduleInvoiceInput invoice)
    {
        return new DeliveryScheduleItem
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber.Trim(),
            InvoiceDate = invoice.InvoiceDate,
            CustomerName = invoice.CustomerName.Trim(),
            CustomerAddress = invoice.CustomerAddress.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(invoice.CustomerPhone) ? null : invoice.CustomerPhone.Trim(),
            Total = invoice.Total,
            Status = DeliveryScheduleItemStatus.Pending,
        };
    }

    internal void MarkDelivered(DateTimeOffset deliveredAtUtc)
    {
        Status = DeliveryScheduleItemStatus.Delivered;
        DeliveredAtUtc = deliveredAtUtc;
        UndeliveredReason = null;
    }

    internal void MarkUndeliverable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        Status = DeliveryScheduleItemStatus.Undeliverable;
        UndeliveredReason = reason.Trim();
        DeliveredAtUtc = null;
    }
}

public enum DeliveryScheduleItemStatus
{
    Pending = 0,
    Delivered = 1,
    Undeliverable = 2,
}

public sealed record DeliveryScheduleInvoiceInput(
    Guid InvoiceId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string CustomerName,
    string CustomerAddress,
    string? CustomerPhone,
    decimal Total);
