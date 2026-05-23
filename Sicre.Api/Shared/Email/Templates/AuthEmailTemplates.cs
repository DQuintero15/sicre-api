using Sicre.Api.Features.Auth.Dtos;

namespace Sicre.Api.Shared.Email.Templates;

internal static class AuthEmailTemplates
{
    internal static string Invitation(
        string fullName,
        string temporaryPassword,
        string frontendUrl
    ) =>
        $"""
            <html lang='es'>
            <body style='margin:0;padding:0;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;background-color:#f5f5f5;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
                    <tr><td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#fff;border-radius:10px;box-shadow:0 3px 6px rgba(0,0,0,0.1);'>
                            <tr><td style='text-align:center;padding:30px;'>
                                <h2 style='color:#1d1d1d;margin:0 0 20px;font-weight:600;'>Acceso a SICRE</h2>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Hola <strong>{fullName}</strong>,</p>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Use la siguiente contraseña temporal para ingresar:</p>
                                <div style='background-color:#fefefe;border:2px solid #1d3e81;border-radius:8px;padding:20px;margin-bottom:25px;text-align:center;'>
                                    <p style='color:#1d3e81;font-size:22px;font-weight:700;font-family:"Courier New",monospace;letter-spacing:1px;margin:0;'>{temporaryPassword}</p>
                                </div>
                                <p style='color:#555;font-size:14px;margin-bottom:30px;'>Deberá cambiar su contraseña al iniciar sesión.</p>
                                <a href='{frontendUrl}' style='background:#1d3e81;color:#fff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;'>Ir a la plataforma</a>
                            </td></tr>
                            <tr><td style='background-color:#f5f5f5;padding:20px;text-align:center;font-size:12px;color:#888;'>© 2025 Grupo del Llano. Todos los derechos reservados.</td></tr>
                        </table>
                    </td></tr>
                </table>
            </body>
            </html>
            """;

    internal static string PasswordReset(string fullName, string resetLink) =>
        $"""
            <html lang='es'>
            <body style='margin:0;padding:0;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;background-color:#f5f5f5;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
                    <tr><td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#fff;border-radius:10px;box-shadow:0 3px 6px rgba(0,0,0,0.1);'>
                            <tr><td style='text-align:center;padding:30px;'>
                                <h2 style='color:#1d1d1d;margin:0 0 20px;font-weight:600;'>Restablecer Contraseña</h2>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Hola <strong>{fullName}</strong>,</p>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Hemos recibido una solicitud para restablecer la contraseña de su cuenta en SICRE.</p>
                                <p style='color:#555;font-size:14px;margin-bottom:25px;'>Haga clic en el siguiente botón para crear una nueva contraseña:</p>
                                <a href='{resetLink}' style='background:#1d3e81;color:#fff;text-decoration:none;padding:14px 45px;border-radius:6px;font-weight:600;display:inline-block;font-size:15px;'>Restablecer Contraseña</a>
                                <div style='background-color:#fff8e1;border:1px solid #ffe082;border-radius:6px;padding:15px;margin:25px 0;text-align:left;'>
                                    <p style='color:#856404;font-size:13px;margin:0;'><strong>⏰ Importante:</strong> Este enlace expirará en <strong>1 hora</strong>.</p>
                                </div>
                                <p style='color:#888;font-size:13px;margin-top:20px;'>Si no solicitó restablecer su contraseña, puede ignorar este correo. Su cuenta permanecerá segura.</p>
                            </td></tr>
                            <tr><td style='background-color:#f5f5f5;padding:20px;text-align:center;font-size:12px;color:#888;'>© 2025 Grupo del Llano. Todos los derechos reservados.</td></tr>
                        </table>
                    </td></tr>
                </table>
            </body>
            </html>
            """;

    internal static string LoginNotification(LoginNotificationEmailDto data)
    {
        var browserInfo = string.IsNullOrEmpty(data.Browser) ? data.UserAgent : data.Browser;
        return $"""
            <html lang='es'>
            <body style='margin:0;padding:0;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif;background-color:#f8f9fb;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
                    <tr><td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:8px;box-shadow:0 1px 3px rgba(0,0,0,0.05);'>
                            <tr><td style='text-align:center;padding:40px 30px 30px;border-bottom:1px solid #f0f0f0;'>
                                <h2 style='color:#0a0e27;margin:0;font-weight:700;font-size:20px;'>Notificación de Seguridad</h2>
                            </td></tr>
                            <tr><td style='padding:30px;'>
                                <p style='color:#0a0e27;font-size:15px;margin:0 0 20px;font-weight:500;'>Hola <strong>{data.UserName}</strong>,</p>
                                <p style='color:#525f7f;font-size:14px;margin:0 0 25px;line-height:1.6;'>Se ha detectado un inicio de sesión en tu cuenta. Si no fuiste tú, por favor contacta con soporte.</p>
                                <p style='color:#0a0e27;font-size:13px;margin:20px 0 15px;font-weight:600;'>Detalles de la sesión:</p>
                                <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                                    <tr style='border-bottom:1px solid #f0f0f0;'>
                                        <td style='padding:12px 0;color:#525f7f;font-size:13px;font-weight:500;width:30%;'>Fecha</td>
                                        <td style='padding:12px 0;color:#0a0e27;font-size:13px;'>{data.LoginTime:dd/MM/yyyy HH:mm:ss} UTC</td>
                                    </tr>
                                    <tr style='border-bottom:1px solid #f0f0f0;'>
                                        <td style='padding:12px 0;color:#525f7f;font-size:13px;font-weight:500;'>Dirección IP</td>
                                        <td style='padding:12px 0;color:#0a0e27;font-size:13px;font-family:"Monaco","Courier",monospace;'>{data.IpAddress}</td>
                                    </tr>
                                    <tr style='border-bottom:1px solid #f0f0f0;'>
                                        <td style='padding:12px 0;color:#525f7f;font-size:13px;font-weight:500;'>Navegador</td>
                                        <td style='padding:12px 0;color:#0a0e27;font-size:13px;'>{browserInfo}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:12px 0;color:#525f7f;font-size:13px;font-weight:500;'>Sistema Operativo</td>
                                        <td style='padding:12px 0;color:#0a0e27;font-size:13px;'>{data.OperatingSystem}</td>
                                    </tr>
                                </table>
                            </td></tr>
                            <tr><td style='background-color:#f8f9fb;padding:20px 30px;text-align:center;font-size:12px;color:#8b8fa2;border-top:1px solid #f0f0f0;border-radius:0 0 8px 8px;'>© 2025 Grupo del Llano. Todos los derechos reservados.</td></tr>
                        </table>
                    </td></tr>
                </table>
            </body>
            </html>
            """;
    }

    internal static string EmailChanged(string fullName, string newEmail, string frontendUrl) =>
        $"""
            <html lang='es'>
            <body style='margin:0;padding:0;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;background-color:#f5f5f5;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
                    <tr><td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#fff;border-radius:10px;box-shadow:0 3px 6px rgba(0,0,0,0.1);'>
                            <tr><td style='text-align:center;padding:30px;'>
                                <h2 style='color:#1d1d1d;margin:0 0 20px;font-weight:600;'>Cambio de Correo Electrónico</h2>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Hola <strong>{fullName}</strong>,</p>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Te informamos que tu correo electrónico de acceso a SICRE ha sido actualizado.</p>
                                <div style='background-color:#e8f4fd;border:1px solid #1d3e81;border-radius:8px;padding:20px;margin-bottom:25px;text-align:center;'>
                                    <p style='color:#555;font-size:14px;margin:0 0 10px;'>Tu nuevo correo electrónico es:</p>
                                    <p style='color:#1d3e81;font-size:18px;font-weight:700;margin:0;'>{newEmail}</p>
                                </div>
                                <p style='color:#555;font-size:14px;margin-bottom:30px;'>A partir de ahora, utiliza este correo para iniciar sesión en la plataforma.</p>
                                <a href='{frontendUrl}' style='background:#1d3e81;color:#fff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;'>Ir a la plataforma</a>
                                <p style='color:#888;font-size:13px;margin-top:25px;'>Si no solicitaste este cambio, por favor contacta con el administrador del sistema.</p>
                            </td></tr>
                            <tr><td style='background-color:#f5f5f5;padding:20px;text-align:center;font-size:12px;color:#888;'>© 2025 Grupo del Llano. Todos los derechos reservados.</td></tr>
                        </table>
                    </td></tr>
                </table>
            </body>
            </html>
            """;
}
