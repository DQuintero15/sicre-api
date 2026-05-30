using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Sicre.Api.Config;
using Sicre.Api.Shared.Email.Templates;

namespace Sicre.Api.Shared.Email;

public class EmailAttachment
{
    public required string FileName { get; set; }
    public required byte[] Content { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
}

public interface IEmailService
{
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        List<EmailAttachment>? attachments = null
    );
}

public class EmailService(
    ILogger<EmailService> logger,
    IOptions<AppSettings> options,
    IWebHostEnvironment env
) : IEmailService
{
    private readonly SmtpSettings _smtp = options.Value.Smtp;

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        List<EmailAttachment>? attachments = null
    )
    {
        var originalRecipient = toEmail;

        if (!string.IsNullOrWhiteSpace(_smtp.RedirectTo))
        {
            toEmail = _smtp.RedirectTo.Trim();
            subject = $"[REDIRIGIDO] {subject}";
            body = BuildRedirectedBody(originalRecipient, body, isHtml);
            logger.LogWarning(
                "Email redirect activo. Destinatario original {Original} → {Redirect}",
                originalRecipient,
                toEmail
            );
        }

        try
        {
            logger.LogInformation(
                "Enviando email a {To} — asunto: '{Subject}' via {Host}:{Port} (modo: {Mode})",
                toEmail,
                subject,
                _smtp.Host,
                _smtp.Port,
                env.IsDevelopment() ? "tradicional" : "relay"
            );

            using var client = new SmtpClient();

            if (env.IsDevelopment())
            {
                var ssl =
                    (_smtp.EnableSsl) ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await client.ConnectAsync(_smtp.Host, _smtp.Port, ssl);
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
            }
            else
            {
                // Producción: SMTP relay sin autenticación
                await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.None);
            }

            var message = BuildMessage(toEmail, subject, body, isHtml, attachments);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email enviado exitosamente a {To}", toEmail);
            return true;
        }
        catch (SmtpCommandException ex)
        {
            logger.LogError(
                ex,
                "Error de comando SMTP enviando a {To}. StatusCode: {Code}",
                toEmail,
                ex.StatusCode
            );
            return false;
        }
        catch (SmtpProtocolException ex)
        {
            logger.LogError(ex, "Error de protocolo SMTP enviando a {To}", toEmail);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enviando email a {To}", toEmail);
            return false;
        }
    }

    private MimeMessage BuildMessage(
        string to,
        string subject,
        string body,
        bool isHtml,
        List<EmailAttachment>? attachments
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.DisplayName, _smtp.Username));
        message.To.Add(new MailboxAddress(string.Empty, to));
        message.Subject = subject;

        var builder = new BodyBuilder();
        if (isHtml)
            builder.HtmlBody = body;
        else
            builder.TextBody = body;

        if (isHtml)
        {
            foreach (var (contentId, data) in EmailLayout.GetInlineLogos())
            {
                var resource = builder.LinkedResources.Add(
                    contentId + ".webp",
                    data,
                    new ContentType("image", "webp")
                );
                resource.ContentId = contentId;
                resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            }
        }

        if (attachments?.Count > 0)
        {
            foreach (var a in attachments)
                builder.Attachments.Add(a.FileName, a.Content, ContentType.Parse(a.ContentType));
        }

        message.Body = builder.ToMessageBody();
        return message;
    }

    private static string BuildRedirectedBody(string original, string body, bool isHtml)
    {
        if (isHtml)
            return $"""
                <div style="padding:12px;margin-bottom:16px;border:1px solid #f59e0b;background:#fffbeb;color:#92400e;font-family:Arial,sans-serif;font-size:14px;">
                    <strong>Email redirigido por ambiente de pruebas.</strong><br />
                    Destinatario original: {System.Net.WebUtility.HtmlEncode(original)}
                </div>
                {body}
                """;

        return $"Email redirigido por ambiente de pruebas.\nDestinatario original: {original}\n\n{body}";
    }
}
