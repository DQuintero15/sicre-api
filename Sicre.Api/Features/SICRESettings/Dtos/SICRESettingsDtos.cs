namespace Sicre.Api.Features.SICRESettings.Dtos;

public class UpdateSICRESettingsRequest
{
    public DateOnly? GoLiveDate { get; set; }
    public bool AutoNotify { get; set; }
}

public class SICRESettingsResponse
{
    public DateOnly? GoLiveDate { get; set; }
    public bool AutoNotify { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByUserName { get; set; }
}
