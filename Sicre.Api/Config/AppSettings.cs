namespace Sicre.Api.Config;

public class AppSettings
{
    public JwtSettings Jwt { get; set; } = new();
    public SmtpSettings Smtp { get; set; } = new();
    public CorsSettings Cors { get; set; } = new();
    public GoogleDriveSettings GoogleDrive { get; set; } = new();
    public SeedSettings Seed { get; set; } = new();
    public FeatureFlags Features { get; set; } = new();
    public string FrontendUrl { get; set; } = string.Empty;
    public string BackendUrl { get; set; } = string.Empty;
}

public class FeatureFlags
{
    public bool GlobalAudit { get; set; } = true;
    public bool BulkDeliver { get; set; } = false;
    public bool Notes { get; set; } = false;
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int TemporaryTokenExpirationMinutes { get; set; } = 30;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string? RedirectTo { get; set; }
}

public class CorsSettings
{
    public List<string> AllowedOrigins { get; set; } = [];
    public List<string> AllowedMethods { get; set; } = [];
    public List<string> AllowedHeaders { get; set; } = [];
    public bool AllowCredentials { get; set; } = true;
}

public class GoogleDriveSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = "SICRE";
    public string FolderId { get; set; } = string.Empty;
}

public class SeedSettings
{
    public string Email { get; set; } = string.Empty;
    public string DefaultUserPassword { get; set; } = string.Empty;
}
