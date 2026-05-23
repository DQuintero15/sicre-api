namespace Sicre.Api.Shared;

public static class Constants
{
    public static class ClaimNames
    {
        public const string TokenType = "token_type";
    }

    public static class TokenTypes
    {
        public const string Temporary = "temporary";
        public const string AccessToken = "access_token";
    }

    public static class CookieNames
    {
        public const string RefreshToken = "lgas__rf__key";
    }
}
