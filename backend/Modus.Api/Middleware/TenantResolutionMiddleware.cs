using Modus.Application.Multitenancy;
using Modus.Infrastructure.Multitenancy;

namespace Modus.Api.Middleware;

/// <summary>
/// 요청에서 테넌트를 해석·검증해 ITenantContext에 세팅.
/// 우선순위: X-Tenant-Code 헤더 → JWT 'tenant' 클레임 → 서브도메인.
/// - 코드가 있으나 카탈로그에 없으면 400.
/// - /api 요청인데 테넌트가 없으면 400 (이 API는 전면 멀티테넌트).
/// (JWT 클레임을 쓰려면 UseAuthentication 뒤에 배치할 것)
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, ITenantContext tenantContext, ITenantStore store)
    {
        var code = Resolve(ctx);

        if (!string.IsNullOrWhiteSpace(code))
        {
            if (store.Find(code!) is null)
            {
                await Reject(ctx, $"알 수 없는 테넌트 '{code}'.");
                return;
            }
            tenantContext.SetTenant(code!);
        }

        if (ctx.Request.Path.StartsWithSegments("/api") && !tenantContext.HasTenant)
        {
            await Reject(ctx, "테넌트가 지정되지 않았습니다(X-Tenant-Code 헤더/서브도메인/JWT).");
            return;
        }

        await _next(ctx);
    }

    private static async Task Reject(HttpContext ctx, string message)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "application/problem+json; charset=utf-8";
        await ctx.Response.WriteAsJsonAsync(new { error = "tenant", message });
    }

    private static string? Resolve(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Tenant-Code", out var header) &&
            !string.IsNullOrWhiteSpace(header))
            return header.ToString().Trim();

        var claim = ctx.User?.FindFirst("tenant")?.Value;
        if (!string.IsNullOrWhiteSpace(claim))
            return claim;

        var host = ctx.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 2)            // acme.mes.example.com → "acme"
            return parts[0];

        return null;
    }
}
