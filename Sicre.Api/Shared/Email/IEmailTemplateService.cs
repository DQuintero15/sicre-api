using Sicre.Api.Features.Auth.Dtos;
using Sicre.Api.Features.Reports.Dtos;

namespace Sicre.Api.Shared.Email;

public interface IEmailTemplateService
{
    string GetInvitationEmailTemplate(string fullName, string temporaryPassword);
    string GetPasswordResetEmailTemplate(string fullName, string resetLink);
    string GetLoginNotificationEmailTemplate(LoginNotificationEmailDto data);
    string GetEmailChangedNotificationTemplate(string fullName, string newEmail);
    string GetReportAlertEmailTemplate(
        string userName,
        string reportName,
        string periodName,
        DateOnly dueDate,
        string alertType,
        int daysRemaining,
        bool isOverdue,
        string? branchName = null
    );
    string GetReportAlertNotificationEmailTemplate(
        string userName,
        string reportName,
        string periodName,
        DateOnly dueDate,
        string alertType,
        string alertMessage,
        string? branchName = null
    );
    string GetReportsAssignedEmailTemplate(ReportsAssignedEmailDto data, Guid notificationId);
    string GetMonthlyStatusEmailTemplate(string userName, string monthName);
}
