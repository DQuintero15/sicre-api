namespace Sicre.Api.Domain.Entities;

public class SICRESettings
{
    public Guid Id { get; set; }
    public DateOnly? GoLiveDate { get; set; }
    public bool AutoNotify { get; set; } = true;
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
