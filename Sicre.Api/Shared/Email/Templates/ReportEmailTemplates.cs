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
        string? branchName = null
    )
    {
        var color = alertType switch
        {
            "Alerta Temprana" => "#17a2b8",
            "Seguimiento" => "#ffc107",
            "Crítica" => "#dc3545",
            _ => "#6c757d",
        };

        var branchRow = string.IsNullOrWhiteSpace(branchName)
            ? ""
            : $"<p style='margin:5px 0;font-size:14px;'><strong>Sede:</strong> {branchName}</p>";

        return $"""
            <html lang='es'>
            <body style='margin:0;padding:0;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;background-color:#f5f5f5;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
                    <tr><td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#fff;border-radius:10px;box-shadow:0 3px 6px rgba(0,0,0,0.1);'>
                            <tr><td style='text-align:center;padding:30px;'>
                                <h2 style='color:#1d1d1d;margin:0 0 10px;font-weight:600;'>Recordatorio de Reporte</h2>
                                <span style='background-color:{color};color:#fff;padding:5px 10px;border-radius:15px;font-size:12px;font-weight:bold;'>{alertType}</span>
                            </td></tr>
                            <tr><td style='padding:30px;'>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>Hola <strong>{userName}</strong>,</p>
                                <p style='color:#333;font-size:15px;margin-bottom:20px;'>
                                    Este es un recordatorio para el reporte <strong>{reportName}</strong> correspondiente al periodo <strong>{periodName}</strong>.
                                </p>
                                <div style='background-color:#f8f9fa;border-left:4px solid {color};padding:15px;margin-bottom:25px;'>
                                    {branchRow}
                                    <p style='margin:5px 0;font-size:14px;'><strong>Fecha de Vencimiento:</strong> {dueDate:dd/MM/yyyy}</p>
                                    <p style='margin:5px 0;font-size:14px;'><strong>{(
                isOverdue ? "Días de atraso" : "Días Restantes"
            )}:</strong> {daysRemaining}</p>
                                </div>
                                <p style='color:#555;font-size:14px;margin-bottom:30px;'>Por favor asegúrese de gestionar este reporte antes de la fecha límite.</p>
                                <div style='text-align:center;'>
                                    <a href='{frontendUrl}' style='background:#1d3e81;color:#fff;text-decoration:none;padding:12px 40px;border-radius:6px;font-weight:600;display:inline-block;'>Ir a la plataforma</a>
                                </div>
                            </td></tr>
                            <tr><td style='background-color:#f5f5f5;padding:20px;text-align:center;font-size:12px;color:#888;'>© 2025 Grupo del Llano. Todos los derechos reservados.</td></tr>
                        </table>
                    </td></tr>
                </table>
            </body>
            </html>
            """;
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
                    ReportStatus.Pending => ("#f1c40f", "Pendiente"),
                    ReportStatus.Overdue => ("#c10015", "Vencido"),
                    _ => ("#6C757D", "Desconocido"),
                };
                return $"""
                <tr style='border-bottom:1px solid #e0e0e0;'>
                    <td style='padding:12px;font-size:13px;color:#333;'>{i.PeriodName}</td>
                    <td style='padding:12px;font-size:13px;color:#333;text-align:center;'>{i.DueDate:dd/MM/yyyy}</td>
                    <td style='padding:12px;font-size:13px;color:#333;text-align:center;'>{i.PeriodStart:dd/MM/yyyy} - {i.PeriodEnd:dd/MM/yyyy}</td>
                    <td style='padding:12px;text-align:center;'>
                        <span style='display:inline-block;padding:6px 12px;background-color:{color};color:#fff;border-radius:12px;font-size:12px;font-weight:600;'>{label}</span>
                    </td>
                </tr>
                """;
            })
        );

        var branchSection = string.IsNullOrWhiteSpace(data.BranchName)
            ? ""
            : $"<p style='color:#333;font-size:13px;margin:0 0 5px;font-weight:600;'>Sede:</p><p style='color:#333;font-size:14px;margin:0 0 25px;'>{data.BranchName}</p>";

        return $"""
            <html lang='es'>
            <body style='margin:0;padding:0;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;background-color:#f5f5f5;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
                    <tr><td align='center'>
                        <table width='650' cellpadding='0' cellspacing='0' style='background-color:#fff;border-radius:8px;'>
                            <tr><td style='text-align:center;padding:30px;'>
                                <h2 style='color:#1d1d1d;margin:0 0 20px;font-weight:600;font-size:22px;'>Reportes Asignados</h2>
                            </td></tr>
                            <tr><td style='padding:30px;'>
                                <p style='color:#333;font-size:15px;margin:0 0 15px;'>Hola <strong>{data.UserName}</strong>,</p>
                                <p style='color:#555;font-size:14px;margin:0 0 20px;'>
                                    Te han asignado <strong>{data.TotalReports}</strong> reporte(s) con <strong>{data.TotalInstances}</strong> obligación(es) como <strong>{data.Role}</strong>.
                                </p>
                                <p style='color:#333;font-size:13px;margin:15px 0 5px;font-weight:600;'>Entidad de Control:</p>
                                <p style='color:#333;font-size:14px;margin:0 0 15px;'>{data.ControlEntityAbbreviation} - {data.ControlEntityName}</p>
                                <p style='color:#333;font-size:13px;margin:15px 0 5px;font-weight:600;'>Reporte:</p>
                                <p style='color:#333;font-size:14px;margin:0 0 5px;'><strong>{data.ReportCode}</strong> - {data.ReportName}</p>
                                {branchSection}
                                <p style='color:#333;font-size:14px;margin:0 0 15px;font-weight:600;'>Obligaciones:</p>
                                <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                                    <thead>
                                        <tr style='background-color:#f5f5f5;border-bottom:2px solid #ddd;'>
                                            <th style='padding:12px;text-align:left;font-size:13px;color:#333;font-weight:600;'>Período</th>
                                            <th style='padding:12px;text-align:center;font-size:13px;color:#333;font-weight:600;'>Vencimiento</th>
                                            <th style='padding:12px;text-align:center;font-size:13px;color:#333;font-weight:600;'>Rango Período</th>
                                            <th style='padding:12px;text-align:center;font-size:13px;color:#333;font-weight:600;'>Estado</th>
                                        </tr>
                                    </thead>
                                    <tbody>{rows}</tbody>
                                </table>
                                <p style='color:#555;font-size:12px;margin:25px 0 20px;text-align:center;'>Para más información, accede a la plataforma:</p>
                                <div style='text-align:center;'>
                                    <a href='{frontendUrl}' style='background:#333;color:#fff;text-decoration:none;padding:10px 30px;border-radius:4px;font-weight:600;display:inline-block;font-size:14px;'>Acceder a SICRE</a>
                                </div>
                            </td></tr>
                            <tr><td style='background-color:#f5f5f5;padding:20px;text-align:center;font-size:12px;color:#888;border-top:1px solid #e0e0e0;'>© 2025 Grupo del Llano. Todos los derechos reservados.</td></tr>
                        </table>
                    </td></tr>
                </table>
                <img src='{backendUrl}/api/notification/{notificationId}/mark-as-read' width='1' height='1' alt='' style='display:none;' />
            </body>
            </html>
            """;
    }
}
