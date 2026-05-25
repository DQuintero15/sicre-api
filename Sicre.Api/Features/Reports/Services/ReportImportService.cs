using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Reports.Dtos.Requests;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;
using SICRESettingsEntity = Sicre.Api.Domain.Entities.SICRESettings;

namespace Sicre.Api.Features.Reports.Services;

public interface IReportImportService
{
    Task<bool> ImportAsync(
        ReportImportRequest request,
        Guid importedByUserId,
        CancellationToken ct = default
    );
}

public class ReportImportService(
    ApplicationDbContext db,
    IReportInstanceGenerator generator,
    IDateHelper dateHelper,
    ILogger<ReportImportService> logger
) : IReportImportService
{
    public async Task<bool> ImportAsync(
        ReportImportRequest request,
        Guid importedByUserId,
        CancellationToken ct = default
    )
    {
        var importId = Guid.NewGuid();

        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["ImportId"] = importId,
                ["ImportSource"] = request.SourceFile ?? "unknown",
                ["ImportedBy"] = importedByUserId,
                ["Feature"] = "ReportImport",
            }
        );

        logger.LogInformation(
            "[ReportImport] Started — importId={ImportId}, source={Source}, reports={Count}, generateInstances={Generate}",
            importId,
            request.SourceFile,
            request.Reports.Count,
            request.GenerateInitialInstances
        );

        try
        {
            var userMap = await LoadUserMapAsync(ct);
            var branchMap = await LoadBranchMapAsync(ct);
            var controlEntities = await db.ControlEntities.ToListAsync(ct);
            var settings = await db.SICRESettings.FirstOrDefaultAsync(ct);
            var processCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            var succeeded = 0;
            var failed = 0;

            foreach (var item in request.Reports)
            {
                try
                {
                    await ProcessItemAsync(
                        item,
                        request.GenerateInitialInstances,
                        settings,
                        importedByUserId,
                        userMap,
                        branchMap,
                        controlEntities,
                        processCache,
                        ct
                    );

                    succeeded++;
                    logger.LogInformation(
                        "[ReportImport] Row {Row}: [{Code}] OK",
                        item.SourceRowNumber,
                        item.Code
                    );
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogError(
                        ex,
                        "[ReportImport] Row {Row}: [{Code}] FAILED — {ErrorMessage}",
                        item.SourceRowNumber,
                        item.Code,
                        ex.Message
                    );
                }
            }

            logger.LogInformation(
                "[ReportImport] Finished — importId={ImportId}, succeeded={Succeeded}, failed={Failed}",
                importId,
                succeeded,
                failed
            );

            return failed == 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ReportImport] Aborted — importId={ImportId}, critical error",
                importId
            );
            return false;
        }
    }

    // ── Row processing ──────────────────────────────────────────────────────────

    private async Task ProcessItemAsync(
        ImportReportItem item,
        bool generateInstances,
        SICRESettingsEntity? settings,
        Guid importedByUserId,
        Dictionary<string, Guid> userMap,
        Dictionary<string, Guid> branchMap,
        List<ControlEntity> controlEntities,
        Dictionary<string, Guid> processCache,
        CancellationToken ct
    )
    {
        var frequency = ParseEnum<ReportFrequency>(item.Frequency, item.SourceRowNumber);
        var generationMode = ParseEnum<ReportGenerationMode>(
            item.GenerationMode,
            item.SourceRowNumber
        );
        var dueDateRuleType = ParseEnum<ReportDueDateRuleType>(
            item.DueDateRuleType,
            item.SourceRowNumber
        );

        var controlEntity = await ResolveControlEntityAsync(item, controlEntities, ct);
        var branchId = ResolveBranchId(item.BranchName, branchMap, item.SourceRowNumber);
        var processId = await ResolveProcessIdAsync(item.ProcessName, processCache, ct);

        var senderUserId = ResolveRequiredUser(
            item.SenderResponsibleEmail,
            userMap,
            item.SourceRowNumber,
            "senderResponsible"
        );
        var uploaderUserId = ResolveRequiredUser(
            item.EntityUploadResponsibleEmail,
            userMap,
            item.SourceRowNumber,
            "entityUploadResponsible"
        );
        var leaderUserId = ResolveRequiredUser(
            item.FollowUpLeaderEmail,
            userMap,
            item.SourceRowNumber,
            "followUpLeader"
        );

        var existing = await db.Reports.FirstOrDefaultAsync(
            r =>
                r.Code == item.Code
                && r.ControlEntityId == controlEntity.Id
                && r.BranchId == branchId,
            ct
        );

        var isNew = existing is null;
        var report = existing ?? new Report { Code = item.Code, Name = item.Name };

        report.Name = item.Name;
        report.ControlEntityId = controlEntity.Id;
        report.BranchId = branchId;
        report.ProcessId = processId;
        report.LegalBasis = item.LegalBasis;
        report.Frequency = frequency;
        report.GenerationMode = generationMode;
        report.DueDateRuleType = dueDateRuleType;
        report.DueDateDay = item.DueDateDay;
        report.DueDateMonth = item.DueDateMonth;
        report.DueDateDatesDefinition = SerializeDueDateDates(item.DueDateDates);
        report.OriginalDueDateText = item.OriginalDueDateText;
        report.AlertEarlyDays = item.AlertEarlyDays ?? 15;
        report.AlertFollowUpDays = item.AlertFollowUpDays ?? 5;
        report.AlertCriticalDays = item.AlertCriticalDays ?? 0;
        report.FormatTypes = MapFormatTypes(item.FormatTypes);
        report.InstructionsUrl = item.InstructionsUrl;
        report.TemplateFileUrl = item.TemplateFileUrl;
        report.NotificationEmails = SerializeNotificationEmails(item.NotificationEmails);
        report.StartDate = DateOnly.Parse(item.StartDate);
        report.EndDate = string.IsNullOrWhiteSpace(item.EndDate)
            ? null
            : DateOnly.Parse(item.EndDate);
        report.SenderResponsibleUserId = senderUserId;
        report.EntityUploadResponsibleUserId = uploaderUserId;
        report.FollowUpLeaderUserId = leaderUserId;
        report.IsActive = item.IsActive;

        if (isNew)
        {
            report.CreatedByUserId = importedByUserId;
            report.CreatedAt = DateTime.UtcNow;
            db.Reports.Add(report);
            logger.LogDebug(
                "Row {Row}: creating new report [{Code}]",
                item.SourceRowNumber,
                item.Code
            );
        }
        else
        {
            report.UpdatedByUserId = importedByUserId;
            report.UpdatedAt = DateTime.UtcNow;
            logger.LogDebug(
                "Row {Row}: updating existing report [{Code}]",
                item.SourceRowNumber,
                item.Code
            );
        }

        await db.SaveChangesAsync(ct);

        if (generateInstances && IsAutoGenerable(report) && settings?.GoLiveDate is not null)
            await GenerateInitialInstancesAsync(
                report,
                settings.GoLiveDate.Value,
                importedByUserId,
                ct
            );
    }

    // ── Entity resolution ───────────────────────────────────────────────────────

    private async Task<ControlEntity> ResolveControlEntityAsync(
        ImportReportItem item,
        List<ControlEntity> controlEntities,
        CancellationToken ct
    )
    {
        if (!string.IsNullOrWhiteSpace(item.ControlEntityNit))
        {
            var normalizedNit = NormalizeNit(item.ControlEntityNit);
            var byNit = controlEntities.FirstOrDefault(e => NormalizeNit(e.Nit) == normalizedNit);
            if (byNit is not null)
                return byNit;
        }

        var byName = controlEntities.FirstOrDefault(e =>
            string.Equals(e.Name, item.ControlEntityName, StringComparison.OrdinalIgnoreCase)
        );
        if (byName is not null)
            return byName;

        var created = new ControlEntity
        {
            Name = item.ControlEntityName,
            Nit = NormalizeNit(item.ControlEntityNit),
            Website = item.ControlEntityWebsite,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.ControlEntities.Add(created);
        await db.SaveChangesAsync(ct);
        controlEntities.Add(created);

        logger.LogInformation("Created control entity: [{Name}]", created.Name);
        return created;
    }

    private async Task<Guid?> ResolveProcessIdAsync(
        string? processName,
        Dictionary<string, Guid> cache,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;

        if (cache.TryGetValue(processName, out var cached))
            return cached;

        var process = await db.Processes.FirstOrDefaultAsync(
            p => p.Name.ToLower() == processName.ToLower(),
            ct
        );

        if (process is null)
        {
            process = new Process { Name = processName };
            db.Processes.Add(process);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Created process: [{Name}]", processName);
        }

        cache[processName] = process.Id;
        return process.Id;
    }

    private Guid? ResolveBranchId(string? branchName, Dictionary<string, Guid> branchMap, int row)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return null;

        var key = branchName.Trim().ToLowerInvariant();
        if (branchMap.TryGetValue(key, out var id))
            return id;

        logger.LogWarning(
            "Row {Row}: branch [{Branch}] not found — branchId set to null",
            row,
            branchName
        );
        return null;
    }

    private Guid ResolveRequiredUser(
        string? email,
        Dictionary<string, Guid> userMap,
        int row,
        string role
    )
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException($"Row {row}: no email provided for {role}");

        var key = email.Trim().ToLowerInvariant();
        if (userMap.TryGetValue(key, out var id))
            return id;

        throw new InvalidOperationException(
            $"Row {row}: user not found for {role} — email={email}"
        );
    }

    // ── Instance generation ─────────────────────────────────────────────────────

    private static bool IsAutoGenerable(Report report) =>
        report.GenerationMode == ReportGenerationMode.Automatic
        && report.Frequency != ReportFrequency.Eventual
        && report.DueDateRuleType != ReportDueDateRuleType.ManualDateRequired;

    private async Task GenerateInitialInstancesAsync(
        Report report,
        DateOnly goLiveDate,
        Guid createdByUserId,
        CancellationToken ct
    )
    {
        var today = dateHelper.GetCurrentDate();
        var candidates = generator.GetCandidatesInWindow(
            report,
            today,
            today.AddMonths(12),
            goLiveDate
        );

        var created = 0;
        foreach (var candidate in candidates)
        {
            var exists = await db.ReportInstances.AnyAsync(
                i =>
                    i.ReportId == report.Id
                    && i.PeriodYear == candidate.PeriodYear
                    && i.PeriodMonth == candidate.PeriodMonth,
                ct
            );

            if (exists)
                continue;

            var status = candidate.DueDate < today ? ReportStatus.Overdue : ReportStatus.Pending;
            db.ReportInstances.Add(
                new ReportInstance
                {
                    Id = Guid.NewGuid(),
                    ReportId = report.Id,
                    PeriodYear = candidate.PeriodYear,
                    PeriodMonth = candidate.PeriodMonth,
                    PeriodName = candidate.PeriodName,
                    PeriodStart = candidate.PeriodStart,
                    PeriodEnd = candidate.PeriodEnd,
                    DueDate = candidate.DueDate,
                    EventDate = candidate.EventDate,
                    Status = status,
                    ResponsibleUserId = report.EntityUploadResponsibleUserId,
                    SupervisorUserId = report.FollowUpLeaderUserId,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Report [{Code}]: generated {Count} instances",
                report.Code,
                created
            );
        }
    }

    // ── Lookup map loaders ──────────────────────────────────────────────────────

    private async Task<Dictionary<string, Guid>> LoadUserMapAsync(CancellationToken ct)
    {
        var users = await db
            .Users.Where(u => u.Email != null)
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(ct);

        return users.ToDictionary(u => u.Email!.ToLowerInvariant(), u => u.Id);
    }

    private async Task<Dictionary<string, Guid>> LoadBranchMapAsync(CancellationToken ct)
    {
        var branches = await db.Branches.ToListAsync(ct);
        return branches.ToDictionary(b => b.Name.ToLowerInvariant(), b => b.Id);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static string NormalizeNit(string? nit) =>
        (nit ?? "").Replace(".", "").Replace("-", "").Replace(" ", "").Trim();

    private T ParseEnum<T>(string value, int row)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        throw new InvalidOperationException(
            $"Row {row}: cannot parse '{value}' as {typeof(T).Name}"
        );
    }

    private static List<ReportFormatType> MapFormatTypes(List<string> formats)
    {
        if (formats.Count == 0)
            return [ReportFormatType.Any];

        var result = formats
            .Select(f =>
                f.ToLowerInvariant() switch
                {
                    "excel" or "xlsx" or "xls" or "spreadsheet" or "hoja de cálculo" =>
                        ReportFormatType.Spreadsheet,
                    "pdf" => ReportFormatType.PDF,
                    "zip" or "rar" or "archive" or "archivo comprimido" => ReportFormatType.Archive,
                    "web" or "webplatform" or "plataforma web" => ReportFormatType.WebPlatform,
                    "csv" or "json" or "xml" or "structureddata" or "datos estructurados" =>
                        ReportFormatType.StructuredData,
                    _ => ReportFormatType.Any,
                }
            )
            .Distinct()
            .ToList();

        return result;
    }

    private static string? SerializeNotificationEmails(string? rawEmails)
    {
        if (string.IsNullOrWhiteSpace(rawEmails))
            return null;

        var emails = rawEmails
            .Split(
                [';', ','],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        return emails.Count == 0 ? null : JsonSerializer.Serialize(emails);
    }

    private static string? SerializeDueDateDates(List<FixedDateEntry>? dates)
    {
        if (dates is null || dates.Count == 0)
            return null;

        return JsonSerializer.Serialize(dates.Select(d => new { month = d.Month, day = d.Day }));
    }
}
