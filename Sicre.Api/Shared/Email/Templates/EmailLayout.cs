namespace Sicre.Api.Shared.Email.Templates;

internal static class EmailLayout
{
    private static byte[]? _llanogasBytes;
    private static byte[]? _cusianagasBytes;
    private static bool _initialized;

    internal static void Initialize(string contentRootPath)
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            var llanogasPath = Path.Combine(
                contentRootPath,
                "Assets",
                "Images",
                "logo-llanogas.webp"
            );
            var cusianagasPath = Path.Combine(
                contentRootPath,
                "Assets",
                "Images",
                "logo-cusianagas.webp"
            );

            if (File.Exists(llanogasPath))
                _llanogasBytes = File.ReadAllBytes(llanogasPath);

            if (File.Exists(cusianagasPath))
                _cusianagasBytes = File.ReadAllBytes(cusianagasPath);
        }
        catch
        {
            // Logos are optional
        }
    }

    // Returns (contentId, bytes) pairs for MailKit LinkedResources
    internal static IEnumerable<(string ContentId, byte[] Data)> GetInlineLogos()
    {
        if (_llanogasBytes != null)
            yield return ("logo-llanogas", _llanogasBytes);
        if (_cusianagasBytes != null)
            yield return ("logo-cusianagas", _cusianagasBytes);
    }

    internal static string Wrap(string bodyHtml)
    {
        var hasLlanogas = _llanogasBytes != null;
        var hasCusianagas = _cusianagasBytes != null;

        string logosHtml;

        if (hasLlanogas && hasCusianagas)
        {
            logosHtml = """
                <table width="100%" cellpadding="0" cellspacing="0" style="padding:24px 0 20px;">
                  <tr>
                    <td align="center">
                      <table cellpadding="0" cellspacing="0">
                        <tr>
                          <td style="padding:0 8px 0 0;vertical-align:middle;">
                            <img src="cid:logo-llanogas" alt="Llanogas" width="110" style="display:block;height:auto;max-height:32px;" />
                          </td>
                          <td style="padding:0 0 0 8px;vertical-align:middle;">
                            <img src="cid:logo-cusianagas" alt="Cusianagas" width="80" style="display:block;height:auto;max-height:24px;" />
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
                """;
        }
        else if (hasLlanogas)
        {
            logosHtml = """
                <table width="100%" cellpadding="0" cellspacing="0" style="padding:24px 0 20px;">
                  <tr>
                    <td align="center">
                      <img src="cid:logo-llanogas" alt="Llanogas" width="110" style="display:block;height:auto;max-height:32px;" />
                    </td>
                  </tr>
                </table>
                """;
        }
        else
        {
            logosHtml = "";
        }

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

                      {logosHtml}

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
