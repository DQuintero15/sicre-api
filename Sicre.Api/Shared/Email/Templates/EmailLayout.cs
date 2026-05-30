namespace Sicre.Api.Shared.Email.Templates;

internal static class EmailLayout
{
    private const string LlanogasLogoUrl =
        "https://www.llanogas.com/sites/default/files/2025-08/logo-llanogas.png";

    private const string CusianagasLogoUrl =
        "https://www.llanogas.com/sites/default/files/2025-08/logo-cusianagas.png";

    internal static string Wrap(string bodyHtml)
    {
        var logos = $"""
            <table width="100%" cellpadding="0" cellspacing="0" style="padding:24px 0 20px;">
              <tr>
                <td align="center">
                  <table cellpadding="0" cellspacing="0">
                    <tr>
                      <td style="padding:0 8px 0 0;vertical-align:middle;">
                        <img src="{LlanogasLogoUrl}" alt="Llanogas" width="110" style="display:block;height:auto;max-height:32px;" />
                      </td>
                      <td style="padding:0 0 0 8px;vertical-align:middle;">
                        <img src="{CusianagasLogoUrl}" alt="Cusianagas" width="80" style="display:block;height:auto;max-height:24px;" />
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            """;

        return $"""
            <html lang="es">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            </head>
            <body style="margin:0;padding:0;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;background-color:#ffffff;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:24px 16px;">
                <tr>
                  <td align="center">
                    <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">

                      {logos}

                      <tr>
                        <td style="padding:0 0 20px;">
                          {bodyHtml}
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:16px 0 0;border-top:1px solid #e5e7eb;">
                          <p style="margin:0;font-size:11px;color:#9ca3af;text-align:center;">
                            SICRE &mdash; Sistema de Consolidacion y Reporte a Entidades
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
}
