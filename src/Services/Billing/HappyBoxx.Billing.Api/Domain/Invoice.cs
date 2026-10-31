using HappyBoxx.BuildingBlocks.Domain;

namespace HappyBoxx.Billing.Api.Domain;

public sealed class Invoice : AuditableEntity
{
    public const int InvoiceNumberMaxLength = 32;
    public const int CustomerNameMaxLength = 150;
    public const int CustomerAddressMaxLength = 500;
    public const int PaymentReferenceMaxLength = 120;
    public const int PaymentNoteMaxLength = 500;

    private readonly List<InvoiceLine> _lines = [];

    private Invoice()
    {
    }

    public Guid CustomerId { get; private set; }

    public string InvoiceNumber { get; private set; } = string.Empty;

    public DateOnly InvoiceDate { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;

    public string CustomerAddress { get; private set; } = string.Empty;

    public string? CustomerPhone { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal GstAmount { get; private set; }

    public decimal Total { get; private set; }

    public DateOnly? PaidOn { get; private set; }

    public string? PaymentReference { get; private set; }

    public string? PaymentNote { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public IReadOnlyCollection<InvoiceLine> Lines => _lines;

    public static Invoice Create(
        string invoiceNumber,
        DateOnly invoiceDate,
        Guid customerId,
        string customerName,
        string customerAddress,
        string? customerPhone,
        IEnumerable<InvoiceLineInput> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        ArgumentOutOfRangeException.ThrowIfEqual(customerId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerAddress);
        ArgumentNullException.ThrowIfNull(lines);

        var invoice = new Invoice
        {
            CustomerId = customerId,
            InvoiceNumber = invoiceNumber.Trim(),
            InvoiceDate = invoiceDate,
            CustomerName = customerName.Trim(),
            CustomerAddress = customerAddress.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim(),
        };

        invoice._lines.AddRange(lines.Select((line, index) => InvoiceLine.Create(
            index + 1,
            line.ProductId,
            line.PriceTier,
            line.Description,
            line.Unit,
            line.Quantity,
            line.UnitPriceInclusive)));

        if (invoice._lines.Count == 0)
        {
            throw new ArgumentException("An invoice must contain at least one line.", nameof(lines));
        }

        invoice.Total = decimal.Round(invoice._lines.Sum(line => line.AmountInclusive), 2, MidpointRounding.AwayFromZero);
        invoice.GstAmount = decimal.Round(invoice.Total / 11m, 2, MidpointRounding.AwayFromZero);
        invoice.Subtotal = invoice.Total - invoice.GstAmount;

        return invoice;
    }

    public void RecordPayment(DateOnly paidOn, string? paymentReference, string? paymentNote)
    {
        if (paidOn == default)
        {
            throw new ArgumentException("Paid date is required.", nameof(paidOn));
        }

        PaidOn = paidOn;
        PaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? null : paymentReference.Trim();
        PaymentNote = string.IsNullOrWhiteSpace(paymentNote) ? null : paymentNote.Trim();
    }
}

public sealed record InvoiceLineInput(
    Guid ProductId,
    InvoicePriceTier PriceTier,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPriceInclusive);
