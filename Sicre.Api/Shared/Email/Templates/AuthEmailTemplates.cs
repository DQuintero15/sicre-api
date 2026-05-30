using Sicre.Api.Features.Auth.Dtos;

namespace Sicre.Api.Shared.Email.Templates;

internal static class AuthEmailTemplates
{
    internal static string Invitation(
        string fullName,
        string temporaryPassword,
        string frontendUrl
    ) =>
        EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:20px;">Acceso a SICRE</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hola <strong>{{fullName}}</strong>,</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 20px;line-height:1.6;">Use la siguiente contraseña temporal para ingresar:</p>
            <div style="background-color:#f9fafb;padding:16px 20px;margin:0 0 24px;">
              <p style="color:#1d3e81;font-size:20px;font-weight:700;font-family:Courier New,monospace;letter-spacing:1px;margin:0;text-align:center;">{{temporaryPassword}}</p>
            </div>
            <p style="color:#6b7280;font-size:13px;margin:0 0 28px;">Debera cambiar su contrasena al iniciar sesion.</p>
            <a href="{{frontendUrl}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Ir a la plataforma</a>
            """);

    internal static string PasswordReset(string fullName, string resetLink) =>
        EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:20px;">Restablecer Contrasena</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hola <strong>{{fullName}}</strong>,</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hemos recibido una solicitud para restablecer la contrasena de su cuenta en SICRE.</p>
            <p style="color:#6b7280;font-size:13px;margin:0 0 24px;">Haga clic en el siguiente boton para crear una nueva contrasena:</p>
            <a href="{{resetLink}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:14px 45px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Restablecer Contrasena</a>
            <p style="color:#9ca3af;font-size:12px;margin:24px 0 0;line-height:1.5;">Este enlace expirara en 1 hora. Si no solicito restablecer su contrasena, puede ignorar este correo.</p>
            """);

    internal static string LoginNotification(LoginNotificationEmailDto data)
    {
        var browserInfo = string.IsNullOrEmpty(data.Browser) ? data.UserAgent : data.Browser;
        return EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:20px;">Notificacion de Seguridad</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 20px;line-height:1.6;">Hola <strong>{{data.UserName}}</strong>, se ha detectado un inicio de sesion en tu cuenta.</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
              <tr>
                <td style="padding:8px 0;color:#6b7280;font-size:13px;">Fecha</td>
                <td style="padding:8px 0;color:#111827;font-size:13px;">{{data.LoginTime:dd/MM/yyyy HH:mm:ss}} UTC</td>
              </tr>
              <tr>
                <td style="padding:8px 0;color:#6b7280;font-size:13px;">Direccion IP</td>
                <td style="padding:8px 0;color:#111827;font-size:13px;font-family:Monaco,Courier,monospace;">{{data.IpAddress}}</td>
              </tr>
              <tr>
                <td style="padding:8px 0;color:#6b7280;font-size:13px;">Navegador</td>
                <td style="padding:8px 0;color:#111827;font-size:13px;">{{browserInfo}}</td>
              </tr>
              <tr>
                <td style="padding:8px 0;color:#6b7280;font-size:13px;">Sistema Operativo</td>
                <td style="padding:8px 0;color:#111827;font-size:13px;">{{data.OperatingSystem}}</td>
              </tr>
            </table>
            <p style="color:#9ca3af;font-size:12px;margin:0;">Si no fuiste tu, contacta con soporte.</p>
            """);
    }

    internal static string EmailChanged(string fullName, string newEmail, string frontendUrl) =>
        EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:20px;">Cambio de Correo Electronico</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hola <strong>{{fullName}}</strong>,</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 20px;line-height:1.6;">Te informamos que tu correo electronico de acceso a SICRE ha sido actualizado.</p>
            <div style="background-color:#f9fafb;padding:14px 20px;margin:0 0 24px;">
              <p style="color:#6b7280;font-size:13px;margin:0 0 6px;">Tu nuevo correo electronico es:</p>
              <p style="color:#1d3e81;font-size:16px;font-weight:600;margin:0;">{{newEmail}}</p>
            </div>
            <p style="color:#6b7280;font-size:13px;margin:0 0 28px;">A partir de ahora, utiliza este correo para iniciar sesion en la plataforma.</p>
            <a href="{{frontendUrl}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Ir a la plataforma</a>
            <p style="color:#9ca3af;font-size:12px;margin:24px 0 0;">Si no solicitaste este cambio, contacta con el administrador del sistema.</p>
            """);
}
