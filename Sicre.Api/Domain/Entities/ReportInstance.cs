using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Domain.Entities;

public class ReportInstance
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Report? Report { get; set; }
    public int PeriodYear { get; set; }
    public int? PeriodMonth { get; set; }
    public required string PeriodName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? EventDate { get; set; }
    public DateTime? SentDate { get; set; }
    public ReportStatus Status { get; set; }
    public string? DelayReason { get; set; }
    public string? Observations { get; set; }
    public string? ManualActivationReason { get; set; }
    public string? DueDateOverrideReason { get; set; }
    public Guid ResponsibleUserId { get; set; }
    public User? ResponsibleUser { get; set; }
    public Guid SupervisorUserId { get; set; }
    public User? SupervisorUser { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public Guid? ActivatedByUserId { get; set; }
    public User? ActivatedByUser { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<ReportAttachment> Attachments { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<ReportReversion> Reversions { get; set; } = [];
}
