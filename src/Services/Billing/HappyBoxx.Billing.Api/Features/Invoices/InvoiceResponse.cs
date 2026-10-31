using HappyBoxx.Billing.Api.Domain;

namespace HappyBoxx.Billing.Api.Features.Invoices;

public sealed record InvoiceLineResponse(
    Guid Id,
    Guid ProductId,
    InvoicePriceTier PriceTier,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPriceInclusive,
    decimal AmountInclusive);

public sealed record InvoiceSummaryResponse(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    Guid CustomerId,
    string CustomerName,
    decimal Total,
    DateOnly? PaidOn);

public sealed record InvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    Guid CustomerId,
    string CustomerName,
    string CustomerAddress,
    string? CustomerPhone,
    string? CustomerEmail,
    decimal Subtotal,
    decimal GstAmount,
    decimal Total,
    DateOnly? PaidOn,
    string? PaymentReference,
    string? PaymentNote,
    IReadOnlyList<InvoiceLineResponse> Lines)
{
    internal static InvoiceResponse From(Invoice invoice, string? customerEmail = null) =>
        new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.CustomerId,
            invoice.CustomerName,
            invoice.CustomerAddress,
            invoice.CustomerPhone,
            customerEmail,
            invoice.Subtotal,
            invoice.GstAmount,
            invoice.Total,
            invoice.PaidOn,
            invoice.PaymentReference,
            invoice.PaymentNote,
            invoice.Lines
                .OrderBy(line => line.LineNumber)
                .Select(line => new InvoiceLineResponse(
                    line.Id,
                    line.ProductId,
                    line.PriceTier,
                    line.Description,
                    line.Unit,
                    line.Quantity,
                    line.UnitPriceInclusive,
                    line.AmountInclusive))
                .ToArray());
}

internal static class Tags
{
    public const string Invoices = "Invoices";
    public const string Customers = "Customers";
    public const string DeliverySchedules = "DeliverySchedules";
}
