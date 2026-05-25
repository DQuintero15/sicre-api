namespace Sicre.Api.Features.GoogleDrive.Dtos;

public class GoogleDriveStatusDto
{
    public bool IsConnected { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
