namespace Sicre.Api.Features.Auth.Dtos;

public class LoginNotificationEmailDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public bool IsNewLogin { get; set; }
}
