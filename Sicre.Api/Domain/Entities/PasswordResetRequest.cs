namespace Sicre.Api.Domain.Entities;

public class PasswordResetRequest
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string TokenHash { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
