using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace HappyBoxx.Billing.Api.Infrastructure.Email;

public sealed class InvoiceEmailOptions
{
    public string Mode { get; init; } = "Disabled";
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string UserName { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "HappyBoxx";
    public string Password { get; init; } = string.Empty;
}

public sealed record InvoiceEmailLine(
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPriceInclusive,
    decimal AmountInclusive);

public sealed record InvoiceEmailContent(
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string CustomerName,
    string CustomerAddress,
    decimal Subtotal,
    decimal GstAmount,
    decimal Total,
    IReadOnlyList<InvoiceEmailLine> Lines);

public sealed record InvoicePaymentReceiptDetails(
    DateOnly PaidOn,
    string? PaymentReference,
    string? PaymentNote);

public interface IInvoiceEmailSender
{
    bool IsEnabled { get; }
    Task SendInvoiceAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        string? subjectOverride,
        string? messageOverride,
        CancellationToken cancellationToken);
    Task SendReceiptAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails paymentDetails,
        bool includeInvoice,
        CancellationToken cancellationToken);
    Task SendPaidInvoicePackageAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails paymentDetails,
        bool includeInvoice,
        string? subjectOverride,
        string? messageOverride,
        CancellationToken cancellationToken);
}

public sealed class InvoiceEmailSender(IOptions<InvoiceEmailOptions> options) : IInvoiceEmailSender
{
    private readonly InvoiceEmailOptions _options = options.Value;

    public bool IsEnabled => string.Equals(_options.Mode, "GmailSmtp", StringComparison.OrdinalIgnoreCase);

    public async Task SendInvoiceAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        string? subjectOverride,
        string? messageOverride,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Invoice email delivery is disabled.");
        }

        var subject = string.IsNullOrWhiteSpace(subjectOverride)
            ? $"Tax Invoice {invoice.InvoiceNumber} - HappyBoxx"
            : subjectOverride.Trim();
        var introText = string.IsNullOrWhiteSpace(messageOverride)
            ? "Please find your tax invoice attached as PDF."
            : messageOverride.Trim();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        foreach (var toEmail in toEmails)
        {
            message.To.Add(MailboxAddress.Parse(toEmail));
        }
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = BuildInvoiceTextBody(invoice, introText),
            HtmlBody = BuildInvoiceHtmlBody(invoice, introText)
        };
        bodyBuilder.Attachments.Add(GetPdfFileName(invoice.InvoiceNumber), BuildInvoicePdf(invoice), ContentType.Parse("application/pdf"));
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendReceiptAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails paymentDetails,
        bool includeInvoice,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Invoice email delivery is disabled.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        foreach (var toEmail in toEmails)
        {
            message.To.Add(MailboxAddress.Parse(toEmail));
        }

        message.Subject = includeInvoice
            ? $"Payment receipt & tax invoice {invoice.InvoiceNumber} - HappyBoxx"
            : $"Payment receipt {invoice.InvoiceNumber} - HappyBoxx";

        var bodyBuilder = new BodyBuilder
        {
            TextBody = BuildReceiptTextBody(invoice, paymentDetails, includeInvoice),
            HtmlBody = BuildReceiptHtmlBody(invoice, paymentDetails, includeInvoice)
        };
        if (includeInvoice)
        {
            bodyBuilder.Attachments.Add(GetPdfFileName(invoice.InvoiceNumber), BuildInvoicePdf(invoice), ContentType.Parse("application/pdf"));
        }
        bodyBuilder.Attachments.Add(GetReceiptPdfFileName(invoice.InvoiceNumber), BuildReceiptPdf(invoice, paymentDetails), ContentType.Parse("application/pdf"));
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendPaidInvoicePackageAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails paymentDetails,
        bool includeInvoice,
        string? subjectOverride,
        string? messageOverride,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Invoice email delivery is disabled.");
        }

        var subject = string.IsNullOrWhiteSpace(subjectOverride)
            ? (includeInvoice
                ? $"Tax Invoice & Payment Receipt {invoice.InvoiceNumber} - HappyBoxx"
                : $"Payment Receipt {invoice.InvoiceNumber} - HappyBoxx")
            : subjectOverride.Trim();
        var introText = string.IsNullOrWhiteSpace(messageOverride)
            ? (includeInvoice
                ? "Please find your tax invoice and payment receipt attached as PDFs."
                : "Please find your payment receipt attached as PDF.")
            : messageOverride.Trim();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        foreach (var toEmail in toEmails)
        {
            message.To.Add(MailboxAddress.Parse(toEmail));
        }
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = BuildPaidPackageTextBody(invoice, paymentDetails, introText),
            HtmlBody = BuildPaidPackageHtmlBody(invoice, paymentDetails, introText)
        };
        if (includeInvoice)
        {
            bodyBuilder.Attachments.Add(GetPdfFileName(invoice.InvoiceNumber), BuildInvoicePdf(invoice), ContentType.Parse("application/pdf"));
        }
        bodyBuilder.Attachments.Add(GetReceiptPdfFileName(invoice.InvoiceNumber), BuildReceiptPdf(invoice, paymentDetails), ContentType.Parse("application/pdf"));
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static string BuildInvoiceTextBody(InvoiceEmailContent invoice, string introText)
    {
        var builder = new StringBuilder();
        builder.AppendLine(introText);
        builder.AppendLine();
        builder.AppendLine($"Invoice number: {invoice.InvoiceNumber}");
        builder.AppendLine($"Invoice date: {invoice.InvoiceDate:dd/MM/yyyy}");
        builder.AppendLine($"Customer: {invoice.CustomerName}");
        builder.AppendLine($"Total (incl. GST): {ToCurrency(invoice.Total)}");
        builder.AppendLine();
        builder.AppendLine("Payment details:");
        builder.AppendLine("ANZ · BSB 014219 · Account 470777199 · HAPPYBOXX PTY LTD");
        builder.AppendLine("Please pay within 7 days and use the invoice number as reference.");
        return builder.ToString();
    }

    private static string BuildInvoiceHtmlBody(InvoiceEmailContent invoice, string introText)
    {
        return $"<p>{Html(introText)}</p>"
            + $"<p><strong>Invoice number:</strong> {Html(invoice.InvoiceNumber)}<br/>"
            + $"<strong>Invoice date:</strong> {invoice.InvoiceDate:dd/MM/yyyy}<br/>"
            + $"<strong>Customer:</strong> {Html(invoice.CustomerName)}<br/>"
            + $"<strong>Total (incl. GST):</strong> {ToCurrency(invoice.Total)}</p>"
            + "<p><strong>Payment details:</strong><br/>ANZ · BSB 014219 · Account 470777199 · HAPPYBOXX PTY LTD<br/>"
            + "Please pay within 7 days and use the invoice number as reference.</p>";
    }

    private static string BuildReceiptTextBody(
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails payment,
        bool includeInvoice)
    {
        var builder = new StringBuilder();
        builder.AppendLine(includeInvoice
            ? "Thank you. We have received your payment. Your receipt and invoice PDFs are attached."
            : "Thank you. We have received your payment. Your receipt PDF is attached.");
        builder.AppendLine();
        builder.AppendLine($"Invoice number: {invoice.InvoiceNumber}");
        builder.AppendLine($"Customer: {invoice.CustomerName}");
        builder.AppendLine($"Paid on: {payment.PaidOn:dd/MM/yyyy}");
        builder.AppendLine($"Amount paid: {ToCurrency(invoice.Total)}");
        if (!string.IsNullOrWhiteSpace(payment.PaymentReference))
        {
            builder.AppendLine($"Reference: {payment.PaymentReference}");
        }

        if (!string.IsNullOrWhiteSpace(payment.PaymentNote))
        {
            builder.AppendLine($"Note: {payment.PaymentNote}");
        }

        return builder.ToString();
    }

    private static string BuildReceiptHtmlBody(
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails payment,
        bool includeInvoice)
    {
        var details = new StringBuilder();
        details.Append("<p>")
            .Append(includeInvoice
                ? "Thank you. We have received your payment. Your receipt and invoice PDFs are attached."
                : "Thank you. We have received your payment. Your receipt PDF is attached.")
            .Append("</p>")
            .Append("<p>")
            .Append("<strong>Invoice number:</strong> ").Append(Html(invoice.InvoiceNumber)).Append("<br/>")
            .Append("<strong>Customer:</strong> ").Append(Html(invoice.CustomerName)).Append("<br/>")
            .Append("<strong>Paid on:</strong> ").Append(payment.PaidOn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).Append("<br/>")
            .Append("<strong>Amount paid:</strong> ").Append(ToCurrency(invoice.Total));
        if (!string.IsNullOrWhiteSpace(payment.PaymentReference))
        {
            details.Append("<br/><strong>Reference:</strong> ").Append(Html(payment.PaymentReference));
        }

        if (!string.IsNullOrWhiteSpace(payment.PaymentNote))
        {
            details.Append("<br/><strong>Note:</strong> ").Append(Html(payment.PaymentNote));
        }

        details.Append("</p>");
        return details.ToString();
    }

    private static string BuildPaidPackageTextBody(
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails payment,
        string introText)
    {
        var builder = new StringBuilder();
        builder.AppendLine(introText);
        builder.AppendLine();
        builder.AppendLine($"Invoice number: {invoice.InvoiceNumber}");
        builder.AppendLine($"Customer: {invoice.CustomerName}");
        builder.AppendLine($"Paid on: {payment.PaidOn:dd/MM/yyyy}");
        builder.AppendLine($"Amount paid: {ToCurrency(invoice.Total)}");
        if (!string.IsNullOrWhiteSpace(payment.PaymentReference))
        {
            builder.AppendLine($"Reference: {payment.PaymentReference}");
        }

        return builder.ToString();
    }

    private static string BuildPaidPackageHtmlBody(
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails payment,
        string introText)
    {
        var details = new StringBuilder();
        details.Append("<p>").Append(Html(introText)).Append("</p>")
            .Append("<p>")
            .Append("<strong>Invoice number:</strong> ").Append(Html(invoice.InvoiceNumber)).Append("<br/>")
            .Append("<strong>Customer:</strong> ").Append(Html(invoice.CustomerName)).Append("<br/>")
            .Append("<strong>Paid on:</strong> ").Append(payment.PaidOn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).Append("<br/>")
            .Append("<strong>Amount paid:</strong> ").Append(ToCurrency(invoice.Total));
        if (!string.IsNullOrWhiteSpace(payment.PaymentReference))
        {
            details.Append("<br/><strong>Reference:</strong> ").Append(Html(payment.PaymentReference));
        }

        details.Append("</p>");
        return details.ToString();
    }

    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);
    private static string ToCurrency(decimal value) => $"A${value:0.00}";
    private static string GetPdfFileName(string invoiceNumber)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safeNumber = new string(invoiceNumber.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return $"Invoice-{safeNumber}.pdf";
    }

    private static string GetReceiptPdfFileName(string invoiceNumber)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safeNumber = new string(invoiceNumber.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return $"Receipt-{safeNumber}.pdf";
    }

    private static byte[] BuildInvoicePdf(InvoiceEmailContent invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(text => text.FontSize(10));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(company =>
                        {
                            company.Item().Text("HAPPY BOXX PTY LTD").SemiBold().FontSize(14);
                            company.Item().Text("3/8 Enterprise Street, Salisbury QLD 4107");
                            company.Item().Text("Phone: 0466 115 997 · happyboxx.info@gmail.com");
                            company.Item().Text("ABN: 59 629 823 285");
                        });

                        row.ConstantItem(180).AlignRight().Text("TAX INVOICE").SemiBold().FontSize(16);
                    });

                    column.Item().LineHorizontal(0.5f);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(customer =>
                        {
                            customer.Item().Text("Bill to").SemiBold();
                            customer.Item().Text(invoice.CustomerName);
                            customer.Item().Text(invoice.CustomerAddress);
                        });

                        row.ConstantItem(220).Column(details =>
                        {
                            details.Item().Text($"Invoice number: {invoice.InvoiceNumber}");
                            details.Item().Text($"Invoice date: {invoice.InvoiceDate:dd/MM/yyyy}");
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            static IContainer HeaderCell(IContainer container) =>
                                container.BorderBottom(0.5f).PaddingVertical(4).Background(Colors.Grey.Lighten3).PaddingHorizontal(4);

                            header.Cell().Element(HeaderCell).Text("Description").SemiBold();
                            header.Cell().Element(HeaderCell).Text("Unit").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Qty").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Unit price (incl. GST)").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount").SemiBold();
                        });

                        foreach (var line in invoice.Lines)
                        {
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(line.Description);
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(line.Unit);
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .AlignRight().Text(line.Quantity.ToString("0.###", CultureInfo.InvariantCulture));
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .AlignRight().Text(ToCurrency(line.UnitPriceInclusive));
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .AlignRight().Text(ToCurrency(line.AmountInclusive));
                        }
                    });

                    column.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal (ex GST): {ToCurrency(invoice.Subtotal)}");
                        totals.Item().Text($"GST included (10%): {ToCurrency(invoice.GstAmount)}");
                        totals.Item().Text($"Total (incl. GST): {ToCurrency(invoice.Total)}").SemiBold();
                    });

                    column.Item().Text("Payment details").SemiBold();
                    column.Item().Text("ANZ · BSB 014219 · Account 470777199 · HAPPYBOXX PTY LTD");
                    column.Item().Text("Please pay within 7 days and use the invoice number as reference.");
                });
            });
        }).GeneratePdf();
    }

    private static byte[] BuildReceiptPdf(InvoiceEmailContent invoice, InvoicePaymentReceiptDetails payment)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(text => text.FontSize(10));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(company =>
                        {
                            company.Item().Text("HAPPY BOXX PTY LTD").SemiBold().FontSize(14);
                            company.Item().Text("3/8 Enterprise Street, Salisbury QLD 4107");
                            company.Item().Text("Phone: 0466 115 997 · happyboxx.info@gmail.com");
                            company.Item().Text("ABN: 59 629 823 285");
                        });

                        row.ConstantItem(180).AlignRight().Text("PAYMENT RECEIPT").SemiBold().FontSize(16);
                    });

                    column.Item().LineHorizontal(0.5f);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(customer =>
                        {
                            customer.Item().Text("Bill to").SemiBold();
                            customer.Item().Text(invoice.CustomerName);
                            customer.Item().Text(invoice.CustomerAddress);
                        });

                        row.ConstantItem(220).Column(details =>
                        {
                            details.Item().Text($"Invoice number: {invoice.InvoiceNumber}");
                            details.Item().Text($"Invoice date: {invoice.InvoiceDate:dd/MM/yyyy}");
                            details.Item().Text($"Paid on: {payment.PaidOn:dd/MM/yyyy}");
                            details.Item().Text($"Amount paid: {ToCurrency(invoice.Total)}");
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            static IContainer HeaderCell(IContainer container) =>
                                container.BorderBottom(0.5f).PaddingVertical(4).Background(Colors.Grey.Lighten3).PaddingHorizontal(4);

                            header.Cell().Element(HeaderCell).Text("Description").SemiBold();
                            header.Cell().Element(HeaderCell).Text("Unit").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Qty").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Unit price (incl. GST)").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount").SemiBold();
                        });

                        foreach (var line in invoice.Lines)
                        {
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(line.Description);
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(line.Unit);
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .AlignRight().Text(line.Quantity.ToString("0.###", CultureInfo.InvariantCulture));
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .AlignRight().Text(ToCurrency(line.UnitPriceInclusive));
                            table.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .AlignRight().Text(ToCurrency(line.AmountInclusive));
                        }
                    });

                    column.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal (ex GST): {ToCurrency(invoice.Subtotal)}");
                        totals.Item().Text($"GST included (10%): {ToCurrency(invoice.GstAmount)}");
                        totals.Item().Text($"Total paid (incl. GST): {ToCurrency(invoice.Total)}").SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(payment.PaymentReference))
                    {
                        column.Item().Text($"Payment reference: {payment.PaymentReference}");
                    }

                    if (!string.IsNullOrWhiteSpace(payment.PaymentNote))
                    {
                        column.Item().Text($"Payment note: {payment.PaymentNote}");
                    }

                    column.Item().Text("Thank you for your payment.").SemiBold();
                });
            });
        }).GeneratePdf();
    }
}

public sealed class DisabledInvoiceEmailSender : IInvoiceEmailSender
{
    public bool IsEnabled => false;

    public Task SendInvoiceAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        string? subjectOverride,
        string? messageOverride,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Invoice email delivery is disabled.");

    public Task SendReceiptAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails paymentDetails,
        bool includeInvoice,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Invoice email delivery is disabled.");

    public Task SendPaidInvoicePackageAsync(
        IReadOnlyList<string> toEmails,
        InvoiceEmailContent invoice,
        InvoicePaymentReceiptDetails paymentDetails,
        bool includeInvoice,
        string? subjectOverride,
        string? messageOverride,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Invoice email delivery is disabled.");
}

public static class InvoiceEmailRegistration
{
    public static IServiceCollection AddInvoiceEmail(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Email").Get<InvoiceEmailOptions>()
            ?? new InvoiceEmailOptions();
        services.AddSingleton<IOptions<InvoiceEmailOptions>>(Options.Create(options));

        if (options.Mode == "Disabled")
        {
            services.AddSingleton<IInvoiceEmailSender, DisabledInvoiceEmailSender>();
            return services;
        }

        if (options.Mode != "GmailSmtp"
            || string.IsNullOrWhiteSpace(options.Host)
            || options.Port is < 1 or > 65535
            || string.IsNullOrWhiteSpace(options.UserName)
            || string.IsNullOrWhiteSpace(options.FromAddress)
            || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "Email:Mode must be GmailSmtp with Host, Port, UserName, FromAddress, and Password configured. "
                + "Store Password outside source control, such as in .NET user-secrets or a deployment secret store.");
        }

        services.AddTransient<IInvoiceEmailSender, InvoiceEmailSender>();
        return services;
    }
}
