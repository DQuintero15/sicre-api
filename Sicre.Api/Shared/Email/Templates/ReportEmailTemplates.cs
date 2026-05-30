using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Reports.Dtos;

namespace Sicre.Api.Shared.Email.Templates;

internal static class ReportEmailTemplates
{
    internal static string ReportAlert(
        string userName,
        string reportName,
        string periodName,
        DateOnly dueDate,
        string alertType,
        int daysRemaining,
        bool isOverdue,
        string frontendUrl,
        string backendUrl,
        string? branchName = null
    )
    {
        var color = alertType switch
        {
            "Alerta Temprana" => "#17a2b8",
            "Seguimiento" => "#d97706",
            "Critica" => "#dc2626",
            _ => "#6b7280",
        };

        var branchRow = string.IsNullOrWhiteSpace(branchName)
            ? ""
            : $"<span style=\"color:#6b7280;font-size:13px;\"> | Sede: {branchName}</span>";

        return EmailLayout.Wrap($$"""
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 16px;">
              <tr>
                <td style="background-color:{{color}};padding:4px 12px;border-radius:4px;">
                  <p style="margin:0;color:#ffffff;font-size:11px;font-weight:600;letter-spacing:0.5px;">{{alertType}}</p>
                </td>
              </tr>
            </table>
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:18px;">Recordatorio de Reporte</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hola <strong>{{userName}}</strong>,</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">
              Este es un recordatorio para el reporte <strong>{{reportName}}</strong> correspondiente al periodo <strong>{{periodName}}</strong>.
            </p>
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f9fafb;margin:0 0 24px;">
              <tr>
                <td style="padding:12px 16px;font-size:13px;color:#4b5563;"><strong>Fecha de vencimiento:</strong> {{dueDate:dd/MM/yyyy}}{{branchRow}}</td>
              </tr>
              <tr>
                <td style="padding:0 16px 12px;font-size:13px;color:#4b5563;"><strong>{{(isOverdue ? "Dias de atraso" : "Dias restantes")}}:</strong> {{daysRemaining}}</td>
              </tr>
            </table>
            <p style="color:#6b7280;font-size:13px;margin:0 0 28px;">Por favor asegurese de gestionar este reporte antes de la fecha limite.</p>
            <a href="{{frontendUrl}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Ir a la plataforma</a>
            """, backendUrl);
    }

    internal static string ReportAlertNotification(
        string userName,
        string reportName,
        string periodName,
        DateOnly dueDate,
        string alertType,
        string alertMessage,
        string frontendUrl,
        string backendUrl,
        Guid instanceId,
        Guid notificationId,
        string? branchName = null
    )
    {
        var (color, label) = alertType switch
        {
            "Preventiva" => ("#16a34a", "Preventiva"),
            "Seguimiento" => ("#d97706", "Seguimiento"),
            "Riesgo" => ("#ea580c", "Riesgo"),
            "Critica" => ("#dc2626", "Critica"),
            _ => ("#6b7280", alertType),
        };

        var branchRow = string.IsNullOrWhiteSpace(branchName)
            ? ""
            : $"<span style=\"color:#6b7280;font-size:13px;\"> | Sede: {branchName}</span>";

        var instanceUrl = $"{frontendUrl}/report-instances/{instanceId}";
        var pixelUrl = $"{backendUrl}/api/notifications/{notificationId}/mark-as-read";

        return EmailLayout.Wrap($$"""
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 16px;">
              <tr>
                <td style="background-color:{{color}};padding:4px 12px;border-radius:4px;">
                  <p style="margin:0;color:#ffffff;font-size:11px;font-weight:600;letter-spacing:0.5px;">{{label}}</p>
                </td>
              </tr>
            </table>
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:18px;">Alerta de Reporte</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hola <strong>{{userName}}</strong>,</p>
            <div style="background-color:#f9fafb;padding:14px 16px;margin:0 0 20px;">
              <p style="margin:0;font-size:14px;color:#4b5563;line-height:1.6;">{{alertMessage}}</p>
            </div>
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f9fafb;margin:0 0 24px;">
              <tr>
                <td style="padding:12px 16px;font-size:13px;color:#4b5563;"><strong>Reporte:</strong> {{reportName}}</td>
              </tr>
              <tr>
                <td style="padding:0 16px;font-size:13px;color:#4b5563;"><strong>Periodo:</strong> {{periodName}}</td>
              </tr>
              <tr>
                <td style="padding:0 16px 12px;font-size:13px;color:#4b5563;"><strong>Vencimiento:</strong> {{dueDate:dd/MM/yyyy}}{{branchRow}}</td>
              </tr>
            </table>
            <a href="{{instanceUrl}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:11px 38px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Ver reporte en SICRE</a>
            <img src="{{pixelUrl}}" width="1" height="1" alt="" style="display:none;" />
            """, backendUrl);
    }

    internal static string ReportsAssigned(
        ReportsAssignedEmailDto data,
        Guid notificationId,
        string frontendUrl,
        string backendUrl
    )
    {
        var rows = string.Join(
            "",
            data.Instances.Select(i =>
            {
                var (color, label) = i.Status switch
                {
                    ReportStatus.Pending => ("#d97706", "Pendiente"),
                    ReportStatus.Overdue => ("#dc2626", "Vencido"),
                    _ => ("#6b7280", "Desconocido"),
                };
                return $"""
                <tr>
                    <td style="padding:10px 8px;font-size:13px;color:#374151;border-bottom:1px solid #f3f4f6;">{i.PeriodName}</td>
                    <td style="padding:10px 8px;font-size:13px;color:#374151;text-align:center;border-bottom:1px solid #f3f4f6;">{i.DueDate:dd/MM/yyyy}</td>
                    <td style="padding:10px 8px;text-align:center;border-bottom:1px solid #f3f4f6;">
                        <span style="display:inline-block;background-color:{color};color:#ffffff;padding:3px 10px;border-radius:4px;font-size:11px;font-weight:600;">{label}</span>
                    </td>
                </tr>
                """;
            })
        );

        var branchSection = string.IsNullOrWhiteSpace(data.BranchName)
            ? ""
            : $"<p style=\"color:#374151;font-size:13px;margin:0 0 4px;\"><strong>Sede:</strong> {data.BranchName}</p>";

        return EmailLayout.Wrap($$"""
            <h2 style="color:#111827;margin:0 0 20px;font-weight:600;font-size:18px;">Reportes Asignados</h2>
            <p style="color:#4b5563;font-size:14px;margin:0 0 16px;line-height:1.6;">Hola <strong>{{data.UserName}}</strong>,</p>
            <p style="color:#4b5563;font-size:14px;margin:0 0 20px;line-height:1.6;">
              Te han asignado <strong>{{data.TotalReports}}</strong> reporte(s) con <strong>{{data.TotalInstances}}</strong> obligacion(es) como <strong>{{data.Role}}</strong>.
            </p>
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f9fafb;margin:0 0 16px;">
              <tr>
                <td style="padding:12px 16px;font-size:13px;color:#374151;"><strong>Entidad:</strong> {{data.ControlEntityAbbreviation}} - {{data.ControlEntityName}}</td>
              </tr>
              <tr>
                <td style="padding:0 16px 12px;font-size:13px;color:#374151;"><strong>Reporte:</strong> {{data.ReportCode}} - {{data.ReportName}}</td>
              </tr>
            </table>
            {{branchSection}}
            <p style="color:#374151;font-size:13px;margin:0 0 8px;font-weight:500;">Obligaciones:</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
              <thead>
                <tr style="background-color:#f9fafb;">
                  <th style="padding:10px 8px;text-align:left;font-size:12px;color:#6b7280;font-weight:500;">Periodo</th>
                  <th style="padding:10px 8px;text-align:center;font-size:12px;color:#6b7280;font-weight:500;">Vencimiento</th>
                  <th style="padding:10px 8px;text-align:center;font-size:12px;color:#6b7280;font-weight:500;">Estado</th>
                </tr>
              </thead>
              <tbody>{{rows}}</tbody>
            </table>
            <p style="color:#6b7280;font-size:13px;margin:0 0 20px;">Para mas informacion, accede a la plataforma:</p>
            <a href="{{frontendUrl}}" style="background:#1d3e81;color:#ffffff;text-decoration:none;padding:10px 30px;border-radius:6px;font-weight:600;display:inline-block;font-size:14px;">Acceder a SICRE</a>
            <img src="{{backendUrl}}/api/notification/{{notificationId}}/mark-as-read" width="1" height="1" alt="" style="display:none;" />
            """, backendUrl);
    }
}
