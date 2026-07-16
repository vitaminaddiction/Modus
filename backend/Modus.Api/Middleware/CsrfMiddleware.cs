using Modus.Api.Auth;

namespace Modus.Api.Middleware;

/// <summary>
/// CSRF double-submit 검증. 상태변경 메서드(POST/PUT/PATCH/DELETE)에서
/// XSRF-TOKEN 쿠키 == X-XSRF-TOKEN 헤더인지 확인.
/// GET류·Bearer(Authorization 헤더) 클라이언트·예외경로는 스킵.
/// </summary>
public sealed class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>CSRF 검증 예외경로(로그인 등 아직 XSRF 쿠키가 없는 진입점). 필요 시 확장.</summary>
    private static readonly HashSet<string> Exempt = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/web/auth/login",
    };

    public CsrfMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        if (RequiresCheck(ctx))
        {
            var cookie = ctx.Request.Cookies[AuthCookies.Xsrf];
            var header = ctx.Request.Headers["X-XSRF-TOKEN"].ToString();
            if (string.IsNullOrEmpty(cookie) || !string.Equals(cookie, header, StringComparison.Ordinal))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsync("CSRF token invalid.");
                return;
            }
        }

        await _next(ctx);
    }

    private bool RequiresCheck(HttpContext ctx)
    {
        var m = ctx.Request.Method;
        if (HttpMethods.IsGet(m) || HttpMethods.IsHead(m) || HttpMethods.IsOptions(m) || HttpMethods.IsTrace(m))
            return false;

        if (ctx.Request.Headers.ContainsKey("Authorization"))   // Bearer 클라이언트는 CSRF 무관
            return false;

        var path = ctx.Request.Path.Value ?? string.Empty;
        return !Exempt.Contains(path);
    }
}
