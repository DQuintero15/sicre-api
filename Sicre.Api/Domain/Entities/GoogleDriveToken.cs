namespace Sicre.Api.Domain.Entities;

public class GoogleDriveToken
{
    public Guid Id { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
