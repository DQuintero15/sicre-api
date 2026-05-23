using Microsoft.AspNetCore.Identity;

namespace Sicre.Api.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }
    public Guid? ProcessId { get; set; }
    public Process? Process { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool HasChangedDefaultPassword { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
