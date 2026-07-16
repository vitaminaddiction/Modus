namespace Modus.Api.Auth;

/// <summary>웹 인증 쿠키: access_token(HttpOnly) + XSRF-TOKEN(비HttpOnly, double-submit).</summary>
public static class AuthCookies
{
    public const string AccessToken = "access_token";
    public const string Xsrf = "XSRF-TOKEN";

    public static void Issue(HttpResponse res, string token, DateTime expires)
    {
        var secure = res.HttpContext.Request.IsHttps;  // dev(http)에선 false, 운영(https)에선 true

        res.Cookies.Append(AccessToken, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires,
        });

        // JS가 읽어 X-XSRF-TOKEN 헤더로 재전송 → double-submit 검증
        res.Cookies.Append(Xsrf, Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires,
        });
    }

    public static void Clear(HttpResponse res)
    {
        res.Cookies.Delete(AccessToken, new CookieOptions { Path = "/" });
        res.Cookies.Delete(Xsrf, new CookieOptions { Path = "/" });
    }
}
