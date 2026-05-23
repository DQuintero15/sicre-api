namespace Sicre.Api.Shared;

public interface IDateHelper
{
    DateTime GetCurrentDateTime();
    DateOnly GetCurrentDate();
}

public class DateHelper : IDateHelper
{
    private static readonly TimeZoneInfo ColombiaZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Bogota"
    );

    public DateTime GetCurrentDateTime() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaZone);

    public DateOnly GetCurrentDate() => DateOnly.FromDateTime(GetCurrentDateTime());
}
