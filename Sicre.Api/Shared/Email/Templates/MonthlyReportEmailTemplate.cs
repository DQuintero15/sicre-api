namespace Sicre.Api.Shared.Email.Templates;

public static class MonthlyReportEmailTemplate
{
    public static string Build(string periodLabel, string dashboardUrl) =>
        $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <title>Informe Mensual de Cumplimiento — SICRE</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f1f5f9;font-family:Arial,Helvetica,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f1f5f9;padding:32px 0;">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">

                      <!-- Header -->
                      <tr>
                        <td style="background-color:#1d3e81;padding:28px 36px;">
                          <table width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td>
                                <p style="margin:0;font-size:11px;font-weight:600;color:#93c5fd;letter-spacing:1.2px;text-transform:uppercase;">Sistema de Consolidación y Reporte a Entidades</p>
                                <h1 style="margin:6px 0 0;font-size:22px;font-weight:700;color:#ffffff;">Informe Mensual de Cumplimiento</h1>
                              </td>
                              <td align="right" style="vertical-align:middle;">
                                <span style="display:inline-block;background:rgba(255,255,255,0.15);color:#ffffff;font-size:13px;font-weight:700;padding:8px 16px;border-radius:20px;white-space:nowrap;">{periodLabel}</span>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Body -->
                      <tr>
                        <td style="padding:32px 36px;">

                          <p style="margin:0 0 20px;font-size:15px;color:#0f172a;line-height:1.6;">
                            El informe mensual de cumplimiento correspondiente al período <strong>{periodLabel}</strong> se encuentra adjunto a este correo en formato PDF.
                          </p>

                          <!-- Info box -->
                          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f8fafc;border:1px solid #e2e8f0;border-left:4px solid #1d3e81;border-radius:0 8px 8px 0;margin-bottom:24px;">
                            <tr>
                              <td style="padding:16px 20px;">
                                <p style="margin:0 0 10px;font-size:12px;font-weight:700;color:#1d3e81;text-transform:uppercase;letter-spacing:0.8px;">El informe incluye</p>
                                <table width="100%" cellpadding="0" cellspacing="0">
                                  <tr>
                                    <td width="50%" style="vertical-align:top;padding-bottom:6px;">
                                      <p style="margin:0;font-size:13px;color:#334155;">&#10003;&nbsp; Resumen ejecutivo por estados</p>
                                      <p style="margin:4px 0 0;font-size:13px;color:#334155;">&#10003;&nbsp; Tendencia de los últimos 12 meses</p>
                                      <p style="margin:4px 0 0;font-size:13px;color:#334155;">&#10003;&nbsp; Cumplimiento por entidad de control</p>
                                    </td>
                                    <td width="50%" style="vertical-align:top;">
                                      <p style="margin:0;font-size:13px;color:#334155;">&#10003;&nbsp; Cumplimiento por sede</p>
                                      <p style="margin:4px 0 0;font-size:13px;color:#334155;">&#10003;&nbsp; Cumplimiento por responsable</p>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                          </table>

                          <!-- Status legend -->
                          <p style="margin:0 0 10px;font-size:12px;font-weight:700;color:#64748b;text-transform:uppercase;letter-spacing:0.8px;">Referencia de estados</p>
                          <table cellpadding="0" cellspacing="0" style="margin-bottom:28px;">
                            <tr>
                              <td style="padding-right:16px;padding-bottom:6px;">
                                <span style="display:inline-block;background:#dcfce7;color:#15803d;font-size:11px;font-weight:700;padding:3px 10px;border-radius:20px;">A Tiempo</span>
                              </td>
                              <td style="padding-right:16px;padding-bottom:6px;">
                                <span style="display:inline-block;background:#ffedd5;color:#c2410c;font-size:11px;font-weight:700;padding:3px 10px;border-radius:20px;">Tarde</span>
                              </td>
                              <td style="padding-right:16px;padding-bottom:6px;">
                                <span style="display:inline-block;background:#fee2e2;color:#b91c1c;font-size:11px;font-weight:700;padding:3px 10px;border-radius:20px;">No Reportado</span>
                              </td>
                              <td style="padding-bottom:6px;">
                                <span style="display:inline-block;background:#fef9c3;color:#854d0e;font-size:11px;font-weight:700;padding:3px 10px;border-radius:20px;">Pendiente</span>
                              </td>
                            </tr>
                          </table>

                          <!-- CTA -->
                          <p style="margin:0 0 24px;font-size:14px;color:#334155;line-height:1.6;">
                            Para un análisis interactivo con filtros adicionales, acceda al tablero de analítica en SICRE.
                          </p>
                          <a href="{dashboardUrl}/analytics" style="display:inline-block;background:#1d3e81;color:#ffffff;font-size:14px;font-weight:600;padding:12px 28px;border-radius:8px;text-decoration:none;">
                            Ir al tablero de analítica &rarr;
                          </a>

                        </td>
                      </tr>

                      <!-- Divider -->
                      <tr>
                        <td style="padding:0 36px;">
                          <hr style="border:none;border-top:1px solid #e2e8f0;margin:0;" />
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="padding:20px 36px 28px;">
                          <p style="margin:0;font-size:12px;color:#94a3b8;line-height:1.6;">
                            Este correo fue generado automáticamente por <strong>SICRE</strong> el día 1 del mes.
                            Si no debería recibirlo, contáctese con el administrador del sistema.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
}
