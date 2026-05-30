namespace Sicre.Api.Shared.Email.Templates;

public static class MonthlyReportEmailTemplate
{
    public static string Build(string periodLabel, string dashboardUrl) =>
        EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 12px;font-weight:600;font-size:18px;">Informe Mensual de Cumplimiento</h2>
            <p style="color:#6b7280;font-size:12px;margin:0 0 20px;font-weight:500;">{{periodLabel}}</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 20px;line-height:1.6;">
              El informe mensual de cumplimiento correspondiente al periodo <strong>{{periodLabel}}</strong> se encuentra adjunto a este correo en formato PDF.
            </p>
            <p style="color:#4b5563;font-size:13px;margin:0 0 16px;font-weight:500;">El informe incluye:</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
              <tr>
                <td style="padding:2px 0;font-size:13px;color:#4b5563;">Resumen ejecutivo por estados</td>
                <td style="padding:2px 0;font-size:13px;color:#4b5563;">Cumplimiento por entidad de control</td>
              </tr>
              <tr>
                <td style="padding:2px 0;font-size:13px;color:#4b5563;">Tendencia de los ultimos 12 meses</td>
                <td style="padding:2px 0;font-size:13px;color:#4b5563;">Cumplimiento por sede</td>
              </tr>
              <tr>
                <td style="padding:2px 0;font-size:13px;color:#4b5563;">Cumplimiento por responsable</td>
                <td></td>
              </tr>
            </table>
            <p style="color:#6b7280;font-size:13px;margin:0 0 24px;">Para un analisis interactivo con filtros adicionales, acceda al tablero de analitica en SICRE.</p>
            <a href="{{dashboardUrl}}/analytics" style="background:#1d3e81;color:#ffffff;font-size:14px;font-weight:600;padding:12px 28px;border-radius:6px;text-decoration:none;display:inline-block;">Ir al tablero de analitica</a>
            """);
}
