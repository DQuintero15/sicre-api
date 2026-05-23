using System.Net;
using System.Text.RegularExpressions;
using Sicre.Api.Features.Auth.Dtos;

namespace Sicre.Api.Features.Auth.Services;

public interface ILoginNotificationService
{
    LoginNotificationEmailDto ExtractLoginInfo(HttpRequest request, string userName, string email);
}

public class LoginNotificationService(ILogger<LoginNotificationService> logger)
    : ILoginNotificationService
{
    public LoginNotificationEmailDto ExtractLoginInfo(
        HttpRequest request,
        string userName,
        string email
    )
    {
        var ip = ExtractIp(request);
        var ua = request.Headers.UserAgent.ToString();
        var (browser, os) = ParseUserAgent(ua);

        return new LoginNotificationEmailDto
        {
            UserName = userName,
            Email = email,
            IpAddress = ip,
            UserAgent = ua,
            Browser = browser,
            OperatingSystem = os,
            LoginTime = DateTime.UtcNow,
            IsNewLogin = true,
        };
    }

    private string ExtractIp(HttpRequest request)
    {
        try
        {
            if (request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                var first = forwarded.ToString().Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(first) && IPAddress.TryParse(first, out _))
                    return first;
            }

            if (
                request.Headers.TryGetValue("X-Real-IP", out var realIp)
                && IPAddress.TryParse(realIp.ToString(), out _)
            )
                return realIp.ToString();

            return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extrayendo IP");
            return "Desconocida";
        }
    }

    private (string browser, string os) ParseUserAgent(string ua)
    {
        if (string.IsNullOrEmpty(ua))
            return ("Desconocido", "Desconocido");

        try
        {
            return (ExtractBrowser(ua), ExtractOs(ua));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parseando user agent");
            return ("Desconocido", "Desconocido");
        }
    }

    private static string ExtractBrowser(string ua)
    {
        if (ua.Contains("Edg"))
        {
            var m = Regex.Match(ua, @"Edg[e]?/([\d.]+)");
            return m.Success ? $"Edge {m.Groups[1].Value}" : "Edge";
        }
        if (ua.Contains("Chrome"))
        {
            var m = Regex.Match(ua, @"Chrome/([\d.]+)");
            return m.Success ? $"Chrome {m.Groups[1].Value}" : "Chrome";
        }
        if (ua.Contains("Firefox"))
        {
            var m = Regex.Match(ua, @"Firefox/([\d.]+)");
            return m.Success ? $"Firefox {m.Groups[1].Value}" : "Firefox";
        }
        if (ua.Contains("Safari") && !ua.Contains("Chrome"))
        {
            var m = Regex.Match(ua, @"Safari/([\d.]+)");
            return m.Success ? $"Safari {m.Groups[1].Value}" : "Safari";
        }
        return "Navegador desconocido";
    }

    private static string ExtractOs(string ua)
    {
        if (ua.Contains("iPhone"))
            return "iOS (iPhone)";
        if (ua.Contains("iPad"))
            return "iOS (iPad)";
        if (ua.Contains("Android"))
        {
            var m = Regex.Match(ua, @"Android ([\d.]+)");
            return m.Success ? $"Android {m.Groups[1].Value}" : "Android";
        }
        if (ua.Contains("Windows NT 10.0"))
            return "Windows 10/11";
        if (ua.Contains("Windows NT"))
            return "Windows";
        if (ua.Contains("Mac OS X"))
        {
            var m = Regex.Match(ua, @"Mac OS X ([\d_.]+)");
            return m.Success ? $"macOS {m.Groups[1].Value.Replace('_', '.')}" : "macOS";
        }
        if (ua.Contains("Linux"))
            return "Linux";
        return "Sistema desconocido";
    }
}
