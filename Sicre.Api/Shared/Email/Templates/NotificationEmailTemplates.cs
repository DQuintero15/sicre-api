namespace Sicre.Api.Shared.Email.Templates;

internal static class NotificationEmailTemplates
{
    internal static string MonthlyStatus(string userName, string monthName, string frontendUrl) =>
        $"""
            <html lang='es'>
            <body style='margin:0;padding:0;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;background-color:#f5f5f5;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
                    <tr><td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#fff;border-radius:10px;box-shadow:0 3px 6px rgba(0,0,0,0.1);'>
                            <tr><td style='text-align:center;padding:30px;'>
                                <h2 style='color:#1d1d1d;margin:0 0 20px;font-weight:600;'>Reporte Mensual de Estado</h2>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Hola <strong>{userName}</strong>,</p>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>
                                    Adjunto encontrarás el reporte mensual de estado de las obligaciones correspondientes a <strong>{monthName}</strong>.
                                </p>
                                <p style='color:#555;font-size:14px;margin-bottom:30px;'>
                                    Este documento contiene el detalle del estado de los reportes bajo su supervisión o administración.
                                </p>
                                <a href='{frontendUrl}' style='background:#1d3e81;color:#fff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;'>Ir a la plataforma</a>
                            </td></tr>
                            <tr><td style='background-color:#f5f5f5;padding:20px;text-align:center;font-size:12px;color:#888;'>© 2025 Grupo del Llano. Todos los derechos reservados.</td></tr>
                        </table>
                    </td></tr>
                </table>
            </body>
            </html>
            """;
}
