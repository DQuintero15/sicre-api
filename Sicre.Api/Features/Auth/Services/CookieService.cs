using Sicre.Api.Shared;

namespace Sicre.Api.Features.Auth.Services;

public interface ICookieService
{
    void SetRefreshTokenCookie(
        HttpResponse response,
        HttpRequest request,
        string token,
        DateTime expiration
    );
    bool TryGetRefreshTokenCookie(HttpRequest request, out string? token);
    void RemoveRefreshTokenCookie(HttpResponse response);
}

public class CookieService : ICookieService
{
    public void SetRefreshTokenCookie(
        HttpResponse response,
        HttpRequest request,
        string token,
        DateTime expiration
    )
    {
        response.Cookies.Append(
            Constants.CookieNames.RefreshToken,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = new DateTimeOffset(expiration),
            }
        );
    }

    public bool TryGetRefreshTokenCookie(HttpRequest request, out string? token) =>
        request.Cookies.TryGetValue(Constants.CookieNames.RefreshToken, out token);

    public void RemoveRefreshTokenCookie(HttpResponse response) =>
        response.Cookies.Delete(Constants.CookieNames.RefreshToken);
}
