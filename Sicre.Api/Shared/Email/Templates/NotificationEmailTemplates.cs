namespace Sicre.Api.Shared.Email.Templates;

internal static class NotificationEmailTemplates
{
    internal static string InstanceEvent(
        string userName,
        string eventType,
        string title,
        string content,
        Guid instanceId,
        string frontendUrl
    )
    {
        var instanceUrl = $"{frontendUrl.TrimEnd('/')}/report-instances/{instanceId}";

        return EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:18px;">{{title}}</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 8px;line-height:1.6;">Hola <strong>{{userName}}</strong>,</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 28px;line-height:1.6;">{{content}}</p>
            <a href="{{instanceUrl}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:12px 32px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Ver instancia</a>
            """);
    }

    internal static string MonthlyStatus(string userName, string monthName, string frontendUrl) =>
        EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:18px;">Reporte Mensual de Estado</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hola <strong>{{userName}}</strong>,</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">
              Adjunto encontraras el reporte mensual de estado de las obligaciones correspondientes a <strong>{{monthName}}</strong>.
            </p>
            <p style="color:#6b7280;font-size:13px;margin:0 0 28px;">Este documento contiene el detalle del estado de los reportes bajo su supervision o administracion.</p>
            <a href="{{frontendUrl}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Ir a la plataforma</a>
            """);
}
