using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportInstance> ReportInstances => Set<ReportInstance>();
    public DbSet<ReportReversion> ReportReversions => Set<ReportReversion>();
    public DbSet<ReportAttachment> ReportAttachments => Set<ReportAttachment>();
    public DbSet<ReportInstanceAuditEntry> ReportInstanceAuditEntries => Set<ReportInstanceAuditEntry>();
    public DbSet<ReportInstanceNote> ReportInstanceNotes => Set<ReportInstanceNote>();
    public DbSet<ControlEntity> ControlEntities => Set<ControlEntity>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<GoogleDriveToken> GoogleDriveTokens => Set<GoogleDriveToken>();
    public DbSet<SICRESettings> SICRESettings => Set<SICRESettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
