using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Shared;

public static class ReportStatusHelper
{
    public static ReportStatus Compute(ReportStatus dbStatus, DateOnly dueDate, int alertCriticalDays)
    {
        if (dbStatus != ReportStatus.Pending)
            return dbStatus;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = (
            dueDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)
        ).Days;

        if (days < 0) return ReportStatus.Overdue;
        if (days <= alertCriticalDays) return ReportStatus.UpcomingDue;
        return dbStatus;
    }
}
