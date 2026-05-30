using Microsoft.Extensions.Options;
using Sicre.Api.Config;
using Sicre.Api.Features.Auth.Dtos;
using Sicre.Api.Features.Reports.Dtos;
using Sicre.Api.Shared.Email.Templates;
using MonthlyReportTpl = Sicre.Api.Shared.Email.Templates.MonthlyReportEmailTemplate;

namespace Sicre.Api.Shared.Email;

public class EmailTemplateService(IOptions<AppSettings> options) : IEmailTemplateService
{
    private readonly AppSettings _settings = options.Value;

    public string GetInvitationEmailTemplate(string fullName, string temporaryPassword) =>
        AuthEmailTemplates.Invitation(fullName, temporaryPassword, _settings.FrontendUrl);

    public string GetPasswordResetEmailTemplate(string fullName, string resetLink) =>
        AuthEmailTemplates.PasswordReset(fullName, resetLink);

    public string GetLoginNotificationEmailTemplate(LoginNotificationEmailDto data) =>
        AuthEmailTemplates.LoginNotification(data);

    public string GetEmailChangedNotificationTemplate(string fullName, string newEmail) =>
        AuthEmailTemplates.EmailChanged(fullName, newEmail, _settings.FrontendUrl);

    public string GetReportAlertEmailTemplate(
        string userName,
        string reportName,
        string periodName,
        DateOnly dueDate,
        string alertType,
        int daysRemaining,
        bool isOverdue,
        string? branchName = null
    ) =>
        ReportEmailTemplates.ReportAlert(
            userName,
            reportName,
            periodName,
            dueDate,
            alertType,
            daysRemaining,
            isOverdue,
            _settings.FrontendUrl,
            branchName
        );

    public string GetReportAlertNotificationEmailTemplate(
        string userName,
        string reportName,
        string periodName,
        DateOnly dueDate,
        string alertType,
        string alertMessage,
        Guid instanceId,
        Guid notificationId,
        string? branchName = null
    ) =>
        ReportEmailTemplates.ReportAlertNotification(
            userName,
            reportName,
            periodName,
            dueDate,
            alertType,
            alertMessage,
            _settings.FrontendUrl,
            _settings.BackendUrl,
            instanceId,
            notificationId,
            branchName
        );

    public string GetReportsAssignedEmailTemplate(
        ReportsAssignedEmailDto data,
        Guid notificationId
    ) =>
        ReportEmailTemplates.ReportsAssigned(
            data,
            notificationId,
            _settings.FrontendUrl,
            _settings.BackendUrl
        );

    public string GetMonthlyStatusEmailTemplate(string userName, string monthName) =>
        NotificationEmailTemplates.MonthlyStatus(userName, monthName, _settings.FrontendUrl);

    public string GetInstanceEventEmailTemplate(
        string userName,
        string eventType,
        string title,
        string content,
        Guid instanceId
    ) =>
        NotificationEmailTemplates.InstanceEvent(
            userName,
            eventType,
            title,
            content,
            instanceId,
            _settings.FrontendUrl
        );

    public string GetMonthlyReportEmailTemplate(string periodLabel) =>
        MonthlyReportTpl.Build(periodLabel, _settings.FrontendUrl);
}
