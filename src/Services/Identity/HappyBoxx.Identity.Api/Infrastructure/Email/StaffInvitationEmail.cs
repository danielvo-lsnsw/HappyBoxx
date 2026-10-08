using MailKit.Net.Smtp;
using MailKit.Security;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HappyBoxx.Identity.Api.Infrastructure.Email;

public sealed class StaffInvitationEmailOptions
{
    public string Mode { get; init; } = "Disabled";
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string UserName { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "HappyBoxx";
    public string PublicBaseUrl { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public interface IStaffInvitationEmailSender
{
    bool IsEnabled { get; }
    string CreateInvitationUrl(string invitationCode);
    Task SendAsync(string email, string role, string invitationUrl, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
}

public sealed class StaffInvitationEmailSender(IOptions<StaffInvitationEmailOptions> options) : IStaffInvitationEmailSender
{
    private readonly StaffInvitationEmailOptions _options = options.Value;

    public bool IsEnabled => string.Equals(_options.Mode, "GmailSmtp", StringComparison.OrdinalIgnoreCase);

    public string CreateInvitationUrl(string invitationCode)
    {
        var baseUrl = new Uri(_options.PublicBaseUrl.EndsWith('/') ? _options.PublicBaseUrl : $"{_options.PublicBaseUrl}/");
        return new Uri(baseUrl, $"accept-invitation#code={Uri.EscapeDataString(invitationCode)}").ToString();
    }

    public async Task SendAsync(
        string email,
        string role,
        string invitationUrl,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Invitation email delivery is disabled.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Your HappyBoxx staff invitation";
        var encodedRole = HtmlEncoder.Default.Encode(role);
        var encodedInvitationUrl = HtmlEncoder.Default.Encode(invitationUrl);
        var textBody = new TextPart("plain")
        {
            Text = $"You have been invited to HappyBoxx as {role}.\n\n"
                + $"Accept the invitation: {invitationUrl}\n\n"
                + $"This link can be used once and expires at {expiresAtUtc:O}. "
                + "It only works when you sign in with the invited email address.",
        };
        var htmlBody = new TextPart("html")
        {
            Text = $"<p>You have been invited to HappyBoxx as <strong>{encodedRole}</strong>.</p>"
                + $"<p><a href=\"{encodedInvitationUrl}\">Accept invitation</a></p>"
                + $"<p>This link can be used once and expires at {expiresAtUtc:O}. "
                + "It only works when you sign in with the invited email address.</p>",
        };
        message.Body = new Multipart("alternative") { textBody, htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

public sealed class DisabledStaffInvitationEmailSender(IOptions<StaffInvitationEmailOptions> options) : IStaffInvitationEmailSender
{
    private readonly StaffInvitationEmailOptions _options = options.Value;

    public bool IsEnabled => false;

    public string CreateInvitationUrl(string invitationCode)
    {
        var baseUrl = new Uri(_options.PublicBaseUrl.EndsWith('/') ? _options.PublicBaseUrl : $"{_options.PublicBaseUrl}/");
        return new Uri(baseUrl, $"accept-invitation#code={Uri.EscapeDataString(invitationCode)}").ToString();
    }

    public Task SendAsync(
        string email,
        string role,
        string invitationUrl,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Invitation email delivery is disabled.");
}

public static class StaffInvitationEmailRegistration
{
    public static IServiceCollection AddStaffInvitationEmail(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection("Email").Get<StaffInvitationEmailOptions>()
            ?? new StaffInvitationEmailOptions();
        if (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var publicBaseUri)
            || (!environment.IsDevelopment() && publicBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Email:PublicBaseUrl must be an absolute HTTPS URL outside Development mode.");
        }

        services.AddSingleton<IOptions<StaffInvitationEmailOptions>>(Options.Create(options));
        if (environment.IsDevelopment() && options.Mode == "Disabled")
        {
            services.AddSingleton<IStaffInvitationEmailSender, DisabledStaffInvitationEmailSender>();
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

        services.AddTransient<IStaffInvitationEmailSender, StaffInvitationEmailSender>();
        return services;
    }
}